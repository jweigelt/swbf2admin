using System.Net;
using System.Xml.Serialization;
using SWBF2Admin.Config;

namespace SWBF2Admin.Gameserver
{
    [ConfigFileInfo(fileName: "./cfg/ingameservercontroller.xml"/*,template: "SWBF2Admin.Resources.cfg.announce.xml"*/)]
    public class IngameServerControllerConfiguration
    {
        public bool Enable { get; set; } = true;
        public int TcpTimeout { get; set; } = 100;
        public int StartupTime { get; set; } = 20000;
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
