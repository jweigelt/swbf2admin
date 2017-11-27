using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace SWBF2Admin.Scheduler
{
    public class TaskScheduler
    {
        public int TickDelay { get; set; } = 10;
        private ConcurrentQueue<SchedulerTask> taskQueue;
        private List<RepeatingSchedulerTask> repeatingTasks;

        private Thread workThread;
        private bool running;

        public TaskScheduler()
        {
            taskQueue = new ConcurrentQueue<SchedulerTask>();
            repeatingTasks = new List<RepeatingSchedulerTask>(); ;
        }

        public void Start()
        {
            running = true;
            workThread = new Thread(this.DoWork);
            workThread.Start();
        }

        public void Stop()
        {
            running = false;
            if (workThread != null)
            {
                workThread.Join();
                workThread = null;
            }
        }

        public void PushTask(SchedulerTask task)
        {
            taskQueue.Enqueue(task);
        }

        public void PushRepeatingTask(RepeatingSchedulerTask task)
        {
            repeatingTasks.Add(task);
        }

        public void ClearRepeatingTasks()
        {
            repeatingTasks.Clear();
        }

        private void DoWork()
        {
            while (running)
            {
                if (taskQueue.Count > 0)
                {
                    SchedulerTask t;
                    while (!taskQueue.TryDequeue(out t)) Thread.Sleep(TickDelay);
                    t.Run();
                }

                foreach (RepeatingSchedulerTask task in this.repeatingTasks)
                {
                    task.Tick();
                }
                Thread.Sleep(TickDelay);
            }
        }

        ~TaskScheduler()
        {
            Stop();
        }

    }
}
