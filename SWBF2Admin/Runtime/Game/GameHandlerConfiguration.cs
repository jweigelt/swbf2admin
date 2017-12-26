using System.Xml.Serialization;
using SWBF2Admin.Config;
namespace SWBF2Admin.Runtime.Game
{
    /// <summary>
    /// Configuration class for game-related stuff
    /// (templates etc. for any ingame events which are not player-related and thus not covered by playerhandler)
    /// </summary>
    [ConfigFileInfo(fileName: "./cfg/game.xml")]
    public class GameHandlerConfiguration
    {
        //[XmlIgnore]
        //public const string RESOURCE_NAME = "SWBF2Admin.Resources.cfg.game.xml";

        /// <summary>
        ///Delay between /status requests
        /// </summary>
        public int StatusUpdateInterval { get; set; } = 30000;

        public bool EnableGameStatsLogging { get; set; } = true;
    }
}