using System.Net;
using System.Xml.Serialization;
using SWBF2Admin.Config;

namespace SWBF2Admin.Gameserver
{
    [ConfigFileInfo(fileName: "./cfg/ingameservercontroller.xml"/*,template: "SWBF2Admin.Resources.cfg.announce.xml"*/)]
    public class IngameServerControllerConfiguration
    {
        public int TcpTimeout { get; set; } = 100;
        public int StartupTime { get; set; } = 20000;
        public int NotRespondingCheckInterval { get; set; } = 5000;
        public int NotRespondingMaxCount { get; set; } = 2;
        public int ReadTimeout { get; set; } = 100;
        public int MapHangTimeout { get; set; } = 20000;
        public int FreezeTime { get; set; } = 1000;
        public int FreezesBeforeKill { get; set; } = 10;

        public string ServerHostname { get; set; } = "127.0.0.1:1138";

        [XmlIgnore]
        public virtual IPEndPoint ServerIPEP
        {
            get
            {
                string[] cc = ServerHostname.Split(':');
                return new IPEndPoint(IPAddress.Parse(cc[0]), (cc.Length > 1 ? int.Parse(cc[1]) : 4658));
            }
        }
    }
}
