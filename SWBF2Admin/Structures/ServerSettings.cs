using System;
using System.Reflection;

using SWBF2Admin.Utility;
namespace SWBF2Admin.Structures
{
    class ServerSettings
    {
        private const string FILE_NAME = "/settings/ServerSettings.cfg";

        public string GameName { get; set; } = "New Server";
        public string Password { get; set; } = "";
        public string AdminPw { get; set; } = "";
        public string IP { get; set; } = "127.0.0.1";
        public ushort GamePort { get; set; } = 3658;
        public ushort RconPort { get; set; } = 4658;
        public ushort Tps { get; set; } = 30;
        public ushort PlayerLimit { get; set; } = 20;
        public ushort PlayerCount { get; set; } = 1;
        public bool Lan { get; set; } = false;
        public ushort Bandwidth { get; set; } = 6144;
        public ushort VoiceMode { get; set; } = 3;
        public string NetRegion { get; set; } = "EU";
        public string VideoStd { get; set; } = "NTSC";
        public bool Heroes { get; set; } = false;
        public bool Awards { get; set; } = true;
        public ushort HrUnlock { get; set; } = 3;
        public ushort HrUnlockValue { get; set; } = 10;
        public ushort HrPlayer { get; set; } = 8;
        public ushort HrTeam { get; set; } = 0;
        public ushort HrRespawn { get; set; } = 90;
        public ushort ConTimeLimit { get; set; } = 0;
        public ushort CTFTimeLimit { get; set; } = 0;
        public ushort HunTimeLimit { get; set; } = 0;
        public ushort CTFScoreLimit { get; set; } = 5;
        public ushort AssScoreLimit { get; set; } = 0;
        public ushort HuntScoreLimit { get; set; } = 0;
        public ushort ConReinforcements { get; set; } = 100;
        public ushort AssReinforcements { get; set; } = 0;
        public bool TeamDamage { get; set; } = true;
        public bool AutoAssignTeams { get; set; } = false;
        public bool Shownames { get; set; } = true;
        public bool Randomize { get; set; } = false;
        public ushort Difficulty { get; set; } = 3;
        public ushort Spawn { get; set; } = 0;
        public ushort PreGameTime { get; set; } = 0;
        public ushort KickVoteThreshold { get; set; } = 0;
        public ushort TeamVoteThreshold { get; set; } = 0;
        public ushort ConAiperTeam { get; set; } = 0;
        public ushort CTFAiPerTeam { get; set; } = 0;
        public ushort AssAiPerTeam { get; set; } = 0;
        public bool AimAssist { get; set; } = false;

        //Attention: messy code ahead
        public static ServerSettings FromSettingsFile(AdminCore core, string path)
        {
            ServerSettings settings = new ServerSettings();
            string file = null;
            string fileName = path + FILE_NAME;
            try
            {
                file = core.Files.ReadFileText(fileName);
                Logger.Log(LogLevel.Verbose, "Server settings saved.");
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Failed to read to file {0} ({1]})", fileName, e.ToString());
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
    }
}
