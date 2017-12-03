using SWBF2Admin.Gameserver;
using System;

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
        public string Scores { get; set; } = " - / - / - ";

        private uint[] tickets = { 0, 0 };
        public string Tickets
        {
            get
            {
                return string.Format("{0}/{1}", tickets[0], tickets[1]);
            }
            set
            {
                string[] s = value.Split('/');
                tickets[0] = BitConverter.ToUInt32(BitConverter.GetBytes(int.Parse(s[0])), 0) + 180;
                tickets[1] = BitConverter.ToUInt32(BitConverter.GetBytes(int.Parse(s[1])), 0) + 180;
                tickets[0] -= Int32.MaxValue ;
                tickets[1] -= Int32.MaxValue;

            }
        }
        public string FFEnabled { get; set; } = " - ";
        public string Heroes { get; set; } = " - ";
    }
}