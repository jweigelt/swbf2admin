using System;
using System.Xml;
using System.Collections.Generic;

using SWBF2Admin.Structures;
using SWBF2Admin.Utility;
using SWBF2Admin.Runtime.ApplyMods;

using MoonSharp.Interpreter;
namespace SWBF2Admin.Runtime.Commands.Dynamic
{
    [MoonSharpUserData]
    public class LuaApi
    {
        [MoonSharpHidden]
        public const string GLOBALS_API = "api";
        [MoonSharpHidden]
        public const string FUNC_INIT = "init";
        [MoonSharpHidden]
        public const string FUNC_RUN = "run";

        private AdminCore core;
        private DynamicCommand command;

        [MoonSharpHidden]
        public LuaApi(AdminCore core, DynamicCommand command)
        {
            this.core = core;
            this.command = command;
        }

        //TODO: add more wrappers

        #region "Players"
        public List<Player> GetPlayers()
        {
            return core.Players.PlayerList;
        }
        public List<Player> FindPlayers(string exp, bool ignoreCase = true, bool exact = false)
        {
            return core.Players.GetPlayers(exp, ignoreCase, exact);
        }
        public void KickPlayer(Player player)
        {
            core.Players.Kick(player);
        }
        public void SwapPlayer(Player player)
        {
            core.Players.Swap(player);
        }
        public void Pm(string message, Player player, params string[] p)
        {
            core.Rcon.Pm(Util.FormatString(message, p), player);
        }

        public List<PlayerBan> GetBans(string playerExp, string adminExp, string reasonExp, bool expired, int banType, uint timestamp, int maxRows)
        {
            return core.Database.GetBans(playerExp, adminExp, reasonExp, expired, banType, timestamp, maxRows);
        }
        public void InsertBan(Player p, Player a, string reason, bool ip, int duration = -1)
        {
            PlayerBan b = null;
            BanType t = (ip ? BanType.IPAddress : BanType.Keyhash);
            if (duration < 0)
                b = new PlayerBan(p.Name, p.KeyHash, p.RemoteAddressStr, a.Name, reason, t, p.DatabaseId, a.DatabaseId);
            else
                b = new PlayerBan(p.Name, p.KeyHash, p.RemoteAddressStr, a.Name, reason, new TimeSpan(0, 0, 0, duration), t, p.DatabaseId, a.DatabaseId);
            core.Database.InsertBan(b);
        }

        #endregion

        #region "Game"
        public ServerInfo GetServerInfo() { return core.Game.LatestInfo; }
        public GameInfo GetGameInfo() { return core.Game.LatestGame; }
        #endregion

        #region "Rcon"
        public string SendCommand(string cmd, params string[] args)
        {
            return core.Rcon.SendCommand(cmd, args);
        }
        public void SendCommandNoResponse(string cmd, params string[] args)
        {
            core.Rcon.SendCommandNoResponse(cmd, args);
        }
        public void Say(string message, params string[] p)
        {
            core.Rcon.Say(Util.FormatString(message, p));
        }
        #endregion

        #region "I/O"
        public string GetConfig(string name)
        {
            foreach (XmlNode node in (XmlNode[])command.UserConfig)
            {
                if (node.Name.Equals(name)) return node.InnerText;
            }
            throw new NullReferenceException($"\"{name}\" was not declared.");
        }
        public string GetUsage() { return command.Usage; }
        public string GetAlias() { return command.Alias; }

        public const int LogLevel_Verbose = (int)LogLevel.Verbose;
        public const int LogLevel_Info = (int)LogLevel.Info;
        public const int LogLevel_Warning = (int)LogLevel.Warning;
        public const int LogLevel_Error = (int)LogLevel.Error;

        public void Log(int level, string message, params string[] p)
        {
            message = $"[LUA] [{command.Alias}] {message}";
            Logger.Log((LogLevel)level, message, p);
        }
        #endregion

        #region "Mods"
        public List<LvlMod> GetMods()
        {
            return core.Mods.Mods;
        }

        public void ApplyMod(LvlMod mod)
        {
            core.Mods.ApplyMod(mod);
        }

        public void RevertMod(LvlMod mod)
        {
            core.Mods.RevertMod(mod);
        }

        public void RevertAllMods()
        {
            core.Mods.RevertAll();
        }
        #endregion
    }
}