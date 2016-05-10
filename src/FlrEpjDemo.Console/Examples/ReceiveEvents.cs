using System;
using System.Collections.Generic;
using FlrEpjDemo.Lib;
using Microsoft.ServiceBus.Messaging;
using NHN.DtoContracts.Flr.Data;
using static System.Console;

namespace FlrEpjDemo.Console.Examples
{
    public class ReceiveEvents
    {
        private readonly FlrEventManager _flrEventManager;
        private readonly ContractRepository _contractRepository;

        public ReceiveEvents(FlrEventManager flrEventManager)
        {
            _flrEventManager = flrEventManager;
            _contractRepository = new ContractRepository();
        }

        public void Run()
        { 
            // Receive "ContractCreated" and "ContractUpdated" events
            // and react accordingly
            _flrEventManager.ContractCreated += HandleContractCreated;
            _flrEventManager.ContractUpdated += HandleContractUpdated;
            _flrEventManager.StartListening(ReceiveMode.PeekLock);

            WriteLine("Press ESC to end...");
            while (ReadKey(true).Key != ConsoleKey.Escape) { }

            _flrEventManager.EndListening();
            _flrEventManager.ContractCreated -= HandleContractCreated;
            _flrEventManager.ContractUpdated -= HandleContractUpdated;
        }

        private void HandleContractCreated(GPContract contract, IDictionary<string, object> properties)
        {
            WriteLine("Received ContractCreated");
            _contractRepository.SaveContract(contract);
        }

        private void HandleContractUpdated(GPContract contract, IDictionary<string, object> properties)
        {
            WriteLine("Received ContractUpdated");
            _contractRepository.UpdateContract(contract);
        }
    }
}
