using System;
using NHN.DtoContracts.Flr.Enum;

namespace FlrEpjDemo.Lib
{
    internal class UnknownFlrEventNameException : Exception
    {
        public UnknownFlrEventNameException(string eventName):base($"{eventName} er en ukjent {nameof(FlrEvents)} type. Gyldige {nameof(FlrEvents)} typer ligger i {typeof(FlrEvents).FullName}"){}
    }
}