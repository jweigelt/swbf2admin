using System;
using SWBF2Admin.Structures;
namespace SWBF2Admin.Runtime.Rcon
{
    class RconChatEventArgs : EventArgs
    {
    
        public ChatMessage Message { get; }

        public RconChatEventArgs(ChatMessage message)
        {
            Message = message;
        }
    }
}