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
using SWBF2Admin.Runtime.ApplyMods;
using SWBF2Admin.Runtime.ProcessMods;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace SWBF2Admin.Web.Pages
{
    class ModsPage : AjaxPage
    {
        private const string TYPE_FILE = "file";
        private const string TYPE_PROCESS = "process";

        private Mutex modMtx = new Mutex();

        class ModsApiParams : ApiRequestParams
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public bool Enabled { get; set; }
        }

        class ModInfo
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public bool Enabled { get; set; }

            public ModInfo(string type, string name, bool enabled)
            {
                Type = type;
                Name = name;
                Enabled = enabled;
            }
        }

        class ModsListResponse
        {
            public bool Ok { get; set; }
            public List<ModInfo> Mods { get; set; }
            public string Error { get; set; }

            public ModsListResponse(List<ModInfo> mods)
            {
                Ok = true;
                Mods = mods;
                Error = string.Empty;
            }

            public ModsListResponse(Exception e)
            {
                Ok = false;
                Mods = null;
                Error = e.Message;
            }
        }

        class ModsToggleResponse
        {
            public bool Ok { get; set; }
            public string Error { get; set; }

            public ModsToggleResponse()
            {
                Ok = true;
                Error = string.Empty;
            }

            public ModsToggleResponse(Exception e)
            {
                Ok = false;
                Error = e.Message;
            }
        }

        public ModsPage(AdminCore core) : base(core, "/live/mods", "mods.htm") { }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx);
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            ModsApiParams p = null;
            if ((p = TryJsonParse<ModsApiParams>(ctx, postData)) == null) return;

            switch (p.Action)
            {
                case "mods_list":
                    WebAdmin.SendHtml(ctx, ToJson(GetMods()));
                    break;

                case "mods_toggle":
                    WebServer.LogAudit(user, "{0} mod \"{1}\" ({2})",
                        p.Enabled ? "enabled" : "disabled", p.Name, p.Type);
                    WebAdmin.SendHtml(ctx, ToJson(ToggleMod(p)));
                    break;
            }
        }

        private ModsListResponse GetMods()
        {
            ModsListResponse r;
            modMtx.WaitOne();
            try
            {
                List<ModInfo> mods = new List<ModInfo>();

                foreach (LvlMod mod in Core.Mods.Mods)
                    mods.Add(new ModInfo(TYPE_FILE, mod.Name, mod.Enabled));

                foreach (ProcessMod mod in Core.BF2.Mods)
                    mods.Add(new ModInfo(TYPE_PROCESS, mod.Name, mod.Enabled));

                r = new ModsListResponse(mods);
            }
            catch (Exception e)
            {
                r = new ModsListResponse(e);
            }
            finally
            {
                modMtx.ReleaseMutex();
            }
            return r;
        }

        private ModsToggleResponse ToggleMod(ModsApiParams p)
        {
            ModsToggleResponse r;
            modMtx.WaitOne();
            try
            {
                if (TYPE_FILE.Equals(p.Type))
                {
                    ToggleFileMod(p.Name, p.Enabled);
                }
                else if (TYPE_PROCESS.Equals(p.Type))
                {
                    ToggleProcessMod(p.Name, p.Enabled);
                }
                else
                {
                    throw new Exception($"Unknown mod type \"{p.Type}\".");
                }
                r = new ModsToggleResponse();
            }
            catch (Exception e)
            {
                r = new ModsToggleResponse(e);
            }
            finally
            {
                modMtx.ReleaseMutex();
            }
            return r;
        }

        private void ToggleFileMod(string name, bool enabled)
        {
            LvlMod mod = Core.Mods.Mods.Find(m => m.Name == name);
            if (mod == null) throw new Exception($"File mod \"{name}\" not found.");

            mod.Enabled = enabled;
            Core.Mods.SaveConfig();

            if (enabled) Core.Mods.ApplyMod(mod);
            else Core.Mods.RevertMod(mod);

            if (Core.Server.Status == ServerStatus.Online)
                Core.Rcon.Say($"{(enabled ? "Applied" : "Reverted")} mod {mod.Name}");
        }

        private void ToggleProcessMod(string name, bool enabled)
        {
            ProcessMod mod = Core.BF2.Mods.Find(m => m.Name == name);
            if (mod == null) throw new Exception($"Process mod \"{name}\" not found.");

            mod.Enabled = enabled;
            mod.ApplyOnStart = enabled;

            Core.BF2.SaveConfig();

            //Only writable while attached to a running game
            if (Core.Server.Status == ServerStatus.Online && Core.BF2.ProcessOpened)
            {
                if (enabled) Core.BF2.ApplyMod(mod);
                else Core.BF2.RevertMod(mod);
            }
        }
    }
}
