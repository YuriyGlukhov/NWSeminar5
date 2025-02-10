using NWSeminar5.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.ComponentModel;
using Microsoft.Identity.Client.Extensions.Msal;

namespace NWSeminar5
{
    public class Server
    {
       static Dictionary<string, IPEndPoint> clients = new Dictionary<string, IPEndPoint>();
       static List<MessageUDP> unReceivedMsgs = new List<MessageUDP>();
       public static CancellationTokenSource cts = new CancellationTokenSource();
       static UdpClient udpClient;


        public static void StartServer()
        {
            udpClient = new UdpClient(12345);
            Console.WriteLine("Сервер запущен");
            ServerWork();
        }
        public static async void Register(MessageUDP message, IPEndPoint endPoint)
        {
            if (clients.TryGetValue(message.FromName, out IPEndPoint? recipient))
            {
                Console.WriteLine($"Пользователь {message.FromName} уже был добавлен ранее!");
            }
            else
            {
                clients.Add(message.FromName, endPoint);

                MessageUDP outMessage = new MessageUDP("Server", $"Пользователь {message.FromName} добавлен");

                string json = outMessage.ToJson();
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                udpClient.Send(bytes, endPoint);

                using (var ctx = new ChatContext())
                {
                    bool checkUserInDB = ctx.Users.Any(x => x.Name == message.FromName);
                    if (!checkUserInDB)
                    {
                        ctx.Add(new User { Name = message.FromName });
                        ctx.SaveChanges();
                    }
                }

               await SendUnReceivedMsg(message,endPoint);
            }
             
        }
        public static void UnReceivedMSGtoDB(MessageUDP message)
        {
            using (var ctx = new ChatContext())
            {
                var toUser = ctx.Users.FirstOrDefault(x => x.Name == message.FromName);
                if (toUser == null)
                {
                    Console.WriteLine("Получатель не найден в базе данных!");
                    return;
                }

                var messagesToUpdate = ctx.Messages
                    .Where(x => x.FromUserId == toUser.Id && x.Reseived == false)
                    .ToList();

                if (messagesToUpdate.Any())
                {
                    foreach (var msg in messagesToUpdate)
                    {
                        msg.Reseived = true;
                    }
                    ctx.SaveChanges();
                    Console.WriteLine($"Обновлено {messagesToUpdate.Count} сообщений в базе.");
                }
            }
        }
        public static async Task SendUnReceivedMsg(MessageUDP message, IPEndPoint endPoint)
        {
            List<MessageUDP> pendingMessages = unReceivedMsgs.Where(m => m.ToName == message.FromName).ToList();

            if (pendingMessages.Count > 0)
            {
                Console.WriteLine($"Отправляем {pendingMessages.Count} недоставленных сообщений для {message.FromName}");

                UnReceivedMSGtoDB(message);
                foreach (var msg in pendingMessages)
                {
                    
                    string str = msg.ToJson();
                    byte[] bytes1 = Encoding.UTF8.GetBytes(str);
                    await udpClient.SendAsync(bytes1, endPoint);

                }
                unReceivedMsgs.RemoveAll(m => m.ToName == message.FromName);
            }
        }
       
        public static void ConfirmMessage(MessageUDP message, IPEndPoint endPoint)
        {
            using (var ctx = new ChatContext())
            {
                var user = ctx.Users.FirstOrDefault(x => x.Name == message.FromName);
                var text = ctx.Messages.FirstOrDefault(x =>x.Text == message.Text && user.Name == message.FromName);
               

                MessageUDP outMessage;

                if (text != null)
                {
                    if (text.Reseived == false)
                    {
                        outMessage = new MessageUDP("Sever", "Сообщение НЕ доставлено");
                    }
                    else
                    {
                        outMessage = new MessageUDP("Sever", $"Сообщение **{text.Text}** доставлено");
                    }
                 
                }
                else  
                {
                    outMessage = new MessageUDP("Sever", "Сообщение не найдено!");
                }
                string str = outMessage.ToJson();
                byte[] bytes1 = Encoding.UTF8.GetBytes(str);
                udpClient.Send(bytes1, endPoint);

            }

        }

        public static void SendMessage(MessageUDP message)
        {
            int? id = null;
            if (clients.TryGetValue(message.ToName, out IPEndPoint? recEpClient))
            {
                using (var ctx = new ChatContext())
                {
                    var fromUser = ctx.Users.First(x => x.Name == message.ToName);
                    var toUser = ctx.Users.First(x => x.Name == message.FromName);
                    var dbMessage = new Message
                    {
                        FromUser = fromUser,
                        ToUser = toUser,
                        Reseived = true,
                        Text = message.Text
                    };
                    ctx.Messages.Add(dbMessage);
                    ctx.SaveChanges();
                    id = dbMessage.Id;
                }
                var str = message.ToJson();
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                udpClient.Send(bytes, recEpClient);

                Console.WriteLine($"Сообщение отправлено пользователю {message.ToName}");
            }

            else
            {
                Console.WriteLine("Пользователь не найден. Сообщение будет отправлено после авторизации получателя");
                unReceivedMsgs.Add(message);

                using (var ctx = new ChatContext())
                {
                    var fromUser = ctx.Users.First(x => x.Name == message.ToName);
                    var toUser = ctx.Users.First(x => x.Name == message.FromName);
                    var dbMessage = new Message
                    {
                        FromUser = fromUser,
                        ToUser = toUser,
                        Reseived = false,
                        Text = message.Text
                    };
                    ctx.Messages.Add(dbMessage);
                    ctx.SaveChanges();
                    id = dbMessage.Id;
                }
            }
        }

        public static void ReadMessage(MessageUDP message,IPEndPoint endPoint)
        {
            Console.WriteLine($"Получено сообщение: {message.FromName} -> {message.ToName}: **{message.Text}** ");

            if(message.Command == Command.Register)
            {
                Console.WriteLine($"Получена команда {message.Command} от {message.FromName}");
                Register(message,new IPEndPoint(endPoint.Address, endPoint.Port));
            }
            if(message.Command == Command.Confirmation)
            {
                Console.WriteLine($"Получена команда {message.Command} от {message.FromName}");
                ConfirmMessage(message, endPoint);
            }
            if (message.Command == Command.Message)
            {
                Console.WriteLine($"Получена команда {message.Command} от {message.FromName}");
                SendMessage(message);               
            }
            if (message.Command == Command.Exit)
            {
                Console.WriteLine($"Получена команда {message.Command} от {message.FromName}");
                Console.WriteLine("Сервер отключается...");
                cts.Cancel();
                udpClient.Close();
            }

        }
        public static void ServerWork()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            

            Console.WriteLine("Сервер ожидает сообщения от клиента...");

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    byte[] buffer = udpClient.Receive(ref endPoint);
                    string str = Encoding.UTF8.GetString(buffer);
                    MessageUDP inMessage = MessageUDP.FromJson(str);
                    ReadMessage(inMessage, endPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении сообщения: {ex.Message}");
                }
            }
        }
        
    }
}
