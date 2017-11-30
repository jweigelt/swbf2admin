using System;
namespace SWBF2Admin.Runtime.Rcon
{
    class RconChatEventArgs : EventArgs
    {
        public string Name { get; }
        public string Message { get; }

        public RconChatEventArgs(string name, string message)
        {
            Name = name;
            Message = message;
        }
    }
}