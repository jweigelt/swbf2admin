using System.Xml.Serialization;
using SWBF2Admin.Config;
namespace SWBF2Admin.Runtime.Players
{
    /// <summary>
    /// Configuration class for player-related stuff
    /// (templates etc.)
    /// </summary>
    [ConfigFileInfo(fileName: "./cfg/players.xml")]
    public class PlayerHandlerConfiguration
    {

        public string OnPlayerAutoKickBanned { get; set; } = "Auto-kicking {player} - player banned.";

        /// <summary>
        ///Delay between /players requests
        /// </summary>
        public int PlayersUpdateInterval { get; set; } = 5000;

        public bool EnablePlayerStatsLogging { get; set; } = true;
    }
}