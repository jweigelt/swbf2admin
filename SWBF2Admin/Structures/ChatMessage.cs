namespace SWBF2Admin.Structures
{
    class ChatMessage
    {
        public string Message { get; }
        public string PlayerName { get; }
        public byte PlayerSlot { get; }

        public ChatMessage(string message, string playerName)
        {
            Message = message;
            PlayerName = playerName;
        }
    }
}