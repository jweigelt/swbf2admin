using SWBF2Admin.Gameserver;

namespace SWBF2Admin.Structures
{
    class ServerInfo
    {
        public ServerStatus Status { get; set; }
        public virtual int StatusId { get { return (int)Status; } }

        public string ServerName { get; set; } = "Keks' No Force | No Shooter";
        public string ServerIP { get; set; } = "127.0.0.1";
        public string Version { get; set; } = "1.0";
        public string MaxPlayers { get; set; } = "10";
        public string Password { get; set; } = "123";
        public string CurrentMap { get; set; } = "tat2g_eli";
        public string NextMap { get; set; } = "tat2g_eli";
        public string GameMode { get; set; } = "Assault";
        public string Players { get; set; } = "5";
        public string Scores { get; set; } = "123 / 123";
        public string Tickets { get; set; } = "1234 / 1234";
        public string FFEnabled { get; set; } = "true";
        public string Heroes { get; set; } = "true";
    }
}