namespace SWBF2Admin.Runtime.Rcon.Packets
{
    public class RconPacket
    {
        public string Command { get; }
        public bool PacketOk { get; set; } = false;
        private string response;
        public virtual string Response { get { return response; } }

        public RconPacket(string command)
        {
            Command = command;
        }

        public virtual void HandleResponse(string response)
        {
            PacketOk = true;
            this.response = response;
        }
    }
}