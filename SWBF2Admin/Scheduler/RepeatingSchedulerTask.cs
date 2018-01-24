/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see<http://www.gnu.org/licenses/>.
 */
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