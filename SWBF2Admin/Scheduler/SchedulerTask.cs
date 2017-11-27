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
            task.Invoke();
        }
    }
}