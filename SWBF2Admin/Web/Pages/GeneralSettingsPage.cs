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
using SWBF2Admin.Gameserver;
using SWBF2Admin.Runtime.Watchdog;
using SWBF2Admin.Structures;
using SWBF2Admin.Structures.Attributes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace SWBF2Admin.Web.Pages
{
    class GeneralSettingsPage : AjaxPage
    {
        public GeneralSettingsPage(AdminCore core) : base(core, "/settings/general", "general.htm") { }

        class DeviceInfo
        {
            public string Name { get; set; }
            public string IPAddress { get; set; }
            public DeviceInfo(string name, string ipAddress)
            {
                Name = name;
                IPAddress = ipAddress;
            }
        }

        class GeneralSettingsApiParams : ApiRequestParams
        {
            public ServerSettings Settings { get; set; }
            public bool EnableScheduledRestart { get; set; }
            public int RestartThresholdMinutes { get; set; }
            public bool EnableRestartAnnouncement { get; set; }
            public int RestartCountdownMinutes { get; set; }
            public int AnnouncementIntervalSeconds { get; set; }
        }

        class GeneralSettingsResponse
        {
            public ServerSettings Settings { get; }
            public List<DeviceInfo> NetworkDevices { get; }
            public bool EnableScheduledRestart { get; }
            public int RestartThresholdMinutes { get; }
            public bool EnableRestartAnnouncement { get; }
            public int RestartCountdownMinutes { get; }
            public int AnnouncementIntervalSeconds { get; }
            public GeneralSettingsResponse(ServerSettings settings, List<DeviceInfo> networkDevices, ScheduleConfiguration schedule)
            {
                Settings = settings;
                NetworkDevices = networkDevices;
                EnableScheduledRestart = schedule.EnableScheduledRestart;
                RestartThresholdMinutes = schedule.RestartThresholdMinutes;
                EnableRestartAnnouncement = schedule.EnableRestartAnnouncement;
                RestartCountdownMinutes = schedule.RestartCountdownMinutes;
                AnnouncementIntervalSeconds = schedule.AnnouncementInterval;
            }
        }

        class GeneralSettingsSaveResponse
        {
            public bool Ok { get; set; }
            public string Error { get; set; }
            public GeneralSettingsSaveResponse(Exception e)
            {
                Ok = false;
                Error = e.Message;
            }
            public GeneralSettingsSaveResponse()
            {
                Ok = true;
            }

        }
        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx);
        }



        private int F2i(float f)
        {
            byte[] fb = BitConverter.GetBytes(f);
            return BitConverter.ToInt32(fb, 0);
        }

        private float I2f(int i)
        {
            byte[] fb = BitConverter.GetBytes(i);
            return BitConverter.ToSingle(fb, 0);
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            GeneralSettingsApiParams p = null;
            if ((p = TryJsonParse<GeneralSettingsApiParams>(ctx, postData)) == null) return;

            switch (p.Action)
            {
                case "general_get":
                    ServerSettings s = Core.Server.Settings;
                    ScheduleConfiguration schedule = Core.Files.ReadConfig<ScheduleConfiguration>();
                    WebAdmin.SendHtml(ctx, ToJson(new GeneralSettingsResponse(s, GetNetworkDevices(), schedule)));
                    break;

                case "general_set":
                    WebServer.LogAudit(user, "modified general settings");
                    var changes = Core.Server.Settings.UpdateFrom(p.Settings, ConfigSection.GENERAL);

                    if (Core.Config.EnableRuntime && Core.Server.Status == ServerStatus.Online)
                    {
                        Core.Scheduler.PushTask(() => Core.Rcon.UpdateServerSettings(changes));
                    }

                    try
                    {
                        //Load existing config first to keep the announcement text
                        ScheduleConfiguration scheduleCfg = Core.Files.ReadConfig<ScheduleConfiguration>();
                        scheduleCfg.EnableScheduledRestart = p.EnableScheduledRestart;
                        scheduleCfg.RestartThresholdMinutes = p.RestartThresholdMinutes;
                        scheduleCfg.EnableRestartAnnouncement = p.EnableRestartAnnouncement;
                        scheduleCfg.RestartCountdownMinutes = p.RestartCountdownMinutes;
                        scheduleCfg.AnnouncementInterval = p.AnnouncementIntervalSeconds;

                        Core.Server.Settings.WriteToFile(Core);
                        Core.Files.WriteConfig(scheduleCfg);
                        if (Core.Config.EnableRuntime) Core.Scheduler.PushTask(() => Core.Schedule.ReloadConfig());
                        WebAdmin.SendHtml(ctx, ToJson(new GeneralSettingsSaveResponse()));
                    }
                    catch (Exception e)
                    {
                        WebAdmin.SendHtml(ctx, ToJson(new GeneralSettingsSaveResponse(e)));
                    }
                    break;
            }
        }

        private List<DeviceInfo> GetNetworkDevices()
        {
            List<DeviceInfo> res = new List<DeviceInfo>();
            foreach (NetworkInterface iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    iface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {

                    foreach (UnicastIPAddressInformation info in iface.GetIPProperties().UnicastAddresses)
                    {
                        if (info.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            res.Add(new DeviceInfo(iface.Description, info.Address.ToString()));
                    }
                }
            }
            return res;
        }

    }
}
