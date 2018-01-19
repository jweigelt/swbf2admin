using SWBF2Admin.Structures;
using SWBF2Admin.Web.Pages;

namespace SWBF2Admin.Runtime.Rcon.Packets
{
    class WebChatPacket : RconPacket
    {
        public virtual ChatPage.ChatSession Session { get; }

        public WebChatPacket(string command, ChatPage.ChatSession session) : base(command)
        {
            Session = session;
        }

        public ChatMessage[] GetMessages()
        {
            string[] rows = Response.Split('\n');
            ChatMessage[] messages = new ChatMessage[rows.Length];
            for (int i = 0; i < messages.Length; i++)
                messages[i] = new ChatMessage(rows[i]);
            return messages;
        }
    }
}