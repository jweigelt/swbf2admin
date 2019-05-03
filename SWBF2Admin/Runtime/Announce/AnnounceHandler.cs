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
        public override void OnServerStart(EventArgs e)
        {
            EnableUpdates();
        }
        public override void OnServerStop()
        {
            DisableUpdates();
        }
        protected override void OnUpdate()
        {
            if (Core.Players.PlayerList.Count > 0 && config.AnnounceList.Count != 0)
            {
                InvokeEvent(Broadcast, this, new AnnounceEventArgs(config.AnnounceList[currentIdx++]));
                if (currentIdx == config.AnnounceList.Count) currentIdx = 0;
            }
        }
    }
}