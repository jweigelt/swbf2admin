using System;
using SWBF2Admin.Config;

namespace SWBF2Admin.Runtime.Announce
{
    public class AnnounceHandler : ComponentBase
    {
        /// <summary>
        /// Invoked when a announce has to be broadcasted
        /// </summary>
        public event EventHandler Broadcast = null;

        /// <summary>
        /// configuration object
        /// </summary>
        private AnnounceHandlerConfiguration config;

        /// <summary>
        /// index of current announce
        /// </summary>
        private int currentIdx = 0;

        public AnnounceHandler(AdminCore core) : base(core) { }

        public override void Configure(CoreConfiguration config)
        {
            this.config = Core.Files.ReadConfig<AnnounceHandlerConfiguration>();
            if (this.config.Enable) UpdateInterval = this.config.Interval * 1000;
        }
        public override void OnServerStart()
        {
            EnableUpdates();
        }
        public override void OnServerStop()
        {
            DisableUpdates();
        }
        protected override void OnUpdate()
        {
            InvokeEvent(Broadcast, this, new AnnounceEventArgs(config.AnnounceList[currentIdx++]));
            if (currentIdx == config.AnnounceList.Count) currentIdx = 0;
        }
    }
}