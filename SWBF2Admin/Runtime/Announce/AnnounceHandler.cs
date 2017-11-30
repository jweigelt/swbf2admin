using System;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Announce
{
    class AnnounceHandler : ComponentBase
    {
        public event EventHandler OnAnnounce = null;
        private AnnounceConfiguration config;
        private DateTime lastAnnounce = DateTime.Now;
        private int currentIdx = 0;

        public AnnounceHandler(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            this.config = Core.Files.ReadConfig<AnnounceConfiguration>();
        }
        public override void OnServerStart()
        {
            EnableUpdates();
        }
        public override void OnServerStop()
        {
            DisableUpdates();
        }

        public void Update()
        {
            if ((DateTime.Now - lastAnnounce).TotalSeconds > config.Interval)
            {
                lastAnnounce = DateTime.Now;
                if (OnAnnounce != null)
                {
                    InvokeEvent(OnAnnounce, this, new AnnounceEventArgs(config.AnnounceList[currentIdx++]));
                }
            }
        }

    }
}