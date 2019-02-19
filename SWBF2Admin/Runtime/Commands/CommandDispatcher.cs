/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see<http://www.gnu.org/licenses/>.
 */
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

        public bool IsConsoleCommand(string cmd)
        {
            return cmd.StartsWith(commandPrefix);
        }

        public void HandleConsoleCommand(string cmd)
        {
            if (IsConsoleCommand(cmd))
            {
                HandleCommand(Player.SUPERUSER, cmd);
            }
        }

        public void LogChat(string playerName, string message)
        {
            Logger.Log(LogLevel.Info, "[CHAT] #{0}: {1}", playerName, message);
        }

        private void Rcon_ChatInput(object sender, EventArgs e)
        {
            ChatMessage msg = ((RconChatEventArgs)e).Message;
            if (IsConsoleCommand(msg.Message))
                HandleCommand(msg.PlayerName, msg.Message);
            else
                LogChat(msg.PlayerName, msg.Message);
        }
        private void HandleCommand(string name, string message)
        {
            List<Player> players = Core.Players.GetPlayers(name, false, true);
            if (players.Count != 1)
            {
                Logger.Log(LogLevel.Verbose,
                    "Player \"{0}\" could not be identified. Blocking access to \"{1}\"", name, message);
                return;
            }
            HandleCommand(players[0], message);
        }

        private void HandleCommand(Player player, string message)
        {
            int cidx = message.IndexOf(' ', commandPrefix.Length);
            string command = (cidx < 0 ? message.Substring(commandPrefix.Length) : message.Substring(commandPrefix.Length, cidx - commandPrefix.Length));
            string[] parameters = (cidx < 0 ? new string[0] : message.Substring(++cidx, message.Length - cidx).Split(' '));
            foreach (ChatCommand c in commandList)
            {
                if (c.Match(command, parameters))
                {
                    if (!c.HasPermission(player))
                    {
                        Logger.Log(LogLevel.Verbose,
                            "Player \"{0}\" doesnt have permission for \"{1}\"", player.Name, c.Alias);
                    }
                    else
                    {
                        if (c.Enabled)
                        {
                            Logger.Log(LogLevel.Verbose, "Running command \"{0}\", invoked by \"{1}\"", c.Alias, player.Name);
                            c.Handle(player, command, parameters);
                        }
                        else
                        {
                            Logger.Log(LogLevel.Verbose, "Command \"{0}\" (called by \"{1}\") is disabled - ignoring call.", c.Alias, player.Name);
                        }
                    }
                    return;
                }
            }

            Logger.Log(LogLevel.Verbose, "Player \"{0}\" issued unknown command \"{1}\"", player.Name, command);
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