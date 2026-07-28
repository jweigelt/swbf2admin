using SWBF2Admin.Config;
using SWBF2Admin.Utility;
using System;

namespace SWBF2Admin.Runtime.Watchdog
{
    public class EmptyRestart : ComponentBase
    {
        private DateTime lastSeenNotEmpty;
        private int restartThreshold;

        public EmptyRestart(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            UpdateInterval = config.EmptyRestartCheckInterval;
            restartThreshold = config.EmptyRestartThreshold;
        }

        public override void OnServerStart(EventArgs e)
        {
            EnableUpdates();
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
            else if ((DateTime.Now - lastSeenNotEmpty).TotalSeconds > restartThreshold)
            {
                Logger.Log(LogLevel.Info, "Server has been empty for {0} seconds - restarting it", restartThreshold.ToString());
                DisableUpdates();
                Core.Server.Restart();
            }
        }
    }
}