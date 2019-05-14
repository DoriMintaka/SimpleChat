using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;

namespace AzureServiceBus
{
    class Program
    {
        static void Main(string[] args)
        {
            var connection = ConfigurationManager.AppSettings["connection"];

            Console.WriteLine("Login:");
            var login = Console.ReadLine();

            ManagementClient client = new ManagementClient(connection);
            var users = client.GetSubscriptionsAsync("chat").Result;
            if (users.All(u => u.SubscriptionName != login))
            {
                var filter = new SqlFilter($"sys.Label != '{login}'");
                RuleDescription rule = new RuleDescription("default", filter);
                try
                {
                    client.CreateSubscriptionAsync(new SubscriptionDescription("chat", login), rule, CancellationToken.None)
                        .GetAwaiter().GetResult();
                }
                catch (Exception)
                {
                    Console.WriteLine("An error occured. Try another login!");
                    Thread.Sleep(1000);
                    return;
                }
            }
            
            var subClient = new SubscriptionClient(connection, "chat", login);
            subClient.RegisterMessageHandler(
                async (message, token) =>
                    await Task.Run(() => Console.WriteLine($"{message.Label}: {GetEmoji(Encoding.UTF8.GetString(message.Body))}"), token),
                async exceptionReceivedEventArgs =>
                    await Task.Run(() => Debug.WriteLine(exceptionReceivedEventArgs.Exception.Message)));

            var topicClient = new TopicClient(connection, "chat");
            topicClient.SendAsync(new Message
                {Label = "server", Body = Encoding.UTF8.GetBytes($"{login} joined the server.")});

            while (true)
            {
                string message = Console.ReadLine();
                if (message == null || message == "/exit")
                {
                    topicClient.SendAsync(new Message
                        { Label = "server", Body = Encoding.UTF8.GetBytes($"{login} left the server.") }).GetAwaiter().GetResult();
                    break;
                }

                topicClient.SendAsync(new Message {Label = login, Body = Encoding.UTF8.GetBytes(message)});
            }

            subClient.CloseAsync().GetAwaiter().GetResult();
            topicClient.CloseAsync().GetAwaiter().GetResult();
            client.CloseAsync().GetAwaiter().GetResult();
        }

        private static string GetEmoji(string text)
        {
            Dictionary<string, string> emotes = new Dictionary<string, string>()
            {
                {
                    ":gus:",
                    "ЗАПУСКАЕМ\r\n░ГУСЯ░▄▀▀▀▄░РАБОТЯГИ░░\r\n▄███▀░◐░░░▌░░░░░░░\r\n░░░░▌░░░░░▐░░░░░░░\r\n░░░░▐░░░░░▐░░░░░░░\r\n░░░░▌░░░░░▐▄▄░░░░░\r\n░░░░▌░░░░▄▀▒▒▀▀▀▀▄\r\n░░░▐░░░░▐▒▒▒▒▒▒▒▒▀▀▄\r\n░░░▐░░░░▐▄▒▒▒▒▒▒▒▒▒▒▀▄\r\n░░░░▀▄░░░░▀▄▒▒▒▒▒▒▒▒▒▒▀▄\r\n░░░░░░▀▄▄▄▄▄█▄▄▄▄▄▄▄▄▄▄▄▀▄\r\n░░░░░░░░░░░▌▌░▌▌░░░░░\r\n░░░░░░░░░░░▌▌░▌▌░░░░░\r\n░░░░░░░░░▄▄▌▌▄▌▌░░░░░"
                },
                { ":hydra:", "ЗАПУСКАЕМ░░\r\n░ГУСЯ░▄▀▀▀▄░ГИДРУ░░\r\n▄███▀░◐░▄▀▀▀▄░░░░░░\r\n░░▄███▀░◐░░░░▌░░░\r\n░░░▐░▄▀▀▀▄░░░▌░░░░\r\n▄███▀░◐░░░▌░░▌░░░░\r\n░░░░▌░░░░░▐▄▄▌░░░░░\r\n░░░░▌░░░░▄▀▒▒▀▀▀▀▄\r\n░░░▐░░░░▐▒▒▒▒▒▒▒▒▀▀▄\r\n░░░▐░░░░▐▄▒▒▒▒▒▒▒▒▒▒▀▄\r\n░░░░▀▄░░░░▀▄▒▒▒▒▒▒▒▒▒▒▀▄\r\n░░░░░░▀▄▄▄▄▄█▄▄▄▄▄▄▄▄▄▄▄▀▄\r\n░░░░░░░░░░░▌▌░▌▌░░░░░\r\n░░░░░░░░░░░▌▌░▌▌░░░░░\r\n░░░░░░░░░▄▄▌▌▄▌▌░░░░░" },
                { ":scout:", "░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░\r\n░░░░░░ЗАПУСКАЕМ ГУСЕЙ РАЗВЕДЧИКОВ░░░░░\r\n░░░░░░░▄▀▀▀▄░░░░░▄▀▀▀▄░░░░░░░░░░░\r\n░░▄███▀░◐░░░▌░░░▐░░░◐░▀███▄░░░░░░\r\n░░░░░░▌░░░░░▐░░░▌░░░░░▐░░░░░░░░░░\r\n░░░░░░▐░░░░░▐░░░▌░░░░░▌░░░░░░░░░░"},
                { ":heart:", "<3"}
            };
            var parsed = text.Split();
            for (int i = 0; i < parsed.Length; i++)
            {
                if (emotes.ContainsKey(parsed[i]))
                {
                    parsed[i] = emotes[parsed[i]];
                }
            }
            var builder = new StringBuilder();
            foreach (var s in parsed)
            {
                builder.Append(s).Append(' ');
            }

            return builder.ToString();
        }
    }
}
