using System;
using System.Collections.Generic;
using FlrEpjDemo.Lib;
using Microsoft.ServiceBus.Messaging;
using NHN.DtoContracts.Flr.Enum;
using static System.Console;

namespace FlrEpjDemo.Console.Examples
{
    public class CollectingEvents
    {
        private readonly EventCollector _eventCollector;
        private readonly EventStorage _eventStorage;
        private readonly FlrEventManager _flrEventManager;
        
        public CollectingEvents(FlrEventManager flrEventManager)
        {
            _flrEventManager = flrEventManager;
            _eventStorage = new EventStorage();
            _eventCollector = new EventCollector(_eventStorage);
        }

        public void Run()
        {
            _flrEventManager.AnyEvent += _eventCollector.CollectEvent;
            _flrEventManager.StartListening(ReceiveMode.PeekLock);

            WriteLine("Press ESC to end...");
            while (ReadKey(true).Key != ConsoleKey.Escape) { }

            _flrEventManager.EndListening();
            _flrEventManager.AnyEvent -= _eventCollector.CollectEvent;
            
            WriteLine("Events collected:");
            foreach (var flrEventData in _eventStorage.GetEvents())
            {
                WriteLine(new string('-', 20));
                WriteLine("Event name: " + flrEventData.Event);
                WriteLine("Body object: " + (flrEventData.BodyObject?.GetType().Name ?? "null"));
                WriteLine("Properties:");
                foreach (var prop in flrEventData.Properties)
                {
                    WriteLine($"Name: {prop.Key}, Value: {prop.Value}");
                }
            }
        }
    }

    public class FlrEventData
    {
        public FlrEvents Event { get; set; }
        public object BodyObject { get; set; }
        public IDictionary<string, object> Properties { get; set; }
    }

    public class EventCollector
    {
        private readonly EventStorage _eventStorage;

        public EventCollector(EventStorage eventStorage)
        {
            _eventStorage = eventStorage;
        }

        public void CollectEvent(FlrEvents flrEvent, object bodyObject, IDictionary<string, object> props)
        {
            var flrEventData = new FlrEventData
            {
                Event = flrEvent,
                BodyObject = bodyObject,
                Properties = props
            };
            _eventStorage.StoreEvent(flrEventData);
        }
    }

    public class EventStorage
    {
        private readonly List<FlrEventData> _store = new List<FlrEventData>();
        public void StoreEvent(FlrEventData eventData)
        {
            _store.Add(eventData);
        }

        public IEnumerable<FlrEventData> GetEvents()
        {
            return _store;
        }
    }
}