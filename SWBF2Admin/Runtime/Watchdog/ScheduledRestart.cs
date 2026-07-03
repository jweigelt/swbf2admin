using SWBF2Admin.Config;
using SWBF2Admin.Utility;
using System;

namespace SWBF2Admin.Runtime.Watchdog
{
    public class ScheduledRestart : ComponentBase
    {
        private ScheduleConfiguration config;
        private DateTime serverStartTime;
        private DateTime lastAnnouncement;
        private bool restartPending;
        private bool isRestarting;

        public ScheduledRestart(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            ReloadConfig();
        }

        /// <summary>
        /// Re-reads schedule.xml (called by the WebAdmin after saving) so changes apply on the next server start
        /// </summary>
        public void ReloadConfig()
        {
            config = Core.Files.ReadConfig<ScheduleConfiguration>();
            UpdateInterval = config.CheckInterval;
        }

        public override void OnInit()
        {
            base.OnInit();
            //restart between maps so we don't kick players mid-game
            Core.Rcon.GameEnded += new EventHandler(Server_GameEnded);
        }

        public override void OnServerStart(EventArgs e)
        {
            restartPending = false;
            isRestarting = false;
            serverStartTime = DateTime.Now;
            lastAnnouncement = DateTime.Now;
            if (config.EnableScheduledRestart) EnableUpdates();
        }

        public override void OnServerStop()
        {
            DisableUpdates();
        }

        protected override void OnUpdate()
        {
            if (!restartPending && (DateTime.Now - serverStartTime).TotalSeconds > config.RestartThreshold)
            {
                Logger.Log(LogLevel.Info, "Server has been running for {0} seconds - scheduling a restart for the next map change", config.RestartThreshold.ToString());
                restartPending = true;
                lastAnnouncement = DateTime.MinValue;
            }

            if (restartPending) Announce();
        }

        private void Announce()
        {
            if (!config.EnableRestartAnnouncement || string.IsNullOrEmpty(config.RestartAnnouncement)) return;
            if (Core.Players.PlayerList.Count < 1) return;
            if ((DateTime.Now - lastAnnouncement).TotalSeconds < config.AnnouncementInterval) return;

            Core.Rcon.Say(config.RestartAnnouncement);
            lastAnnouncement = DateTime.Now;
        }

        private void Server_GameEnded(object sender, EventArgs e)
        {
            if (restartPending && !isRestarting)
            {
                Logger.Log(LogLevel.Info, "Map ended - performing scheduled restart");
                isRestarting = true; //make sure we don't try to restart twice
                Core.Server.Restart();
            }
        }
    }
}
