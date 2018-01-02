using System;
using System.IO;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Config;
using SWBF2Admin.Structures;
using SWBF2Admin.Runtime.Rcon;
using SWBF2Admin.Runtime.Permissions;
using SWBF2Admin.Runtime.Commands.Admin;
using SWBF2Admin.Runtime.Commands.Map;
using SWBF2Admin.Runtime.Commands.Misc;
using SWBF2Admin.Runtime.Commands.Permissions;
using SWBF2Admin.Runtime.Commands.Dynamic;

using MoonSharp.Interpreter;

namespace SWBF2Admin.Runtime.Commands
{
    public class CommandDispatcher : ComponentBase
    {
        private const string DYNCOMMAND_DIR = "./cfg/dyncmd";

        private List<ChatCommand> commandList = new List<ChatCommand>();
        private string commandPrefix;
        private bool enableDynamic;

        public CommandDispatcher(AdminCore core) : base(core) { }
        public override void Configure(CoreConfiguration config)
        {
            commandPrefix = config.CommandPrefix;
            enableDynamic = config.CommandEnableDynamic;
        }
        public override void OnInit()
        {
            //Commands can be registered here
            RegisterCommand<CmdSwap>();
            RegisterCommand<CmdKick>();
            RegisterCommand<CmdBan>();
            RegisterCommand<CmdIpBan>();
            RegisterCommand<CmdTempban>();
            RegisterCommand<CmdTempIpBan>();

            RegisterCommand<CmdMap>();
            RegisterCommand<CmdAddMap>();
            RegisterCommand<CmdRemoveMap>();
            RegisterCommand<CmdSetNextMap>();

            RegisterCommand<CmdPutGroup>();
            RegisterCommand<CmdRmGroup>();
            RegisterCommand<CmdGimmeAdmin>();

            RegisterCommand<CmdApplyMods>();

            Permission.InitPermissions(Core.Database.GetPermissions());

            if (enableDynamic)
            {
                UserData.RegisterAssembly();
                LoadDynCommands();
            }
            Core.Rcon.ChatInput += new EventHandler(Rcon_ChatInput);
        }

        private void Rcon_ChatInput(object sender, EventArgs e)
        {
            RconChatEventArgs ce = (RconChatEventArgs)e;
            if (ce.Message.StartsWith(commandPrefix))
                HandleCommand(ce.Name, ce.Message);
            else
                Logger.Log(LogLevel.Info, "[CHAT] #{0}: {1}", ce.Name, ce.Message);
        }
        private void HandleCommand(string name, string message)
        {
            int cidx = message.IndexOf(' ', commandPrefix.Length);
            string command = (cidx < 0 ? message.Substring(commandPrefix.Length) : message.Substring(commandPrefix.Length, cidx - commandPrefix.Length));
            string[] parameters = (cidx < 0 ? new string[0] : message.Substring(++cidx, message.Length - cidx).Split(' '));

            foreach (ChatCommand c in commandList)
            {
                if (c.Match(command, parameters))
                {
                    List<Player> players = Core.Players.GetPlayers(name, false, true);
                    if (players.Count != 1)
                    {

                        Logger.Log(LogLevel.Verbose,
                            "Player \"{0}\" could not be identified. Blocking access to \"{1}\"", name, c.Alias);
                    }
                    // TODO Use permissions
                    else if (!c.HasPermission(players[0]))
                    {
                        Logger.Log(LogLevel.Verbose,
                            "Player \"{0}\" doesnt have permission for \"{1}\"", players[0].Name, c.Alias);
                    }
                    else
                    {
                        if (c.Enabled)
                        {
                            Logger.Log(LogLevel.Verbose, "Running command \"{0}\", invoked by \"{1}\"", c.Alias, name);
                            c.Run(players[0], command, parameters);
                        }
                        else
                        {
                            Logger.Log(LogLevel.Verbose, "Command \"{0}\" (called by \"{1}\") is disabled - ignoring call.", c.Alias, name);
                        }
                    }
                    return;
                }
            }

            Logger.Log(LogLevel.Verbose, "Player \"{0}\" issued unknown command \"{1}\"", name, command);
        }

        private void RegisterCommand<T>() where T : ChatCommand
        {
            ChatCommand c = Core.Files.ReadConfig<T>();
            c.Core = Core;
            commandList.Add(c);
        }

        private void LoadDynCommands()
        {
            DirectoryInfo[] dirs = Core.Files.GetDirectories(DYNCOMMAND_DIR);
            foreach (DirectoryInfo dir in dirs)
            {
                try
                {
                    DynamicCommand c = Core.Files.ReadConfig<DynamicCommand>($"{dir.FullName}/{dir.Name}.xml");
                    c.Init(Core, dir);
                    commandList.Add(c);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, "Couldn't load dynamic command \"{0}\" ({1})- disabling it.", dir.Name, e.Message);
                }
            }

        }
    }
}