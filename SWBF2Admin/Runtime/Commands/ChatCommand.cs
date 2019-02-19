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

using System.Collections.Concurrent;
using SWBF2Admin.Utility;
using SWBF2Admin.Structures;

using System.Xml.Serialization;
using SWBF2Admin.Runtime.Permissions;

namespace SWBF2Admin.Runtime.Commands
{
    public abstract class ChatCommand
    {
        [XmlIgnore]
        public virtual Permission Permission { get; set; }

        public bool Enabled { get; set; } = true;
        public string Alias { get; set; } = "change me";
        public string Usage { get; set; } = "change me";

        private bool shouldSendToConsole;

        [XmlIgnore]
        public AdminCore Core { get; set; }

        public ChatCommand(string alias, string permissionName, string usage)
        {
            Alias = alias;
            Permission = Permission.FromName(permissionName);
            if (Permission == null)
            {
                Logger.Log(LogLevel.Error, "Command '{0}' is using an invalid permission name '{1}'", alias, permissionName);
            }
            Usage = usage;
        }
        public ChatCommand() { }

        public virtual bool Match(string command, string[] parameters)
        {
            return (command.ToLower().Equals(Alias.ToLower()));
        }

        public abstract bool Run(Player player, string commandLine, string[] parameters);

        public bool Handle(Player player, string commandLine, string[] parameters)
        {
            /*
             * For use in SendFormatted.
             * This is only reliable if commands cant be executed in parallel.
             * Ideally, the player would be send to each SendFormatted, but thats a major refactor that Im not sure
             * we want to do
             */
            shouldSendToConsole = player.isSuperuser();
            return Run(player, commandLine, parameters);
        }

        protected string FormatString(string message, params string[] tags)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (i + 1 >= tags.Length)
                    Logger.Log(LogLevel.Warning, "No value for parameter {0} specified. Ignoring it.", tags[i]);
                else
                    message = message.Replace(tags[i], tags[++i]);
            }
            return message;
        }

        protected void SendFormatted(string message, params string[] tags)
        {
            SendFormatted(message, null, tags);
        }

        protected void SendFormatted(string message, Player player, params string[] tags)
        {
            message = FormatString(message, tags);

            /*
             * Ideally this would *only* send to the console, but if we want this to work with webadmin,
             * then we have to send to both console and RCON to handle both when superuser came from console
             * and from webadmin. In the future webadmin wont use superuser, so we can fix this
             */
            if (shouldSendToConsole)
                Logger.Log(LogLevel.Info, message);

            if (player == null)
                Core.Rcon.Say(message);
            else
                Core.Rcon.Pm(message, player);
        }

        public virtual bool HasPermission(Player player)
        {
            if (player.isSuperuser())
            {
                return true;
            }
            return Core.Database.HasPermission(player, Permission);
        }
    }
}