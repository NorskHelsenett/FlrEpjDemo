using System;
using Microsoft.ServiceBus.Messaging;

namespace FlrEpjDemo.Lib
{
    internal class EventNameMissingException : Exception
    {
        public EventNameMissingException():base($"eventName mangler i {nameof(BrokeredMessage)}.{nameof(BrokeredMessage.Properties)}"){}
    }
}