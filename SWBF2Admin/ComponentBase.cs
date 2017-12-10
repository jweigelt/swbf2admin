using System;
using SWBF2Admin.Scheduler;
using SWBF2Admin.Config;
namespace SWBF2Admin
{
    class ComponentBase
    {
        public AdminCore Core { get; set; }

        public int UpdateInterval { get; set; } = -1;
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
        public virtual void OnServerStart() { }

        ///<summary>Called after the gameserver was either stopped or crashed</summary>
        public virtual void OnServerStop() { }

        ///<summary>Called in a periodic interval, if UpdateInterval is greater than 0 and EnableUpdates() was called.</summary>
        public virtual void OnUpdate() { }

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
        public void EnableUpdates()
        {
            enableUpdate = true;
        }

        ///<summary>Disables calls to OnUpdate();</summary>
        public void DisableUpdates()
        {
            enableUpdate = false;
        }
    }
}