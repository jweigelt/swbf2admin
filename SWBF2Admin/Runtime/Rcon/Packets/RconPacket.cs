namespace SWBF2Admin.Runtime.Rcon.Packets
{
    class RconPacket
    {
        public string Command { get; }
        public bool PacketOk { get; set; } = false;
        
        public RconPacket(string command)
        {
            Command = command;
        }

        public virtual void HandleResponse(string response)
        {
            PacketOk = true;
        }
    }
}