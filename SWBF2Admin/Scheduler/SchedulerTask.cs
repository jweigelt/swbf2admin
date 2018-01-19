using SWBF2Admin.Utility;
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
           //Logger.Log(LogLevel.Verbose, "Running task {0}::{1}", task.Target.ToString(),task.Method.Name);
            task.Invoke();
        }
    }
}