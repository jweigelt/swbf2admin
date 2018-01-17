namespace SWBF2Admin.Structures
{
    class ChatMessage
    {
        public bool IsSystem { get; }
        public string Message { get; }
        public string PlayerName { get; }
        public byte PlayerSlot { get; }

        public ChatMessage(string message, string playerName)
        {
            Message = message;
            PlayerName = playerName;
            IsSystem = false;
        }

        public ChatMessage(string message)
        {
            Message = message;
            IsSystem = true;
        }
    }
}