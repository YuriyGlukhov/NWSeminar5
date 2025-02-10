using System;

namespace NWSeminar5
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                await Server.StartServer();
            }
            else
            {

                string username = args[0];
                if (int.TryParse(args[1], out int clientPort))
                {
                    await Client.ClientSender(username, clientPort);
                }
                else
                {
                    Console.WriteLine("Ошибка: укажите корректный порт.");
                }

            }
        }
    }
}
