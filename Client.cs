using NWSeminar5.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Abstractions;
using System.Xml.Linq;

namespace NWSeminar5
{
    internal class Client
    {
        public static async Task ClientListener(UdpClient udpClient)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                try
                {
                    UdpReceiveResult buffer = await udpClient.ReceiveAsync();
                    string str = Encoding.UTF8.GetString(buffer.Buffer);

                    MessageUDP inMessage = MessageUDP.FromJson(str);

                    Console.WriteLine($"\n{inMessage}");
                }
                catch (Exception)
                {
                    Console.WriteLine("Сервер выключен");
                }
            }
        }

        public static async Task ClientSender(string? nik, int clientPort)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);
            UdpClient udpClient = new UdpClient(clientPort);

            _ = Task.Run(async () => await ClientListener(udpClient));

            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    Console.Write("Введите команду (register, confirm, message or exit): ");
                    string? command = Console.ReadLine()?.ToLower();

                    if (command == "register")
                    {
                        MessageUDP message = new MessageUDP(nik, "")
                        {
                            Command = Command.Register
                        };
                        string json = message.ToJson();
                        byte[] bytes = Encoding.UTF8.GetBytes(json);
                        await udpClient.SendAsync(bytes, endPoint);
                    }

                    else if (command == "confirm")
                    {
                        Console.Write("Введите имя получателя: ");
                        string? toName = Console.ReadLine();

                        if (string.IsNullOrEmpty(toName))
                        {
                            Console.WriteLine("Вы не ввели имя!");
                            continue;
                        }
                        Console.Write("Введите сообщение, которое хотите проверить: ");
                        string? text = Console.ReadLine();

                        if (!string.IsNullOrEmpty(text))
                        {
                            MessageUDP message = new MessageUDP(nik, text)
                            {
                                Command = Command.Confirmation
                            };
                            string json = message.ToJson();
                            byte[] bytes = Encoding.UTF8.GetBytes(json);
                            await udpClient.SendAsync(bytes, endPoint);
                        }
                    }

                    else if (command == "message")
                    {
                        Console.Write("Введите имя получателя: ");
                        string? toName = Console.ReadLine();

                        if (string.IsNullOrEmpty(toName))
                        {
                            Console.WriteLine("Вы не ввели имя!");
                            continue;
                        }
                        Console.Write("Введите сообщение: ");
                        string? text = Console.ReadLine();

                        if (!string.IsNullOrEmpty(text))
                        {
                            MessageUDP message = new MessageUDP(nik, text)
                            {
                                ToName = toName,
                                Command = Command.Message
                            };

                            string json = message.ToJson();
                            byte[] bytes = Encoding.UTF8.GetBytes(json);
                            await udpClient.SendAsync(bytes, endPoint);
                        }
                        else
                        {
                            Console.WriteLine("Вы не ввели сообщение!");
                        }

                    }
                    else if (command == "exit".ToLower())
                    {
                        MessageUDP message = new MessageUDP(nik, "")
                        {
                            Command = Command.Exit
                        };
                        string json = message.ToJson();
                        byte[] bytes = Encoding.UTF8.GetBytes(json);
                        await udpClient.SendAsync(bytes, endPoint);

                        Console.WriteLine("Отключаемся от сервера");
                        Server.cts.Cancel();
                        udpClient.Close();
                        break;
                    }

                }
                catch (Exception)
                {
                    Console.WriteLine("Сервер выключен:(");
                }
            }
        }
    }
}
