using System;
using System.IO;
using System.Collections.Generic;

using SWBF2Admin.Utility;
using SWBF2Admin.Config;
using SWBF2Admin.Structures;
using SWBF2Admin.Runtime.Rcon;
using SWBF2Admin.Runtime.Commands.Admin;
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

        public CommandDispatcher(AdminCore core) : base(core)
        {
        }
        public override void Configure(CoreConfiguration config)
        {
            commandPrefix = config.CommandPrefix;
            enableDynamic = config.CommandEnableDynamic;
        }
        public override void OnInit()
        {
            //Commands can be registered here
            RegisterCommand<CmdKick>();
            RegisterCommand<CmdTempban>();
            RegisterCommand<CmdAddMap>();
            if (enableDynamic)
            {
                UserData.RegisterAssembly();
                LoadDynCommands();
            }
            Core.Rcon.ChatInput += new EventHandler(Rcon_ChatInput);
        }

        private void RegisterCommand<T>() where T : ChatCommand
        {
            ChatCommand c = Core.Files.ReadConfig<T>();
            c.Core = Core;
            commandList.Add(c);
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
                    if (players.Count == 1)
                    {
                        //permissions could be checked here
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
                    else
                    {
                        Logger.Log(LogLevel.Verbose, "Player \"{0}\" could not be identified. Blocking access to \"{1}\"", name, c.Alias);
                    }
                    return;
                }
            }

            Logger.Log(LogLevel.Verbose, "Player \"{0}\" issued unknown command \"{1}\"", name, command);
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
                    Logger.Log(LogLevel.Warning, "Couldn't load dynamic command \"{0}\" - disabling it.", dir.Name);
                }
            }

        }
    }
}