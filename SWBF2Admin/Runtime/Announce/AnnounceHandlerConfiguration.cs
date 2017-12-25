using System.Collections.Generic;
using SWBF2Admin.Config;
namespace SWBF2Admin.Runtime.Announce
{
    [ConfigFileInfo(fileName: "./cfg/announce.xml"/*,template: "SWBF2Admin.Resources.cfg.announce.xml"*/)]
    public class AnnounceHandlerConfiguration
    {
        /// <summary>
        /// Enable announce broadcasting
        /// </summary>
        public bool Enable { get; set; } = false;

        /// <summary>
        /// Delay (in seconds) between broadcasts
        /// </summary>
        public int Interval { get; set; } = 360;

        /// <summary>
        /// List containing all announces
        /// </summary>
        public List<Announce> AnnounceList = new List<Announce>();
    }
}
