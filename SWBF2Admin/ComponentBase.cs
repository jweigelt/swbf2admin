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
using SWBF2Admin.Scheduler;
using SWBF2Admin.Config;
using SWBF2Admin.Utility;
using SWBF2Admin.Structures;
namespace SWBF2Admin
{
    public class ComponentBase
    {
        protected AdminCore Core { get; }
        private RepeatingSchedulerTask task = null;

        public virtual RepeatingSchedulerTask Task { get { return task; } }

        public int UpdateInterval
        {
            get { return (task == null ? -1 : task.Interval); }
            set
            {
                if (task == null) task = new RepeatingSchedulerTask(() => Update());
                task.Interval = value;
            }
        }

        private bool enableUpdate = false;

        public ComponentBase(AdminCore core)
        {
            Core = core;
        }

        ///<summary>Called once after reading core configuration</summary>
        ///<param name="config">Core configuration object</param>
        public virtual void Configure(CoreConfiguration config) { }

        ///<summary>Called once during startup</summary>
        public virtual void OnInit() { }

        ///<summary>Called once during shutdown</summary>
        public virtual void OnDeInit() { }

        ///<summary>Called after the gameserver was launched</summary>
        public virtual void OnServerStart(EventArgs e) { }

        ///<summary>Called after the gameserver was either stopped or crashed</summary>
        public virtual void OnServerStop() { }

        ///<summary>Called in a periodic interval, if UpdateInterval is greater than 0 and EnableUpdates() was called.</summary>
        protected virtual void OnUpdate() { }

        ///<summary>Wrapper-function for OnUpdate</summary>
        public void Update()
        {
            if (enableUpdate) OnUpdate();
        }

        ///<summary>
        ///Invokes an EventHandler.
        ///<para>The EventHandler is always invoked be Core.Scheduler.workThread</para> 
        ///</summary>
        ///<param name="evt">Core configuration object</param>
        ///<param name="sender">sender object for invoking the EventHandler</param>
        ///<param name="e">EventArgs for invoking the EventHandler</param>
        public void InvokeEvent(EventHandler evt, object sender, EventArgs e)
        {
            if (evt != null)
            {
                if (Core.Scheduler.IsSync)
                {
                    evt.Invoke(sender, e);
                }
                else
                {
                    Core.Scheduler.PushTask(new SchedulerTask(() => evt.Invoke(sender, e)));
                }
            }

        }

        ///<summary>Enabled periodic calls to OnUpdate(), if UpdateInterval is greater than 0</summary>
        protected void EnableUpdates()
        {
            enableUpdate = true;
        }

        ///<summary>Disables calls to OnUpdate();</summary>
        protected void DisableUpdates()
        {
            enableUpdate = false;
        }

        protected void SendFormatted(string message, params string[] tags)
        {
            SendFormatted(message, null, tags);
        }

        protected void SendFormatted(string message, Player player, params string[] tags)
        {
            message = Util.FormatString(message, tags);
            if (player == null) Core.Rcon.Say(message); else Core.Rcon.Pm(message, player);
        }

    }
}