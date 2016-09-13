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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks; 
#endregion
namespace Xigadee
{
    /// <summary>
    /// This class holds the current status of the Microservice container.
    /// </summary>
    public class MicroserviceStatistics: MessagingStatistics, ILogStoreName
    {
        #region Constructor
        /// <summary>
        /// This is the statistics default constructor.
        /// </summary>
        public MicroserviceStatistics() : base()
        {

        } 
        #endregion

        #region Name
        /// <summary>
        /// This override places the name at the top of the JSON
        /// </summary>
        public override string Name
        {
            get
            {
                return base.Name;
            }

            set
            {
                base.Name = value;
            }
        }
        #endregion

        /// <summary>
        /// This is the application tag for the overall system.
        /// </summary>
        public string Application { get; set; }

        /// <summary>
        /// This is the environment tag, i.e production, UAT, SIT, staging etc. 
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// This is the current status of the service.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// This is the service id used for communication.
        /// </summary>
        public string ExternalServiceId { get; set; }

        /// <summary>
        /// This is the last time that the statistics were updated.
        /// </summary>
        public DateTime LogTime { get; set; }

        /// <summary>
        /// This is the service start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// This is the service uptime.
        /// </summary>
        public string Uptime
        {
            get
            {
                var span = LogTime - StartTime;
                return StatsCounter.LargeTime(span);
            }
        }
            
        /// <summary>
        /// This is the unique client identifier.
        /// </summary>
        public string ServiceId { get; set; }

        /// <summary>
        /// This is the machine name on the device.
        /// </summary>
        public string MachineName { get; set; }
        /// <summary>
        /// This is the task manager statistics.
        /// </summary>
        public TaskManagerStatistics Tasks { get; set; }

        /// <summary>
        /// This is the current configuration for the Microservice.
        /// </summary>
        public MicroserviceConfigurationOptions Configuration { get; set; }
        /// <summary>
        /// This is a list of the handlers active on the system and their status.
        /// </summary>
        public CommunicationStatistics Communication { get; set; }

        /// <summary>
        /// This is the command container statistics/
        /// </summary>
        public CommandContainerStatistics Commands { get; set; }
        /// <summary>
        /// The resource statistics.
        /// </summary>
        public ResourceTrackerStatistics Resources { get; set; }

        /// <summary>
        /// The security statictics.
        /// </summary>
        public SecurityStatistics Security { get; set; }
        /// <summary>
        /// The scheduler statistics.
        /// </summary>
        public SchedulerStatistics Scheduler { get; set; }

        /// <summary>
        /// The data collection statistics. These include the logger, event source and telemetry statistics.
        /// </summary>
        public DataCollectionStatistics DataCollection { get; set; }

        private long mTimeouts;

        public void TimeoutRegister(long count)
        {
            Interlocked.Add(ref mTimeouts, count);
        }

        /// <summary>
        /// This is the number of timeouts since the tracker started.
        /// </summary>
        public long Timeouts
        {
            get { return mTimeouts; }
        }

        #region StorageId
        /// <summary>
        /// This is the Id used in the undelying storage.
        /// </summary>
        public string StorageId
        {
            get
            {
                return string.Format("{0}_{3:yyyyMMddHHmmssFFF}_{1}_{2}", Name, MachineName, ServiceId, LogTime);
            }
        }
        #endregion


        /// <summary>
        /// This is the unique client identifier.
        /// </summary>
        public string VersionId { get; set; }
        /// <summary>
        /// This is the version of the Microservice engine.
        /// </summary>
        public string EngineVersionId { get; set; }
    }
}