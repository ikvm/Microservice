﻿#region using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion
namespace Xigadee
{
    /// <summary>
    /// This is the job base class. A job is a regular process that requires poll support.
    /// In additions, a job can run as a master job, which uses the fabric to communicate 
    /// and negotiate with other instances to agree on only a single instance should run 
    /// at any one time, effectively a system wide singleton.
    /// </summary>
    public abstract class JobBase<S> : MessageInitiatorBase<MessageInitiatorRequestTracker,S>, IJob, IMessageHandlerDynamic
        where S: JobStatistics, new()
    {
        #region Declarations
        /// <summary>
        /// This collection holds the list of master job standby partners.
        /// </summary>
        protected ConcurrentDictionary<string, StandbyPartner> mStandbyPartner;
        /// <summary>
        /// The current status.
        /// </summary>
        protected DateTime? mCurrentMasterReceiveTime;

        protected string mCurrentMasterServiceId = null;

        protected int mCurrentMasterPollAttempts = 0;

        protected Random mRandom = new Random(Environment.TickCount);

        protected DateTime? mLastPollTime = null;
        /// <summary>
        /// This holds the master job collection.
        /// </summary>
        protected Dictionary<Guid, MasterJobHolder> mMasterJobs;
        /// <summary>
        /// This is the job timer
        /// </summary>
        protected List<Schedule> mSchedules;
        /// <summary>
        /// This is the configuration object used to specify the job type and poll schedule.
        /// </summary>
        protected JobConfiguration mConfig;
        #endregion
        #region Constructor
        /// <summary>
        /// This is the primary configuration constructor. You can use this to specify a master job or a standard job.
        /// </summary>
        /// <param name="config">The job configuration class.</param>
        protected JobBase(JobConfiguration config)
        {
            mSchedules = new List<Schedule>();
            mConfig = config ?? new JobConfiguration();
            mStandbyPartner = new ConcurrentDictionary<string, StandbyPartner>();

            if (mConfig.IsMasterJob) 
            {
                State = MasterJobState.VerifyingComms;

                var masterjobPoll = new Schedule(MasterJobStateNotificationOutgoing, string.Format("MasterJob: {0}", config.MasterJobName ?? GetType().Name));

                mMasterJobs = new Dictionary<Guid, MasterJobHolder>();
                NegotiationChannelPriority = mConfig.MasterJobNegotiationChannelPriority;
                NegotiationChannelId = mConfig.MasterJobNegotiationChannelId??ChannelId;
                NegotiationMessageType = mConfig.MasterJobNegotiationChannelType??GetType().Name;

                masterjobPoll.Frequency = TimeSpan.FromSeconds(20);
                masterjobPoll.InitialWait = TimeSpan.FromSeconds(5);
                masterjobPoll.IsLongRunning = false;

                mSchedules.Add(masterjobPoll);
            }

            if (mConfig.HasTimerPoll)
                TimerPollSchedulesRegister(config);
        }
        #endregion
        #region TimerPollSchedulesRegister(JobConfiguration config)
        /// <summary>
        /// This method can be overriden to enable additional schedules to be registered for the job.
        /// </summary>
        /// <param name="config">The incoming job configuration.</param>
        protected virtual void TimerPollSchedulesRegister(JobConfiguration config)
        {

        } 
        #endregion

        #region StartInternal()
        /// <summary>
        /// This override starts the time poll.
        /// </summary>
        /// <param name="args">The arguments. These are not stored.</param>
        protected override void StartInternal()
        {
            base.StartInternal();

            mSchedules.ForEach((s) => Scheduler.Register(s));
        } 
        #endregion
        #region StopInternal()
        /// <summary>
        /// This override stops the time.
        /// </summary>
        protected override void StopInternal()
        {
            if (State == MasterJobState.Active)
            {
                MasterJobStop();
                NegotiationTransmit(MasterJobStates.ResyncMaster).Wait();
            }

            mSchedules.ForEach((s) =>
            {
                s.ShouldPoll = false;
                Scheduler.Unregister(s);
            });

            base.StopInternal();
        }
        #endregion

        #region StatisticsRecalculate()
        /// <summary>
        /// This method recalculates the statistics for the job.
        /// </summary>
        protected override void StatisticsRecalculate()
        {
            base.StatisticsRecalculate();
            mStatistics.MasterJob.Active = mConfig.IsMasterJob;
            if (mConfig.IsMasterJob)
            {
                mStatistics.MasterJob.Server = string.Format("{0} @ {1:o}", mCurrentMasterServiceId, mCurrentMasterReceiveTime);
                mStatistics.MasterJob.Status = string.Format("Status={0} Channel={1}/{2} Type={3}", State.ToString(), NegotiationChannelId, NegotiationChannelPriority, NegotiationMessageType);
                mStatistics.MasterJob.Standbys = mStandbyPartner.Values.ToList();
            }
        } 
        #endregion

        #region CommandsRegister()
        /// <summary>
        /// This override registers the state notification for the master job.
        /// </summary>
        public override void CommandsRegister()
        {
            if (mConfig.IsMasterJob)
            {
                CommandRegister(NegotiationChannelId, NegotiationMessageType, null, MasterJobStateNotificationIncoming);
            }

            base.CommandsRegister();
        }
        #endregion
        #region ProcessMessage(ServiceMessage message)
        /// <summary>
        /// This method is used to send messages to the sender.
        /// This can be overridden to support specific functionality.
        /// </summary>
        /// <param name="message">The message to externalOnly.</param>
        /// <param name="opts">The transmission options.</param>
        protected virtual void ProcessMessage(TransmissionPayload payload)
        {
            Dispatcher(this, payload);
        }
        #endregion

        #region NegotiationChannelId
        /// <summary>
        /// This is the channel used to negotiate.
        /// </summary>
        public string NegotiationChannelId { get; set; }
        #endregion
        #region NegotiationChannelPriority
        /// <summary>
        /// This is the message priority.
        /// </summary>
        public int NegotiationChannelPriority { get; set; }
        #endregion
        #region NegotiationMessageType
        /// <summary>
        /// This is the negotiation message type.
        /// </summary>
        public string NegotiationMessageType { get; set; }
        #endregion
        #region NegotiationTransmit(string action)
        /// <summary>
        /// This method transmits the notification message to the other instances.
        /// </summary>
        /// <param name="action">The action to transmit.</param>
        protected virtual async Task NegotiationTransmit(string action)
        {
            var payload = TransmissionPayload.Create();
            payload.Options = ProcessOptions.RouteExternal;
            var message = payload.Message;

            message.ChannelId = NegotiationChannelId;
            message.ChannelPriority = NegotiationChannelPriority;
            message.MessageType = NegotiationMessageType;
            message.ActionType = action;

            //Go straight to the dispatcher as we don't want to use the tracker for this job
            //as it is transmit only.
            Dispatcher(this, payload);
        }
        #endregion

        #region State
        /// <summary>
        /// This boolean property identifies whether this job is the master job for the particular 
        /// NegotiationMessageType.
        /// </summary>
        public MasterJobState State { get; set; }
        #endregion

        #region --> MasterJobStateNotificationIncoming(TransmissionPayload rq, List<TransmissionPayload> rs)
        /// <summary>
        /// This method processes state notifications from other instances of the MasterJob.
        /// </summary>
        /// <param name="rq">The request.</param>
        /// <param name="rs">The responses - this is not used.</param>
        private async Task MasterJobStateNotificationIncoming(TransmissionPayload rq, List<TransmissionPayload> rs)
        {
            //Filter out any messages sent from this service.
            if (string.Equals(rq.Message.OriginatorServiceId, OriginatorId, StringComparison.InvariantCultureIgnoreCase))
            {
                //We can now say that the masterjob channel is working, so we can now enable the job for negotiation.
                if (State == MasterJobState.VerifyingComms)
                    State = MasterJobState.Starting;

                return;
            }

            switch (rq.Message.ActionType)
            {
                case MasterJobStates.IAmStandby:
                    if (State == MasterJobState.Active)
                    {
                        var record = new StandbyPartner(rq.Message.OriginatorServiceId);
                        mStandbyPartner.AddOrUpdate(record.ServiceId, (s) => record, (s,o) => record);
                    }
                    break;
                case MasterJobStates.IAmMaster:
                    if (State == MasterJobState.Active)
                    {
                        MasterJobStop();
                    }
                    State = MasterJobState.Inactive;
                    mCurrentMasterServiceId = rq.Message.OriginatorServiceId;
                    mCurrentMasterPollAttempts = 0;
                    mCurrentMasterReceiveTime = DateTime.UtcNow;
                    await NegotiationTransmit(MasterJobStates.IAmStandby);
                    break;
                case MasterJobStates.ResyncMaster:
                    State = MasterJobState.Starting;
                    mCurrentMasterServiceId = "";
                    mCurrentMasterReceiveTime = DateTime.UtcNow;
                    break;
                case MasterJobStates.WhoIsMaster:
                    if (State == MasterJobState.Active)
                        await MasterJobSyncMaster();
                    break;
                case MasterJobStates.RequestingControl1:
                    if (State == MasterJobState.Active)
                        await MasterJobSyncMaster();
                    else if (State <= MasterJobState.Requesting1)
                        State = MasterJobState.Inactive;
                    break;
                case MasterJobStates.RequestingControl2:
                    if (State == MasterJobState.Active)
                        await MasterJobSyncMaster();
                    else if (State <= MasterJobState.Requesting2)
                        State = MasterJobState.Inactive;
                    break;
                case MasterJobStates.TakingControl:
                    if (State == MasterJobState.Active)
                        await MasterJobSyncMaster();
                    else if (State <= MasterJobState.TakingControl)
                        State = MasterJobState.Inactive;
                    break;

            }
        }
        #endregion

        #region MasterJobStateNotificationOutgoing(Schedule state, CancellationToken token)
        /// <summary>
        /// This is the master job negotiation logic.
        /// </summary>
        /// <param name="schedule">The current schedule object.</param>
        protected virtual async Task MasterJobStateNotificationOutgoing(Schedule schedule, CancellationToken token)
        {
            mLastPollTime = DateTime.UtcNow;
            //We set a random poll time to reduce the potential for deadlocking
            //and to make the negotiation messaging more efficient.
            schedule.Frequency = TimeSpan.FromSeconds(5 + mRandom.Next(10));

            switch (State)
            {
                case MasterJobState.VerifyingComms:
                    await NegotiationTransmit(MasterJobStates.WhoIsMaster);
                    break;
                case MasterJobState.Starting:
                    await NegotiationTransmit(MasterJobStates.WhoIsMaster);
                    State = MasterJobState.Requesting1;
                    mCurrentMasterPollAttempts++;
                    break;
                case MasterJobState.Inactive:
                    await NegotiationTransmit(MasterJobStates.WhoIsMaster);
                    if (mCurrentMasterPollAttempts == 0)
                        schedule.Frequency = TimeSpan.FromSeconds(10 + mRandom.Next(60));
                    if (mCurrentMasterPollAttempts >= 3)
                        State = MasterJobState.Starting;
                    mCurrentMasterPollAttempts++;
                    break;
                case MasterJobState.Requesting1:
                    await NegotiationTransmit(MasterJobStates.RequestingControl1);
                    State = MasterJobState.Requesting2;
                    break;
                case MasterJobState.Requesting2:
                    await NegotiationTransmit(MasterJobStates.RequestingControl2);
                    State = MasterJobState.TakingControl;
                    break;
                case MasterJobState.TakingControl:
                    await NegotiationTransmit(MasterJobStates.TakingControl);
                    State = MasterJobState.Active;
                    MasterJobStart();
                    break;
                case MasterJobState.Active:
                    await NegotiationTransmit(MasterJobStates.IAmMaster);
                    schedule.Frequency = TimeSpan.FromSeconds(5 + mRandom.Next(25));
                    mCurrentMasterServiceId = null;
                    break;
            }

            mLastPollTime = DateTime.UtcNow;
        } 
        #endregion

        #region MasterJobSyncMaster()
        /// <summary>
        /// This method is used to send a sync message when the master job becomes active.
        /// </summary>
        /// <returns></returns>
        private async Task MasterJobSyncMaster()
        {
            await NegotiationTransmit(MasterJobStates.IAmMaster);
            mCurrentMasterServiceId = "ACTIVE";
            mCurrentMasterReceiveTime = DateTime.UtcNow;
        }
        #endregion

        #region MasterJobCommandsRegister()
        /// <summary>
        /// You should override this command to register incoming requests when the master job becomes active.
        /// </summary>
        protected virtual void MasterJobCommandsRegister()
        {

        }
        #endregion
        #region MasterJobCommandsUnregister()
        /// <summary>
        /// You should override this method to unregister active commands when the job is shutting down or moves to an inactive state.
        /// </summary>
        protected virtual void MasterJobCommandsUnregister()
        {

        } 
        #endregion

        #region MasterJobStart()
        /// <summary>
        /// This method registers each job with the scheduler.
        /// </summary>
        protected virtual void MasterJobStart()
        {
            MasterJobCommandsRegister();

            foreach (var job in mMasterJobs.Values)
            {
                try
                {
                    if (job.Initialise != null)
                        job.Initialise(job.Schedule);
                }
                catch (Exception ex)
                {
                    mStatistics.Ex = ex;
                    Logger.Log(mStatistics);
                }

                Scheduler.Register(job.Schedule);
            }
        }
        #endregion
        #region MasterJobExecute(Schedule schedule)
        /// <summary>
        /// This method retrieves the master job from the collection and calls the 
        /// relevant action.
        /// </summary>
        /// <param name="schedule">The schedule to activate.</param>
        protected virtual async Task MasterJobExecute(Schedule schedule, CancellationToken cancel)
        {
            var id = schedule.Id;
            if (mMasterJobs.ContainsKey(id))
                try
                {
                    await mMasterJobs[id].Action(schedule);
                }
                catch (Exception ex)
                {
                    mStatistics.Ex = ex;
                    Logger.Log(mStatistics);
                    throw;
                }
        }
        #endregion
        #region MasterJobStop()
        /// <summary>
        /// This method removes each job from the scheduler.
        /// </summary>
        protected virtual void MasterJobStop()
        {
            foreach (var job in mMasterJobs.Values)
            {
                try
                {
                    if (job.Cleanup != null)
                        job.Cleanup(job.Schedule);
                }
                catch (Exception ex)
                {
                    mStatistics.Ex = ex;
                    Logger.Log(mStatistics);
                }

                Scheduler.Unregister(job.Schedule);
                mStandbyPartner.Clear();
            }

            MasterJobCommandsUnregister();
        }
        #endregion
        #region MasterJobRegister ...
        /// <summary>
        /// This method registers a master job that will be called at the schedule specified 
        /// when the job is active.
        /// </summary>
        /// <param name="schedule">The schedule to call.</param>
        /// <param name="action">The action to call.</param>
        protected void MasterJobRegister(TimeSpan? frequency, Func<Schedule, Task> action
            , string name = null, TimeSpan? initialWait = null, DateTime? initialTime = null
            , Action<Schedule> initialise = null
            , Action<Schedule> cleanup = null)
        {
            var schedule = new Schedule(MasterJobExecute, name?? string.Format("MasterJob{0}", GetType().Name))
            {
                InitialWait = initialWait,
                Frequency = frequency,
                InitialTime = initialTime
            };

            mMasterJobs.Add(schedule.Id, new MasterJobHolder(schedule, action, initialise, cleanup));
        }
        #endregion

        #region ResponseId
        /// <summary>
        /// This override will receive the incoming messages
        /// </summary>
        protected override MessageFilterWrapper ResponseId
        {
            get
            {
                if (ResponseChannelId == null)
                    return null;

                return new MessageFilterWrapper(new ServiceMessageHeader(ResponseChannelId, "MasterJob", GetType().Name)) { ClientId = OriginatorId };
            }
        }
        #endregion
    }
}