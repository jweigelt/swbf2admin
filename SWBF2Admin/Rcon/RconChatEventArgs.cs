using System;
namespace SWBF2Admin.Rcon
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