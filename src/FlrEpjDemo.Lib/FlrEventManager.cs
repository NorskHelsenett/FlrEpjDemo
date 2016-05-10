using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.ServiceBus.Messaging;
using NHN.DtoContracts.Flr.Data;
using NHN.DtoContracts.Flr.Enum;
using NHN.DtoContracts.ServiceBus.Data;
#pragma warning disable 67

namespace FlrEpjDemo.Lib
{
    internal class EventHandlerInfo
    {
        public EventHandlerInfo(EventInfo eventInfo, Type maybeBodyObjectType)
        {
            EventInfo = eventInfo;
            Type = maybeBodyObjectType;
        }

        public EventInfo EventInfo { get; }
        public Type Type { get; }
    }

    /// <summary>
    /// Lytting og håndtering av hendelser som inntreffer for
    /// angitt FLR topic subscription.
    /// </summary>
    public class FlrEventManager
    {
        private readonly string _sbCnnString;
        private readonly SubscriptionInfo _subscription;

        private bool _isListening;
        private Task _listenTask;

        private MessagingFactory _messagingFactory;

        private static readonly Dictionary<Type, DataContractSerializer> Serializers = new Dictionary<Type, DataContractSerializer>
        {
            {typeof (GPContract), new DataContractSerializer(typeof (GPContract))},
            {typeof (GPOnContractAssociation), new DataContractSerializer(typeof (GPOnContractAssociation))},
            {typeof (PatientToGPContractAssociation), new DataContractSerializer(typeof (PatientToGPContractAssociation))},
            {typeof (OutOfOfficeLocation), new DataContractSerializer(typeof (OutOfOfficeLocation))}
        };
        private static readonly Dictionary<FlrEvents, EventHandlerInfo> EventHandlers = new Dictionary<FlrEvents, EventHandlerInfo>();

        static FlrEventManager()
        {
            var events = typeof(FlrEventManager).GetEvents();
            foreach (FlrEvents flrEvent in Enum.GetValues(typeof(FlrEvents)))
            {
                var eventInfo = events.FirstOrDefault(f => f.Name == flrEvent.ToString());
                if (eventInfo == null)
                    throw new Exception($"Event {flrEvent} mangler en public event Action<...>");

                var numGenericParams = eventInfo.EventHandlerType.GenericTypeArguments.Length;
                var maybeBodyObjectType = numGenericParams > 1 ? eventInfo.EventHandlerType.GenericTypeArguments[0] : null;

                EventHandlers.Add(flrEvent, new EventHandlerInfo(eventInfo, maybeBodyObjectType));
            }
        }

        public FlrEventManager(string sbCnnString, SubscriptionInfo subscription)
        {
            _sbCnnString = sbCnnString;
            _subscription = subscription;
        }

        public bool IsListening => _isListening;

        /// <summary>
        /// Start å lytte på meldinger.
        /// </summary>
        /// <param name="receiveMode">Receive mode (<see cref="ReceiveMode"/>)</param>
        public void StartListening(ReceiveMode receiveMode)
        {
            if (_isListening)
                return;

            _messagingFactory = MessagingFactory.CreateFromConnectionString(_sbCnnString);
            _isListening = true;
            _listenTask = Task.Run(() => Listen(receiveMode));
            ListeningStarted?.Invoke();
        }

        /// <summary>
        /// Stopper lytting etter hendelser.
        /// </summary>
        public void EndListening()
        {
            _isListening = false;
            
            if (_listenTask == null || _messagingFactory == null)
                return;

            if (!_messagingFactory.IsClosed)
                _messagingFactory.Close();

            Task.WaitAll(_listenTask);
            ListeningEnded?.Invoke();
            _messagingFactory = null;
        }

        private void Listen(ReceiveMode receiveMode)
        {
            var mr = _messagingFactory.CreateMessageReceiver(_subscription.FullPath, receiveMode);
            while (_isListening)
            {
                BrokeredMessage message = null;
                try
                {
                    message = mr.Receive();
                }
                catch (TimeoutException)
                {
                    ListeningTimedOut?.Invoke();
                    continue;
                }
                catch (OperationCanceledException) //When Cancel() is called on client or factory
                {
                    return;
                }
                catch (Exception ex)
                {
                    ExceptionOccured?.Invoke(message, ex);
                    return;
                }

                if (message == null)
                    continue;

                try
                {
                    var isMessageHandled = HandleMessage(message);
                    if (receiveMode == ReceiveMode.PeekLock)
                    {
                        if (isMessageHandled)
                            message.Complete();
                        else
                            message.Abandon();
                    }
                }
                catch (Exception ex)
                {
                    if (receiveMode == ReceiveMode.PeekLock)
                        message.Abandon();

                    ExceptionOccured?.Invoke(message, ex);
                }

                Thread.Sleep(50);
            }
        }

        private bool HandleMessage(BrokeredMessage message)
        {
            MessageReceived?.Invoke(message);

            object eventName;
            if (!message.Properties.TryGetValue("eventName", out eventName))
                throw new EventNameMissingException();

            FlrEvents flrEvent;
            if (!Enum.TryParse((string) eventName, out flrEvent))
                throw new UnknownFlrEventNameException((string) eventName);

            var eventHandle = EventHandlers[flrEvent];
            var eventInfo = eventHandle.EventInfo;
            var bodyObjectType = eventHandle.Type;
            var eventDelegate = GetType().GetField(eventInfo.Name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this) as MulticastDelegate;

            if (eventDelegate == null && AnyEvent == null)
                return false;

            object bodyObject = null;
            if (bodyObjectType != null)
            {
                // Deserialize body object
                var stream = message.GetBody<Stream>();
                var serializer = Serializers[bodyObjectType];
                var reader = XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas.Max);
                bodyObject = serializer.ReadObject(reader);
            }

            // Fire event handlers
            AnyEvent?.Invoke(flrEvent, bodyObject, message.Properties);

            if (eventDelegate != null)
            {
                var args = bodyObject == null
                    ? new object[] {message.Properties}
                    : new[] {bodyObject, message.Properties};

                foreach (var handler in eventDelegate.GetInvocationList())
                    handler.Method.Invoke(handler.Target, args);
            }

            return true;
        }

        /// <summary>
        /// Fyres når et uventet unntak blir kastet.
        /// </summary>
        public event Action<BrokeredMessage, Exception> ExceptionOccured;

        /// <summary>
        /// Fyres når en melding er mottatt, før den blir håndtert.
        /// </summary>
        public event Action<BrokeredMessage> MessageReceived;

        /// <summary>
        /// Fyres når man starter å lytte på topic.
        /// </summary>
        public event Action ListeningStarted;

        /// <summary>
        /// Fyres når man slutter å lytte på topic.
        /// </summary>
        public event Action ListeningEnded;

        /// <summary>
        /// Fyres når timeout inntreffer under lytting
        /// </summary>
        public event Action ListeningTimedOut;

        /// <summary>
        /// Fyrs for alle hendelser som inntreffer.
        /// </summary>
        public event Action<FlrEvents, object, IDictionary<string, object>> AnyEvent;


        // Events, must be named after enum values in NHN.DtoContracts.Flr.Enum.FlrEvents

        /// <summary>
        /// Fyres når en kontrakt blir opprettet.
        /// </summary>
        public event Action<GPContract, IDictionary<string, object>> ContractCreated;

        /// <summary>
        /// Fyres når en kontrakt er oppdatert.
        /// </summary>
        public event Action<GPContract, IDictionary<string, object>> ContractUpdated;

        /// <summary>
        /// Fyres når en kontrakt blir kansellert.
        /// </summary>
        public event Action<GPContract, IDictionary<string, object>> ContractCanceled;

        /// <summary>
        /// Fyres når en ny legeperiode blir koblet mot en fastlegeavtale.
        /// </summary>
        public event Action<GPOnContractAssociation, IDictionary<string, object>> GPOnContractCreated;

        /// <summary>
        /// Fyres når en legeperiode blir oppdatert med ny informasjon.
        /// </summary>
        public event Action<GPOnContractAssociation, IDictionary<string, object>> GPOnContractUpdated;

        /// <summary>
        /// Fyres når en legeperiode blir slettet.
        /// </summary>
        public event Action<GPOnContractAssociation, IDictionary<string, object>> GPOnContractDeleted;
        
        /// <summary>
        /// Fyres når en pasient blir opprettet.
        /// </summary>
        public event Action<PatientToGPContractAssociation, IDictionary<string, object>> PatientOnContractCreated;

        /// <summary>
        /// Fyres når en pasient blir oppdatert.
        /// </summary>
        public event Action<PatientToGPContractAssociation, IDictionary<string, object>> PatientOnContractUpdated;

        /// <summary>
        /// Fyres når en pasient blir avsluttet.
        /// </summary>
        public event Action<PatientToGPContractAssociation, IDictionary<string, object>> PatientOnContractCanceled;

        /// <summary>
        /// Fyres når et nytt utekontor blir opprettet.
        /// </summary>
        public event Action<OutOfOfficeLocation, IDictionary<string, object>> OutOfOfficeLocationCreated;

        /// <summary>
        /// Fyres når en utekontor blir oppdatert.
        /// </summary>
        public event Action<OutOfOfficeLocation, IDictionary<string, object>> OutOfOfficeLocationUpdated;

        /// <summary>
        /// Fyres når et utekontor blir slettet.
        /// </summary>
        public event Action<OutOfOfficeLocation, IDictionary<string, object>> OutOfOfficeLocationDeleted;

        /// <summary>
        /// Fyres når en pasients NIN blir endret.
        /// </summary>
        public event Action<IDictionary<string, object>> PatientNinChanged;
    }
}