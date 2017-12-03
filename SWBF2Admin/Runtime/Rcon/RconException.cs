using System;
namespace SWBF2Admin.Runtime.Rcon
{
    class RconException : Exception
    {
        public RconException(Exception innerException) : base("", innerException) { }
        public RconException(string str) { }
    }
}