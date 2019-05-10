using System;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace MessageReceiver
{
    class Program
    {
        private static IQueueClient qclient;
        private static ISubscriptionClient sclient;

        static async Task Main(string[] args)
        {
            string connection = ConfigurationManager.AppSettings["connection"];
            string name = "testqueue";
            string topic = "testtopic";
            string subscription = "sub1";

            //qclient = new QueueClient(connection, name, ReceiveMode.ReceiveAndDelete);
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                AutoComplete = false
            };

            //qclient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
            //Console.Read();
            //await qclient.CloseAsync();

            sclient = new SubscriptionClient(connection, topic, subscription, ReceiveMode.ReceiveAndDelete);
            sclient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
            Console.Read();
            await sclient.CloseAsync();
        }

        static Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            Console.WriteLine($"{message.Label}: {Encoding.UTF8.GetString(message.Body)}");
            return Task.CompletedTask;
        }
        
        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Debug.WriteLine(exceptionReceivedEventArgs.Exception.Message);
            return Task.CompletedTask;
        }
    }
}
