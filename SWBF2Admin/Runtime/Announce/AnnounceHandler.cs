using System;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Announce
{
    class AnnounceHandler : ComponentBase
    {
        public event EventHandler OnAnnounce = null;
        private AnnounceHandlerConfiguration config;
        private DateTime lastAnnounce = DateTime.Now;
        private int currentIdx = 0;

        public AnnounceHandler(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            this.config = Core.Files.ReadConfig<AnnounceHandlerConfiguration>();
        }

        public override void OnServerStart()
        {
            EnableUpdates();
        }

        public override void OnServerStop()
        {
            DisableUpdates();
        }

        public override void OnUpdate()
        {
            if ((DateTime.Now - lastAnnounce).TotalSeconds > config.Interval)
            {
                lastAnnounce = DateTime.Now;
                InvokeEvent(OnAnnounce, this, new AnnounceEventArgs(config.AnnounceList[currentIdx++]));
                if (currentIdx == config.AnnounceList.Count) currentIdx = 0;
            }
        }

    }
}