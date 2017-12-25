using System.Collections.Generic;
using SWBF2Admin.Runtime.Permissions;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime.Commands.Admin
{
    public abstract class MapCommand : ChatCommand
    {
        public string OnNoMode { get; set; } = "Map {map_name} doesn't have {mode}. Available are: {available}";
        public string OnInvalidMode { get; set; } = "Incorrect gamemode format, use \"era_mode\". Example: c_con";
        public string OnTooMany { get; set; } = "Too many maps {count} found.";
        public string OnMultiple { get; set; } = "Multiple maps found: {maps}";
        public string OnSyntaxError { get; set; } = "No map / gamemode specified. Usage: {usage}";
        public string OnNoMapFound { get; set; } = "No map matching '{expression}' could be found.";

        public string Separator { get; set; } = ", ";
        public string MapFormat { get; set; } = "{nicename}: {name} ({modes})";
        public int MaxMapListLen { get; set; } = 5;
        public bool SearchNiceName { get; set; } = true;

        public MapCommand(string alias, Permission permission) : base(alias, permission, $"{alias} <map> <gamemode>") { }
        public abstract bool AffectMap(ServerMap map, string mode, Player player, string commandLine, string[] parameters, int paramIdx);

        public override bool Run(Player player, string commandLine, string[] parameters)
        {
            if (parameters.Length < 1)
            {
                SendFormatted(OnSyntaxError, "{usage}", Usage);
                return false;
            }

            string shortExp = (parameters[0].Length > 3 ? parameters[0].Substring(0, 3) : parameters[0]);
            string gamemode;

            if (parameters.Length < 2)
            {
                if (parameters[0].Contains("_"))
                {
                    gamemode = parameters[0].Split('_')[1];
                }
                else
                {
                    SendFormatted(OnSyntaxError, "{usage}", Usage);
                    return false;
                }
            }
            else
            {
                gamemode = parameters[1];
            }

            if (!CheckMode(gamemode))
            {
                SendFormatted(OnInvalidMode, "{expression}", gamemode);
                return false;
            }

            List<ServerMap> matchingMaps = Core.Database.GetMaps(shortExp, (SearchNiceName ? parameters[0] : ""));
            if (matchingMaps.Count == 0)
            {
                SendFormatted(OnNoMapFound, "{expression}", parameters[0]);
                return false;
            }

            if (matchingMaps.Count > 1)
            {
                if (matchingMaps.Count > MaxMapListLen)
                {
                    SendFormatted(OnTooMany, "{count}", matchingMaps.ToString());
                }
                else
                {
                    string mapList = string.Empty;
                    foreach (ServerMap m in matchingMaps)
                    {
                        mapList += FormatString(MapFormat, "{nicename}", m.NiceName, "{name}", m.Name, "{modes}", GetModes(m));
                        mapList += Separator;
                    }
                    SendFormatted(OnMultiple, "{count}", matchingMaps.ToString(), "{maps}", mapList);
                }
                return false;
            }
            ServerMap map = matchingMaps[0];

            if (!map.HasMode(gamemode))
            {
                SendFormatted(OnNoMode, "{map_name}", map.Name, "{map_nicename}", "{available}", map.NiceName, GetModes(map));
                return false;
            }

            return AffectMap(matchingMaps[0], gamemode, player, commandLine, parameters, 1);
        }

        private bool CheckMode(string mode)
        {
            //TODO: I'm not sure if modmaps use different formats so era and gamemode aren't checked
            return ((mode.Length == 5 || mode.Length == 6) && mode.Contains("_"));
        }

        private string GetModes(ServerMap map)
        {
            return string.Join(Separator, map.GetGCWGameModes().ToArray())
             + Separator + string.Join(Separator, map.GetCWGameModes().ToArray());
        }
    }
}