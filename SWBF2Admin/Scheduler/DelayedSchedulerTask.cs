using System;

namespace SWBF2Admin.Scheduler
{
    public class DelayedSchedulerTask : RepeatingSchedulerTask
    {
        public DelayedSchedulerTask(TaskDelegate task) : base(task) { }
        public DelayedSchedulerTask(TaskDelegate task, int interval) : base(task, interval) { }

        /// <summary>
        /// Runs the task if required.
        /// </summary>
        public override void Tick()
        {
            if (Interval < GetMillis())
            {
                Remove = true;
                Run();
            }
        }
    }
}