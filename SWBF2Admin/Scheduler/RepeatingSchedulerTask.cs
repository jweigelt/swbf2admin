using System;

namespace SWBF2Admin.Scheduler
{
    public class RepeatingSchedulerTask : SchedulerTask
    {
        private Int64 lastRun;
        private DateTime startTime;
        public int Interval { get; set; }

        public RepeatingSchedulerTask(TaskDelegate task) : base(task)
        {
            startTime = new DateTime(1970, 1, 1);
        }
        public RepeatingSchedulerTask(TaskDelegate task, int interval)
            : base(task)
        {
            Interval = interval;
            startTime = new DateTime(1970, 1, 1);
        }

        public void Tick()
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

        private Int64 GetMillis()
        {
            return (Int64)((DateTime.Now - startTime).TotalMilliseconds);
        }

    }
}