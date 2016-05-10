using System.Collections.Generic;
using NHN.DtoContracts.Flr.Data;

namespace FlrEpjDemo.Console.Examples
{
    public class ContractRepository
    {
        private readonly IDictionary<long, GPContract> _contracts;

        public ContractRepository()
        {
            _contracts = new Dictionary<long, GPContract>();
        }

        public void SaveContract(GPContract contract)
        {
            _contracts.Add(contract.Id, contract);
        }

        public void UpdateContract(GPContract contract)
        {
            GPContract existing;
            if (_contracts.TryGetValue(contract.Id, out existing))
            {
                contract.PatientList = existing.PatientList;
                _contracts[contract.Id] = contract;
            }
        }
    }
}