using SWBF2Admin.Utility;
using SWBF2Admin.Structures;

using System.Xml.Serialization;
using SWBF2Admin.Runtime.Permissions;

namespace SWBF2Admin.Runtime.Commands
{
    public abstract class ChatCommand
    {
        [XmlIgnore]
        public Permission Permission { get; set; }

        public bool Enabled { get; set; } = true;
        public string Alias { get; set; } = "change me";
        public string Usage { get; set; } = "change me";

        [XmlIgnore]
        public AdminCore Core { get; set; }

        public ChatCommand(string alias, Permission permission, string usage)
        {
            Alias = alias;
            Permission = permission;
            Usage = usage;
        }
        public ChatCommand() { }

        public virtual bool Match(string command, string[] parameters)
        {
            return (command.ToLower().Equals(Alias.ToLower()));
        }

        public abstract bool Run(Player player, string commandLine, string[] parameters);

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
            if (player == null) Core.Rcon.Say(message); else Core.Rcon.Pm(message, player);
        }
    }
}