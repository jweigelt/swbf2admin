using System.Net;
using System.Collections.Generic;

using SWBF2Admin.Structures;
using System.Threading;
using System;

namespace SWBF2Admin.Web.Pages
{
    class MapSettingsPage : AjaxPage
    {

        Mutex sRMtx = new Mutex();
        class MapApiParams : ApiRequestParams
        {
            public List<string> Maps { get; set; }
        }

        class MapSaveResponse
        {
            public bool Ok { get; set; }
            public string Error { get; set; }
            public MapSaveResponse(Exception e)
            {
                Ok = false;
                Error = e.Message;
            }
            public MapSaveResponse()
            {
                Ok = true;
            }
        }

        class MapRotResponse
        {
            public bool Ok { get; set; }
            public List<string> Maps { get; set; }
            public string Error { get; set; }

            public MapRotResponse(List<string> maps)
            {
                Ok = true;
                Maps = maps;
                Error = string.Empty;
            }

            public MapRotResponse(Exception e)
            {
                Ok = false;
                Maps = null;
                Error = e.Message;
            }
        }

        public MapSettingsPage(AdminCore core) : base(core, "/settings/maps", "maps.htm") { }

        public override void HandleGet(HttpListenerContext ctx, WebUser user)
        {
            ReturnTemplate(ctx);
        }

        public override void HandlePost(HttpListenerContext ctx, WebUser user, string postData)
        {
            MapApiParams p = null;
            if ((p = TryJsonParse<MapApiParams>(ctx, postData)) == null) return;

            switch (p.Action)
            {
                case "maps_installed":
                    List<ServerMap> mapList = Core.Database.GetMaps();
                    WebAdmin.SendHtml(ctx, ToJson(mapList));
                    break;

                case "maps_save":
                    WebAdmin.SendHtml(ctx, ToJson(SaveMapRot(p.Maps)));
                    break;

                case "maps_rotation":
                    WebAdmin.SendHtml(ctx, ToJson(GetMapRotation()));
                    break;
            }
        }

        private MapSaveResponse SaveMapRot(List<string> mapRot)
        {
            MapSaveResponse r;
            sRMtx.WaitOne();
            try
            {
                ServerMap.SaveMapRotation(Core, mapRot);
                r = new MapSaveResponse();
            }
            catch (Exception e)
            {
                r = new MapSaveResponse(e);
            }
            finally
            {
                sRMtx.ReleaseMutex();
            }
            return r;
        }

        private MapRotResponse GetMapRotation()
        {
            MapRotResponse r;
            sRMtx.WaitOne();
            try
            {
                r = new MapRotResponse(ServerMap.ReadMapRotation(Core));
            }
            catch (Exception e)
            {
                r = new MapRotResponse(e);
            }
            finally
            {
                sRMtx.ReleaseMutex();
            }

            return r;
        }

    }
}