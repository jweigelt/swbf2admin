using System;
using System.Text;
using System.Security.Cryptography;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Utility
{
    class Util
    {
        public static byte[] StrToBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public static string BytesToStr(byte[] buf, int count = -1)
        {
            if (count < 0) count = buf.Length;
            return Encoding.ASCII.GetString(buf, 0, count);
        }

        public static string Md5(string s)
        {
            byte[] hash = null;
            StringBuilder b = new StringBuilder();
            using (MD5 md5 = MD5.Create()) hash = md5.ComputeHash(Encoding.ASCII.GetBytes(s));
            for (int i = 0; i < hash.Length; i++) b.Append(hash[i].ToString("X2"));
            return b.ToString().ToLower();
        }

        public static string FormatString(string message, params string[] tags)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (i + 1 >= tags.Length)
                    Logger.Log(LogLevel.Warning, "No value for parameter {0} specified. Ignoring it.", tags[i]);
                else
                    message = message.Replace(tags[i], tags[++i]); //TODO: improve performance
            }
            return message;
        }

        public static string ParseVars(string s, AdminCore core)
        {
            //time, using .NET DateTime formatters
            int idx1, idx2;
            if ((idx1 = s.IndexOf("{t:")) > 0)
            {
                if ((idx2 = s.IndexOf("}", idx1 + 3)) > 0)
                {
                    string fmt = s.Substring(idx1 + 3, idx2 - (idx1 + 3));
                    s = s.Substring(0, idx1) + DateTime.Now.ToString(fmt) + s.Substring(++idx2);
                }
                else
                    Logger.Log(LogLevel.Error, "String format error: missing } (parsing \"{0}\")", s);
            }

            //server status
            ServerInfo info = core.Game.LatestInfo;
            s = FormatString(s,
                "{s:map}", info.CurrentMap,
                "{s:ff}", info.FFEnabled,
                "{s:gm}", info.GameMode,
                "{s:heroes}", info.Heroes,
                "{s:maxplayers}", info.MaxPlayers,
                "{s:nextmap}", info.NextMap,
                "{s:password}", info.Password,
                "{s:players}", info.Players,
                "{s:ip}", info.ServerIP,
                "{s:name}", info.ServerName,
                "{s:t1score}", info.Team1Score.ToString(),
                "{s:t2score}", info.Team2Score.ToString(),
                "{s:t1tickets}", info.Team1Tickets.ToString(),
                "{s:t2tickets}", info.Team1Score.ToString(),
                "{s:version}", info.Version);

            //game nr
            GameInfo game = core.Game.LatestGame;
            s = FormatString(s,
               "{g:nr}", game.DatabaseId.ToString());

            //banner for generous folks
            s = FormatString(s, "{banner}", $"{Constants.PRODUCT_NAME} v{Constants.PRODUCT_VERSION} ({Constants.PRODUCT_AUTHOR})");

            return s;
        }
    }
}