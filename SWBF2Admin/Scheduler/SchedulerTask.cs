using SWBF2Admin.Utility;
using System;

namespace SWBF2Admin.Scheduler
{
    public class SchedulerTask
    {
        public delegate void TaskDelegate();
        private TaskDelegate task;

        public SchedulerTask(TaskDelegate task)
        {
            this.task = task;
        }

        public void Run()
        {
            try
            {
                task.Invoke();
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Verbose, "Failed to run task {0}::{1} ({2})", task.Target.ToString(), task.Method.Name, e.Message);
            }
        }
    }
}