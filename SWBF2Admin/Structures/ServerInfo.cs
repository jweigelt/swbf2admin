using SWBF2Admin.Gameserver;

namespace SWBF2Admin.Structures
{
    class ServerInfo
    {
        public ServerStatus Status { get; set; }
        public virtual int StatusId { get { return (int)Status; } }

        public string ServerName { get; set; } = " - ";
        public string ServerIP { get; set; } = " - ";
        public string Version { get; set; } = " - ";
        public string MaxPlayers { get; set; } = " - ";
        public string Password { get; set; } = " - ";
        public string CurrentMap { get; set; } = " - ";
        public string NextMap { get; set; } = " - ";
        public string GameMode { get; set; } = " - ";
        public string Players { get; set; } = " - ";
        public string Scores { get; set; } = " - / - ";
        public string Tickets { get; set; } = " - / - ";
        public string FFEnabled { get; set; } = " - ";
        public string Heroes { get; set; } = " - ";
    }
}