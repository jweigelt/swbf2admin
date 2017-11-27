using System;
namespace SWBF2Admin.Rcon
{
    class RconException : Exception
    {

        public RconException(Exception innerException) : base("", innerException) { }
        public RconException(string str) { }
    }
}