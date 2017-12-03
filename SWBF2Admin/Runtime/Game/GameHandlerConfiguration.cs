using System.Xml.Serialization;
namespace SWBF2Admin.Runtime.Game
{
    /// <summary>
    /// Configuration class for game-related stuff
    /// (templates etc. for any ingame events which are not player-related and thus not covered by playerhandler)
    /// </summary>
    public class GameHandlerConfiguration
    {      
        [XmlIgnore]
        public const string FILE_NAME = "./cfg/game.xml";

        //[XmlIgnore]
        //public const string RESOURCE_NAME = "SWBF2Admin.Resources.cfg.game.xml";
       
        public int StatusUpdateInterval { get; set; } = 30000;
    }
}