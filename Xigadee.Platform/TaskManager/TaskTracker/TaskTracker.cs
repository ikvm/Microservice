﻿#region Copyright
// Copyright Hitachi Consulting
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

#region using
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion
namespace Xigadee
{
    /// <summary>
    /// This is the tracker agent used to trace queued and executing jobs.
    /// </summary>
    [DebuggerDisplay("{Type}/{Name}={ProcessSlot}@{Priority}|{Id}")]
    public class TaskTracker
    {
        /// <summary>
        /// This is the priority value for a internal task.
        /// </summary>
        public const int PriorityInternal = -1;

        public TaskTracker(TaskTrackerType type, TimeSpan? ttl)
        {
            Type = type;
            TTL = ttl??TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// This is the unique tracking id for the process.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();
        /// <summary>
        /// This is the maximum time to live for the process.
        /// </summary>
        public TimeSpan TTL { get; }
        /// <summary>
        /// This is the Microservice tracker type
        /// </summary>
        public TaskTrackerType Type { get; }
        /// <summary>
        /// This is the UTC timestamp for the process.
        /// </summary>
        public DateTime UTCStart { get; } = DateTime.UtcNow;
        /// <summary>
        /// This is the tick count for the process.
        /// </summary>
        public int TickCount { get; } = Environment.TickCount;
        /// <summary>
        /// This is the cancellation token used to signal tiemouts or shutdown.
        /// </summary>
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        /// <summary>
        /// This is the command that initiated the request.
        /// </summary>
        public ICommand Callback { get; set; }
        /// <summary>
        /// This is the id that the command has registered the request under.
        /// </summary>
        public string CallbackId { get; set; }

        public int? ExecuteTickCount { get; set; }

        public TimeSpan? TimeProcessing
        {
            get
            {
                return UTCExecute.HasValue ? DateTime.UtcNow - UTCExecute.Value : default(TimeSpan?);
            }
        }

        public TimeSpan? TimeToExpiry
        {
            get
            {
                return ExpireTime.HasValue ? ExpireTime.Value - DateTime.UtcNow : default(TimeSpan?);
            }
        }

        public long? ProcessSlot { get; set; }

        public int? Priority { get; set; }

        /// <summary>
        /// This is the friendly name used during statistic debugging.
        /// </summary>
        public string Name { get; set; }


        public string Caller { get; set; }


        /// <summary>
        /// This boolean property identifies when a task is long running and is used to identify that fact to the Task Manager.
        /// </summary>
        public bool IsLongRunning { get; set; }

        /// <summary>
        /// This boolean property identifies whether the request has been generated by another task for immediate processing.
        /// This type of task will not count as a running task as it has been generated by a task that already has been assigned 
        /// a running slot.
        /// </summary>
        public bool IsInternal { get { return Priority.HasValue && Priority.Value == PriorityInternal; } }
        /// <summary>
        /// This boolean property indicates whether the task has been flagged for cancellation.
        /// </summary>
        public bool IsCancelled { get; set; }

        public bool IsKilled { get; set; }

        /// <summary>
        /// This is the functional called which returns the task to be executed when the tracker is scheduled to execute.
        /// </summary>
        public Func<CancellationToken, Task> Execute { get; set; }
        /// <summary>
        /// This action is executed once the task has completed. It passed the original task, a boolean value 
        /// indicating whether the task failed, and any exception that was generated by the failure.
        /// </summary>
        public Action<TaskTracker, bool, Exception> ExecuteComplete { get; set; }


        public DateTime? UTCExecute { get; set; }

        public DateTime? CancelledTime { get; set; }

        #region HasExpired
        /// <summary>
        /// This boolean value indicates whether the process has expired.
        /// </summary>
        public bool HasExpired
        {
            get
            {
                var time = ExpireTime;
                return time.HasValue && (DateTime.UtcNow > time.Value);
            }
        } 
        #endregion

        public DateTime? ExpireTime
        {
            get
            {
                return (!UTCExecute.HasValue || IsLongRunning)?default(DateTime?):UTCExecute.Value.Add(TTL);
            }
        }

        #region Cancel()
        /// <summary>
        /// This method is used to cancel the task.
        /// </summary>
        public void Cancel()
        {
            if (IsCancelled)
                return;

            CancelledTime = DateTime.UtcNow;
            IsCancelled = true;

            Cts.Cancel();

            try
            {
                if (Callback != null && !string.IsNullOrEmpty(CallbackId))
                    Callback.TimeoutTaskManager(CallbackId);
            }
            catch (Exception)
            {

            }
        } 
        #endregion

        /// <summary>
        /// This is the task used when the tracker is executed.
        /// </summary>
        public Task ExecuteTask { get; set; }


        /// <summary>
        /// This is the context object that can be used to hold additional data for the context/
        /// </summary>
        public object Context { get; set; }

        public bool IsFailure { get; set; }

        public Exception FailureException { get; set; }

        #region Debug
        /// <summary>
        /// This is the debug message for the task.
        /// </summary>
        public string Debug
        {
            get
            {
                try
                {
                    var queueTime = StatsCounter.LargeTime((UTCExecute ?? DateTime.UtcNow) - UTCStart);
                    var executeTime = StatsCounter.LargeTime(TimeProcessing, "Never");
                    var expireTime = StatsCounter.LargeTime(TimeToExpiry, "Never");

                    string id = null;
                    string pid = null;

                    switch (Type)
                    {
                        case TaskTrackerType.Notset:
                            return "Not set";
                        case TaskTrackerType.Payload:
                            var payload = Context as TransmissionPayload;
                            id = payload.Message.CorrelationKey;
                            pid = payload.Id.ToString("N").ToUpperInvariant();
                            break;
                        case TaskTrackerType.Schedule:
                            var schedule = Context as Schedule;
                            id = schedule.Id.ToString("N");
                            break;
                        case TaskTrackerType.ListenerPoll:
                            id = Id.ToString("N");
                            break;
                        case TaskTrackerType.Overload:
                            id = Id.ToString("N");
                            break;
                    }

                    return string.Format("{10} {0}[{1}] {2} [{3}] Runtime={5} Expires={6} ({7}){8}{9} QueueTime={4} {11}"
                        , Type
                        , Priority
                        , id
                        , Name
                        , queueTime
                        , executeTime
                        , expireTime
                        , Caller
                        , IsLongRunning ? " Long running" : ""
                        , IsCancelled ? (IsKilled?"Killed":"Cancelled") : ""
                        , ProcessSlot
                        , pid
                        );
                }
                catch (Exception ex)
                {
                    return string.Format("Error {0} - {1}", Id, ex.Message);
                }
            }
        }
        #endregion

    }
}
