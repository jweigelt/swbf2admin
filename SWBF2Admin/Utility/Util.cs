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
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Reflection;

using SWBF2Admin.Structures;
using System.Diagnostics;

namespace SWBF2Admin.Utility
{
    public class Util
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
            if (info != null)
            {
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
            }
            //game nr
            GameInfo game = core.Game.LatestGame;
            if (game != null)
            {
                s = FormatString(s,
                   "{g:nr}", game.DatabaseId.ToString());
            }
            //these db queries can be quite expensive -> only query if we really have to
            //TODO: figure something out to cache total points/kills/deaths
            if (s.Contains("{stats:totalpoints}")) s = FormatString(s, "{stats:totalpoints}", core.Database.GetTotalPoints().ToString());
            if (s.Contains("{stats:totalkills}")) s = FormatString(s, "{stats:totalkills}", core.Database.GetTotalKills().ToString());
            if (s.Contains("{stats:totaldeaths}")) s = FormatString(s, "{stats:totaldeaths}", core.Database.GetTotalDeaths().ToString());
            if (s.Contains("{stats:totalplayers}")) s = FormatString(s, "{stats:totalplayers}", core.Database.GetTotalPlayers().ToString());
            if (s.Contains("{stats:totalmatches}")) s = FormatString(s, "{stats:totalmatches}", core.Database.GetTotalMatches().ToString());

            //banner for generous folks
            s = FormatString(s, "{banner}", $"{GetProductName()} v{GetProductVersion()} ({GetProductAuthor()})");

            return s;
        }

        public static string GetProductName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name;
        }


        public static string GetProductVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public static string GetProductAuthor()
        {
            return "LeKeks, Yoni, AsLan, Alpha";
        }

        public static List<string> SegmentString(string str, int maxSegmentSize)
        {
            List<string> segments = new List<string>();
            for (int i = 0; i < str.Length; i += maxSegmentSize)
            {
                string segment = str.Substring(i, Math.Min(str.Length - i, maxSegmentSize));
                segments.Add(segment);
            }

            return segments;
        }

        public static string HtmlEncode(string html)
        {
            //TODO: verify that WebUtility.HtmlEncode() is effective enough
            return WebUtility.HtmlEncode(html);
        }

        public static int F2i(float f)
        {
            byte[] fb = BitConverter.GetBytes(f);
            return BitConverter.ToInt32(fb, 0);
        }

        public static float I2f(int i)
        {
            byte[] fb = BitConverter.GetBytes(i);
            return BitConverter.ToSingle(fb, 0);
        }
    }
}