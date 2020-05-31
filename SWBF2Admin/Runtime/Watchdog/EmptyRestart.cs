using System;
using SWBF2Admin.Config;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Runtime.Watchdog
{
    public class EmptyRestart : ComponentBase
    {
        private DateTime lastSeenNotEmpty;
        private int restartThreshold;
        private bool isRestarting;

        public EmptyRestart(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            UpdateInterval = config.EmptyRestartCheckInterval;
            restartThreshold = config.EmptyRestartThreshold;
        }

        public override void OnInit()
        {
            base.OnInit();
        }

        public override void OnServerStart(EventArgs e)
        {
            EnableUpdates();
            isRestarting = false;
            lastSeenNotEmpty = DateTime.Now;
        }

        public override void OnServerStop()
        {
            DisableUpdates();
        }

        protected override void OnUpdate()
        {
            if (Core.Players.PlayerList.Count > 0)
            {
                lastSeenNotEmpty = DateTime.Now;
            }
            else if (!isRestarting && (DateTime.Now - lastSeenNotEmpty).TotalSeconds > restartThreshold)
            {
                Logger.Log(LogLevel.Info, "Server has been empty for {0} seconds - restarting it", restartThreshold.ToString());
                Core.Server.Restart();
                isRestarting = true; //make sure we don't try to restart twice
            }
        }
    }
}