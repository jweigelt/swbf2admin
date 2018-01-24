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
using System.Reflection;

using SWBF2Admin.Structures.Attributes;
using SWBF2Admin.Utility;
namespace SWBF2Admin.Structures
{
    //[ConfigFileInfo(fileName:"./server/settings/ServerSettings.cfg", Template = )
    public class ServerSettings
    {
        private const string FILE_NAME = "/settings/ServerSettings.cfg";

        [ConfigSection(ConfigSection.GENERAL)]
        public string GameName { get; set; } = "New Server";

        [ConfigSection(ConfigSection.GENERAL)]
        public string Password { get; set; } = "";

        [ConfigSection(ConfigSection.GENERAL, true, false)]
        public string AdminPw { get; set; } = "";

        [ConfigSection(ConfigSection.GENERAL)]
        public string IP { get; set; } = "127.0.0.1";

        [ConfigSection(ConfigSection.GENERAL)]
        public ushort GamePort { get; set; } = 3658;

        [ConfigSection(ConfigSection.GENERAL)]
        public ushort RconPort { get; set; } = 4658;

        [ConfigSection(ConfigSection.GENERAL)]
        public ushort Tps { get; set; } = 30;

        [ConfigSection(ConfigSection.GENERAL)]
        public ushort PlayerLimit { get; set; } = 20;

        [ConfigSection(ConfigSection.GENERAL)]
        public ushort PlayerCount { get; set; } = 1;

        [ConfigSection(ConfigSection.GENERAL)]
        public bool Lan { get; set; } = false;

        [ConfigSection(ConfigSection.GENERAL, true, false)]
        public ushort Bandwidth { get; set; } = 6144;

        [ConfigSection(ConfigSection.GENERAL_KEEPDEFAULT)]
        public ushort VoiceMode { get; set; } = 3;

        [ConfigSection(ConfigSection.GENERAL)]
        public string NetRegion { get; set; } = "EU";

        [ConfigSection(ConfigSection.GENERAL_KEEPDEFAULT)]
        public string VideoStd { get; set; } = "NTSC";

        #region "Heroes"
        [ConfigSection(ConfigSection.GAME)]
        public bool Heroes { get; set; } = false;

        [ConfigSection(ConfigSection.GAME)]
        public ushort HrUnlock { get; set; } = 3;

        [ConfigSection(ConfigSection.GAME)]
        public ushort HrUnlockValue { get; set; } = 10;

        [ConfigSection(ConfigSection.GAME)]
        public ushort HrPlayer { get; set; } = 1;

        [ConfigSection(ConfigSection.GAME)]
        public ushort HrTeam { get; set; } = 1;

        [ConfigSection(ConfigSection.GAME)]
        public ushort HrRespawn { get; set; } = 90;
        #endregion

        #region "CON"
        [ConfigSection(ConfigSection.GAME)]
        public ushort ConTimeLimit { get; set; } = 0;
        [ConfigSection(ConfigSection.GAME)]
        public ushort ConReinforcements { get; set; } = 100;
        [ConfigSection(ConfigSection.GAME)]
        public ushort ConAiperTeam { get; set; } = 0;
        #endregion
        #region "CTF"
        [ConfigSection(ConfigSection.GAME)]
        public ushort CTFScoreLimit { get; set; } = 5;
        [ConfigSection(ConfigSection.GAME)]
        public ushort CTFTimeLimit { get; set; } = 0;
        [ConfigSection(ConfigSection.GAME)]
        public ushort CTFAiPerTeam { get; set; } = 0;
        #endregion
        #region "HUNT"
        [ConfigSection(ConfigSection.GAME)]
        public ushort HuntScoreLimit { get; set; } = 0;
        [ConfigSection(ConfigSection.GAME)]
        public ushort HunTimeLimit { get; set; } = 0;
        #endregion
        #region "ASS"
        [ConfigSection(ConfigSection.GAME)]
        public ushort AssScoreLimit { get; set; } = 0;
        [ConfigSection(ConfigSection.GAME)]
        public ushort AssReinforcements { get; set; } = 0;
        [ConfigSection(ConfigSection.GAME)]
        public ushort AssAiPerTeam { get; set; } = 0;
        #endregion
        #region "ELI"
        [ConfigSection(ConfigSection.GAME)]
        public ushort EliTimeLimit { get; set; } = 0;
        [ConfigSection(ConfigSection.GAME)]
        public ushort EliAiPerTeam { get; set; } = 0;
        #endregion

        [ConfigSection(ConfigSection.GAME)]
        public bool Shownames { get; set; } = true;

        [ConfigSection(ConfigSection.GAME)]
        public bool TeamDamage { get; set; } = true;

        [ConfigSection(ConfigSection.GAME)]
        public bool Awards { get; set; } = true;

        [ConfigSection(ConfigSection.GAME)]
        public bool AutoAssignTeams { get; set; } = false;

        [ConfigSection(ConfigSection.GAME)]
        public ushort Difficulty { get; set; } = 3;

        [ConfigSection(ConfigSection.GAME)]
        public ushort Spawn { get; set; } = 0;

        [ConfigSection(ConfigSection.GAME)]
        public ushort PreGameTime { get; set; } = 0;

        [ConfigSection(ConfigSection.GAME)]
        public ushort KickVoteThreshold { get; set; } = 0;

        [ConfigSection(ConfigSection.GAME)]
        public ushort TeamVoteThreshold { get; set; } = 0;

        [ConfigSection(ConfigSection.GAME)]
        public bool AimAssist { get; set; } = false;

        [ConfigSection(ConfigSection.MAPS)]
        public bool Randomize { get; set; } = false;

        //Attention: messy code ahead
        public static ServerSettings FromSettingsFile(AdminCore core, string path)
        {
            ServerSettings settings = new ServerSettings();
            string file = null;
            string fileName = path + FILE_NAME;

            if (!core.Files.FileExists(fileName))
            {
                return settings;
            }

            try
            {
                file = core.Files.ReadFileText(fileName);
                Logger.Log(LogLevel.Verbose, "Server settings saved.");
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Failed to read to file {0} ({1})", fileName, e.ToString());
                throw e;
            }

            file = file.Replace("\r\n", "\n");
            string[] rows = file.Split('\n');
            PropertyInfo[] props = typeof(ServerSettings).GetProperties();

            foreach (string r in rows)
            {
                if (r.Length < 1) continue;
                string[] dat = r.Split(' ');
                if (dat.Length < 2)
                {
                    Logger.Log(LogLevel.Warning, "Invalid server setting '{0}'. No value specified - skipping.", r);
                    continue;
                }

                bool found = false;
                string settingName = dat[0].Replace("/", "").ToLower();


                foreach (PropertyInfo p in props)
                {

                    if (p.Name.ToLower().Equals(settingName))
                    {

                        found = true;
                        if (p.PropertyType.IsEquivalentTo(typeof(bool)))
                            p.SetValue(settings, (dat[1].Equals("1")));
                        else if (p.PropertyType.IsEquivalentTo(typeof(ushort)))
                        {
                            ushort s = 0;
                            if (!ushort.TryParse(dat[1], out s))
                            {
                                Logger.Log(LogLevel.Warning, "Invalid server setting '{0}'. Expecting valid integer - skipping.", r);
                                break;
                            }
                            p.SetValue(settings, s);
                        }
                        else if (p.PropertyType.IsEquivalentTo(typeof(string)))
                        {
                            string val = "";
                            int idx_start = 0, idx_stop = 0;

                            if ((idx_start = r.IndexOf('"')) > 0)
                            {
                                if ((idx_stop = r.IndexOf('"', ++idx_start)) < 0)
                                {
                                    Logger.Log(LogLevel.Warning, "Invalid server setting '{0}'. Expected closing \" - skipping.", r);
                                    break;
                                }
                                val = r.Substring(idx_start, idx_stop - idx_start);
                            }
                            else
                            {
                                val = dat[1];
                            }

                            p.SetValue(settings, val);
                        }
                    }
                }
                if (!found)
                {
                    Logger.Log(LogLevel.Warning, "Unknown server setting '{0}' - ignoring it.", r);
                }
            }
            return settings;
        }

        public void WriteToFile(AdminCore core)
        {
            string file = "";
            string fileName = core.Server.ServerPath + FILE_NAME;

            PropertyInfo[] props = typeof(ServerSettings).GetProperties();
            foreach (PropertyInfo p in props)
            {
                file += "/" + p.Name.ToLower() + " ";

                if (p.PropertyType.IsEquivalentTo(typeof(bool)))
                    file += (((bool)p.GetValue(this)) ? "1" : "0");

                else if (p.PropertyType.IsEquivalentTo(typeof(ushort)))
                    file += ((ushort)p.GetValue(this)).ToString();

                else if (p.PropertyType.IsEquivalentTo(typeof(string)))
                    file += "\"" + (string)p.GetValue(this) + "\"";

                file += "\r\n";
            }

            try
            {
                core.Files.WriteFileText(fileName, file);
                Logger.Log(LogLevel.Verbose, "Server settings saved.");
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Failed to write to file {0} ({1]})", fileName, e.ToString());
            }
        }

        public void UpdateFrom(ServerSettings s, int type)
        {
            PropertyInfo[] props = typeof(ServerSettings).GetProperties();
            foreach (PropertyInfo p in props)
            {
                ConfigSection[] attr = (ConfigSection[])p.GetCustomAttributes(typeof(ConfigSection), false);
                if (attr.Length > 0)
                {
                    if ((attr[0].Type & type) > 0) p.SetValue(this, p.GetValue(s));
                }
            }
        }
    }
}
