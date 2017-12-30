using System;
namespace SWBF2Admin.Runtime.Rcon
{
    class RconException : Exception
    {
        public RconException(Exception innerException) : base(innerException.Message, innerException) { }
        public RconException(string str) : base(str) { }
        public RconException() { }
    }
}