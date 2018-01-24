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
using Newtonsoft.Json;
using SWBF2Admin.Utility;
using System.Collections.Generic;
namespace SWBF2Admin.Structures
{
    public class ServerMap
    {
        private enum MapFlags
        {
            GCWCon = (1 << 0),
            GCWCTF = (1 << 1),
            GCW1Flag = (1 << 2),
            GCWHunt = (1 << 3),
            GCWEli = (1 << 4),
            GCWAss = (1 << 5),

            CWCon = (1 << 10),
            CWCTF = (1 << 11),
            CW1Flag = (1 << 12),
            CWHunt = (1 << 13),
            CWEli = (1 << 14),
            CWAss = (1 << 15)
        }
        public ServerMap(long databaseId, string name, string niceName, long flags)
        {
            DatabaseId = databaseId;
            Name = name;
            NiceName = niceName;
            Flags = (int)flags;
        }

        public ServerMap() { }

        public long DatabaseId { get; }
        public string Name { get; set; }
        public string NiceName { get; set; }

        public int Flags { get; set; }

        [JsonIgnore]
        public virtual bool HasGCWCon { get { return (Flags & (int)MapFlags.GCWCon) > 0; } set { if (value) Flags |= (int)MapFlags.GCWCon; else Flags &= ~(int)MapFlags.GCWCon; } }
        [JsonIgnore]
        public virtual bool HasGCWCTF { get { return (Flags & (int)MapFlags.GCWCTF) > 0; } set { if (value) Flags |= (int)MapFlags.GCWCTF; else Flags &= ~(int)MapFlags.GCWCTF; } }
        [JsonIgnore]
        public virtual bool HasGCW1Flag { get { return (Flags & (int)MapFlags.GCW1Flag) > 0; } set { if (value) Flags |= (int)MapFlags.GCW1Flag; else Flags &= ~(int)MapFlags.GCW1Flag; } }
        [JsonIgnore]
        public virtual bool HasGCWHunt { get { return (Flags & (int)MapFlags.GCWHunt) > 0; } set { if (value) Flags |= (int)MapFlags.GCWHunt; else Flags &= ~(int)MapFlags.GCWHunt; } }
        [JsonIgnore]
        public virtual bool HasGCWEli { get { return (Flags & (int)MapFlags.GCWEli) > 0; } set { if (value) Flags |= (int)MapFlags.GCWEli; else Flags &= ~(int)MapFlags.GCWEli; } }
        [JsonIgnore]
        public virtual bool HasGCWAss { get { return (Flags & (int)MapFlags.GCWAss) > 0; } set { if (value) Flags |= (int)MapFlags.GCWAss; else Flags &= ~(int)MapFlags.GCWAss; } }

        [JsonIgnore]
        public virtual bool HasCWCon { get { return (Flags & (int)MapFlags.CWCon) > 0; } set { if (value) Flags |= (int)MapFlags.CWCon; else Flags &= ~(int)MapFlags.CWCon; } }
        [JsonIgnore]
        public virtual bool HasCWCTF { get { return (Flags & (int)MapFlags.CWCTF) > 0; } set { if (value) Flags |= (int)MapFlags.CWCTF; else Flags &= ~(int)MapFlags.CWCTF; } }
        [JsonIgnore]
        public virtual bool HasCW1Flag { get { return (Flags & (int)MapFlags.CW1Flag) > 0; } set { if (value) Flags |= (int)MapFlags.CW1Flag; else Flags &= ~(int)MapFlags.CW1Flag; } }
        [JsonIgnore]
        public virtual bool HasCWHunt { get { return (Flags & (int)MapFlags.CWHunt) > 0; } set { if (value) Flags |= (int)MapFlags.CWHunt; else Flags &= ~(int)MapFlags.CWHunt; } }
        [JsonIgnore]
        public virtual bool HasCWEli { get { return (Flags & (int)MapFlags.CWEli) > 0; } set { if (value) Flags |= (int)MapFlags.CWEli; else Flags &= ~(int)MapFlags.CWEli; } }
        [JsonIgnore]
        public virtual bool HasCWAss { get { return (Flags & (int)MapFlags.CWAss) > 0; } set { if (value) Flags |= (int)MapFlags.CWAss; else Flags &= ~(int)MapFlags.CWAss; } }

        public static List<string> ReadMapRotation(AdminCore core)
        {
            string sr = core.Files.ReadFileText(core.Config.ServerPath + "/settings/ServerRotation.cfg");
            string[] cs = sr.Split('/');
            List<string> res = new List<string>();

            foreach (string c in cs)
            {
                if (c.ToLower().StartsWith("addmap"))
                {
                    string[] cp = c.Split(' ');
                    if (cp.Length < 2)
                        Logger.Log(LogLevel.Warning, "Skipping invalid SeverRotation-Command '{0}'", cp);
                    else
                        res.Add(cp[1]);
                }
            }
            return res;
        }
        public static void SaveMapRotation(AdminCore core, List<string> maps)
        {
            string sr = string.Empty;
            foreach (string m in maps)
            {
                sr += string.Format("/addmap {0} ", m);
            }
            core.Files.WriteFileText(core.Config.ServerPath + "/settings/ServerRotation.cfg", sr);
        }

        //Attention: messy code ahead
        public static List<ServerMap> ReadServerMapConfig(AdminCore core)
        {
            Logger.Log(LogLevel.Info, "Starting map import...");
            string mapCfg = core.Files.ReadFileText(core.Config.ServerPath + "/settings/Maps.cfg");
            mapCfg = mapCfg.Replace("\r", "");
            string[] rows = mapCfg.Split('\n');

            int cnt = 0;
            List<ServerMap> maps = new List<ServerMap>();

            foreach (string r in rows)
            {
                //Read mapfile
                string mapName = r.Split(',')[0];
                if (mapName.Length < 9)
                {
                    Logger.Log(LogLevel.Warning, "Invalid map: '{0}' - skipping row", mapName);
                    break;
                }

                //Parse era
                char era = mapName[4];
                string mode = mapName.Substring(6);

                if (era != 'c' && era != 'g')
                {
                    Logger.Log(LogLevel.Warning, "Invalid era: '{0}' - skipping row", era.ToString());
                    break;
                }

                //Find map
                string name = mapName.Substring(0, 4);
                ServerMap m = null;
                foreach (ServerMap mp in maps)
                {
                    if (mp.Name.Equals(name))
                    {
                        m = mp;
                        break;
                    }
                }
                if (m == null)
                {
                    m = new ServerMap();
                    m.Name = name;
                    maps.Add(m);
                }

                //Parse mode
                bool gcw = (era == 'g');

                switch (mode)
                {
                    case "con":
                        if (gcw) m.HasGCWCon = true;
                        else m.HasCWCon = true;
                        break;
                    case "ctf":
                        if (gcw) m.HasGCWCTF = true;
                        else m.HasCWCTF = true;
                        break;
                    case "1flag":
                        if (gcw) m.HasGCW1Flag = true;
                        else m.HasCW1Flag = true;
                        break;
                    case "hunt":
                        if (gcw) m.HasGCWHunt = true;
                        else m.HasCWHunt = true;
                        break;
                    case "eli":
                        if (gcw) m.HasGCWEli = true;
                        else m.HasCWEli = true;
                        break;
                    case "ass":
                        if (gcw) m.HasGCWAss = true;
                        else m.HasCWAss = true;
                        break;
                }
                cnt++;
            }
            return maps;
        }

        public List<string> GetCWGameModes()
        {
            List<string> modes = new List<string>();
            if (HasCW1Flag) modes.Add("1flag");
            if (HasCWAss) modes.Add("ass");
            if (HasCWCon) modes.Add("con");
            if (HasCWCTF) modes.Add("ctf");
            if (HasCWEli) modes.Add("eli");
            if (HasCWHunt) modes.Add("hunt");

            return modes;
        }

        public List<string> GetGCWGameModes()
        {
            List<string> modes = new List<string>();
            if (HasGCW1Flag) modes.Add("1flag");
            if (HasGCWAss) modes.Add("ass");
            if (HasGCWCon) modes.Add("con");
            if (HasGCWCTF) modes.Add("ctf");
            if (HasGCWEli) modes.Add("eli");
            if (HasGCWHunt) modes.Add("hunt");

            return modes;
        }

        //TODO
        public bool HasMode(string mode)
        {
            return true;
        }


    
    }
}