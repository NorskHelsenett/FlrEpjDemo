using FlrEpjDemo.Lib;
using NHN.DtoContracts.Flr.Service;
using static System.Console;

namespace FlrEpjDemo.Console.Examples
{
    public class GetPatientlists
    {
        private readonly int _organizationNumber;
        private readonly IFlrReadOperations _flrRead;
        private readonly ContractRepository _contractRepository;

        public GetPatientlists(int organizationNumber, IFlrReadOperations flrRead)
        {
            _organizationNumber = organizationNumber;
            _flrRead = flrRead;

            _contractRepository = new ContractRepository();
        }

        public void Run()
        {
            // Get contracts for my organization
            var contracts = _flrRead.GetGPContractsOnOffice(_organizationNumber, null);

            WriteLine("Contracts found:");
            foreach (var contract in contracts)
                WriteLine($"Id: {contract.Id}, Municipality:{contract.Municipality.CodeText}, Period: {contract.Valid.From:d} - {contract.Valid.To:d}");

            // Get the patient list for each contract and store it
            foreach (var contract in contracts)
            {
                contract.PatientList = _flrRead.GetGPPatientList(contract.Id);
                _contractRepository.SaveContract(contract);
            }
        }
    }
}
