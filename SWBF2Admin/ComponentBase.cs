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

        protected string FormatString(string message, params string[] tags)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (i + 1 >= tags.Length)
                    Logger.Log(LogLevel.Warning, "No value for parameter {0} specified. Ignoring it.", tags[i]);
                else
                    message = message.Replace(tags[i], tags[++i]);
            }
            return message;
        }

        protected void SendFormatted(string message, params string[] tags)
        {
            SendFormatted(message, null, tags);
        }

        protected void SendFormatted(string message, Player player, params string[] tags)
        {
            message = FormatString(message, tags);
            if (player == null) Core.Rcon.Say(message); else Core.Rcon.Pm(message, player);
        }

    }
}