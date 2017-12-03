using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.Generic;

using SWBF2Admin.Structures;
using SWBF2Admin.Structures.Attributes;

namespace SWBF2Admin.Web.Pages
{
    class GeneralSettingsPage : AjaxPage
    {
        public GeneralSettingsPage(AdminCore core) : base(core, Constants.WEB_URL_SETTINGS_GENERAL, Constants.WEB_FILE_SETTINGS_GENERAL) { }

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
        }

        class GeneralSettingsResponse
        {
            public ServerSettings Settings { get; }
            public List<DeviceInfo> NetworkDevices { get; }
            public GeneralSettingsResponse(ServerSettings settings, List<DeviceInfo> networkDevices)
            {
                Settings = settings;
                NetworkDevices = networkDevices;
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

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            GeneralSettingsApiParams p = null;
            if ((p = TryJsonParse<GeneralSettingsApiParams>(ctx, postData)) == null) return;

            switch (p.Action)
            {
                case "general_get":
                    WebAdmin.SendHtml(ctx, ToJson(new GeneralSettingsResponse(Core.Server.Settings, GetNetworkDevices())));
                    break;

                case "general_set":
                    Core.Server.Settings.UpdateFrom(p.Settings, ConfigSection.GENERAL);
                    try
                    {
                        Core.Server.Settings.WriteToFile(Core);
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