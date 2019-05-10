using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace AzureServiceBus
{
    class Program
    {
        private static string[] logins = {"mintaka", "warmaster256", "caveek"};
        static void Main(string[] args)
        {
            var connection = ConfigurationManager.AppSettings["connection"];
            string login;
            ISubscriptionClient subClient;
            ITopicClient topicClient;

            do
            {
                Console.WriteLine("Login:");
                login = Console.ReadLine();
            } while (!logins.Contains(login));

            subClient = new SubscriptionClient(connection, "chat", login);
            topicClient = new TopicClient(connection, "chat");

            subClient.RegisterMessageHandler(
                async (Message message, CancellationToken token) =>
                    await Task.Run(() => Console.WriteLine($"{message.Label}: {Encoding.UTF8.GetString(message.Body)}"), token),
                async (ExceptionReceivedEventArgs exceptionReceivedEventArgs) =>
                    await Task.Run(() => Debug.WriteLine(exceptionReceivedEventArgs.Exception.Message)));
            topicClient.SendAsync(new Message
                {Label = "server", Body = Encoding.UTF8.GetBytes($"{login} joined the server.")});

            while (true)
            {
                string message = Console.ReadLine();
                if (message == null || message == "exit")
                {
                    topicClient.SendAsync(new Message
                        { Label = "server", Body = Encoding.UTF8.GetBytes($"{login} left the server.") });
                    break;
                }
                Console.SetCursorPosition(0, Console.CursorTop - 1);

                topicClient.SendAsync(new Message {Label = login, Body = Encoding.UTF8.GetBytes(message)});
            }

            subClient.CloseAsync().GetAwaiter().GetResult();
            topicClient.CloseAsync().GetAwaiter().GetResult();
        }
    }
}
