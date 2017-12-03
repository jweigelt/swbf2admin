using System.Collections.Generic;
using System.Xml.Serialization;
namespace SWBF2Admin.Runtime.Announce
{
    public class AnnounceHandlerConfiguration
    {
       
        [XmlIgnore]
        public const string FILE_NAME = "./cfg/announce.xml";

        //[XmlIgnore]
        //public const string RESOURCE_NAME = "SWBF2Admin.Resources.cfg.announce.xml";
        

        public bool Enable { get; set; } = false;
        public int Interval { get; set; } = 360;
        public List<Announce> AnnounceList = new List<Announce>();
    }
}
