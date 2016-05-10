using System;
using System.Configuration;
using FlrEpjDemo.Console.Examples;
using FlrEpjDemo.Lib;
using Microsoft.ServiceBus.Messaging;
using NHN.DtoContracts.Flr.Service;
using NHN.DtoContracts.ServiceBus.Data;
using NHN.DtoContracts.ServiceBus.Service;
using NHN.WcfClientFactory;
using static System.Console;

namespace FlrEpjDemo.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var myOrganizationNumber = int.Parse(ConfigurationManager.AppSettings["OrganizationNumber"]);
            var clientFactory = GetWcfClientFactory();
            var flrRead = clientFactory.Get<IFlrReadOperations>();

            // Start or get subscription to flr topic
            var subscription = GetSubscription(clientFactory);

            // Start listening to events
            var sbCnnString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
            var flrEventManager = new FlrEventManager(sbCnnString, subscription);

            flrEventManager.ListeningStarted += () => WriteLine("Listening started");
            flrEventManager.ListeningEnded += () => WriteLine("Listening ended");
            flrEventManager.ExceptionOccured += HandleException;

            // Initialize examples
            var getPatientListsExample = new GetPatientlists(myOrganizationNumber, flrRead);
            var receiveEventsExample = new ReceiveEvents(flrEventManager);
            var collectingEvents = new CollectingEvents(flrEventManager);

            var running = true;

            do
            {
                WriteLine(new string('-', 100));
                WriteLine("Select an example to run");
                WriteLine("Press ESC to exit");
                WriteLine(new string('-', 100));
                WriteLine("1. Contract and patient list lookup");
                WriteLine("2. Receiving events");
                WriteLine("3. Collecting events");
                var key = ReadKey(true);
                WriteLine(new string('-', 100));
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        running = false;
                        break;
                    case ConsoleKey.D1:
                        getPatientListsExample.Run();
                        break;
                    case ConsoleKey.D2:
                        receiveEventsExample.Run();
                        break;
                    case ConsoleKey.D3:
                        collectingEvents.Run();
                        break;
                }
            } while (running);
        }

        private static void HandleException(BrokeredMessage msg, Exception exception)
        {
            WriteLine("Something went wrong: " + exception.Message);
            WriteLine("Message: " + msg);
        }

        private static SubscriptionInfo GetSubscription(WcfClientFactory clientFactory)
        {
            var serviceBusManager = clientFactory.Get<IServiceBusManager>();
            var eventSource = ConfigurationManager.AppSettings["SubscriptionInfo.EventSource"];
            var userSystemIdent = ConfigurationManager.AppSettings["SubscriptionInfo.UserSystemIdent"];

            return serviceBusManager.AddOrGetSubscription(eventSource, userSystemIdent);
        }

        private static WcfClientFactory GetWcfClientFactory()
        {
            var username = ConfigurationManager.AppSettings["WcfClientFactory.Username"];
            var password = ConfigurationManager.AppSettings["WcfClientFactory.Password"];
            var clientFactory = new WcfClientFactory(Hostnames.Utvikling)
            {
                Username = username,
                Password = password,
                FixDnsIdentityProblem = true,
                Transport = Transport.Https
            };
            return clientFactory;
        }
    }
}