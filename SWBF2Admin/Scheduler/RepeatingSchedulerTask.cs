using System;

namespace SWBF2Admin.Scheduler
{
    public class RepeatingSchedulerTask : SchedulerTask
    {
        private Int64 lastRun;
        protected DateTime StartTime { get; set; }
        public int Interval { get; set; }
        public bool Remove { get; set; } = false;

        public RepeatingSchedulerTask(TaskDelegate task) : base(task)
        {
            StartTime = DateTime.Now;
        }

        public RepeatingSchedulerTask(TaskDelegate task, int interval)
            : base(task)
        {
            Interval = interval;
            StartTime = DateTime.Now;
        }

        /// <summary>
        /// Runs the task if required.
        /// </summary>
        public virtual void Tick()
        {
            if (Interval == 0)
            {
                Run();
                return;
            }
            else if (lastRun < GetMillis() - Interval)
            {
                lastRun = GetMillis();
                Run();
            }
        }

        /// <summary>
        /// Gets millis since object creation
        /// </summary>
        protected Int64 GetMillis()
        {
            return (Int64)((DateTime.Now - StartTime).TotalMilliseconds);
        }

    }
}