using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace SWBF2Admin.Scheduler
{
    public class TaskScheduler
    {
        public bool IsSync
        {
            get
            {
                return ((workThread != null) && workThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId);
            }
        }
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

        /// <summary>
        /// Starts the scheduler's workthread
        /// </summary>
        public void Start()
        {
            running = true;
            workThread = new Thread(this.DoWork);
            workThread.Start();
        }

        /// <summary>
        /// Stops the scheduler's workthread
        /// </summary>
        public void Stop()
        {
            running = false;
            if (workThread != null)
            {
                workThread.Join();
                workThread = null;
            }
        }

        /// <summary>
        /// Adds a task to the queue
        /// </summary>
        /// <param name="task">Task to be executed</param>
        public void PushTask(SchedulerTask task)
        {
            taskQueue.Enqueue(task);
        }

        /// <summary>
        /// Adds a task to the queue
        /// </summary>
        /// <param name="d">Delegate to be executed</param>
        public void PushTask(SchedulerTask.TaskDelegate d)
        {
            taskQueue.Enqueue(new SchedulerTask(d));
        }

        /// <summary>
        /// Adds a repeating task to the list
        /// </summary>
        /// <param name="task">Task to be executed</param>
        public void PushRepeatingTask(RepeatingSchedulerTask task)
        {
            repeatingTasks.Add(task);
        }

        /// <summary>
        /// Adds a repeating task to the list
        /// </summary>
        /// <param name="task">Task to be executed</param>
        /// <param name="interval">Delay between running the task</param>
        public void PushRepeatingTask(SchedulerTask.TaskDelegate d, int interval)
        {
            repeatingTasks.Add(new RepeatingSchedulerTask(d, interval));
        }

        /// <summary>
        /// Removes all repeating tasks
        /// </summary>
        public void ClearRepeatingTasks()
        {
            repeatingTasks.Clear();
        }

        /// <summary>
        /// Mainthread
        /// </summary>
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
