﻿#region using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using System.IO;
using Microsoft.ServiceBus;
using System.Threading;
#endregion
namespace Xigadee
{
    /// <summary>
    /// This is the generic Azure Service Bus client holder class. 
    /// This allows a common client for difference between a QueueClient and SubscriptionClient.
    /// </summary>
    /// <typeparam name="C">The client type.</typeparam>
    public class AzureClientHolder<C, M> : ClientHolder<C, M>
        where C : ClientEntity
    {
        #region Transmit(TransmissionPayload payload, int retry = 0)
        /// <summary>
        /// This method will transmit a message.
        /// </summary>
        /// <param name="message">The message to transmit.</param>
        /// <param name="retry">The current number of retries.</param>
        public override async Task Transmit(TransmissionPayload payload, int retry = 0)
        {
            bool tryAgain = false;
            bool fail = true;
            try
            {
                LastTickCount = Environment.TickCount;

                if (retry > MaxRetries)
                    throw new RetryExceededTransmissionException();

                var message = MessagePack(payload);
                await MessageTransmit(message);
                if (BoundaryLogger != null)
                    BoundaryLogger.Log(BoundaryLoggerDirection.Outgoing, payload);
                fail = false;
            }
            catch (NoMatchingSubscriptionException nex)
            {
                //OK, this happens when the remote transmitting party has closed or recycled.
                LogException(string.Format("The sender has closed: {0}", payload.Message.CorrelationServiceId), nex);
                if (BoundaryLogger != null)
                    BoundaryLogger.Log(BoundaryLoggerDirection.Outgoing, payload, nex);

            }
            catch (TimeoutException tex)
            {
                LogException("TimeoutException (Transmit)", tex);
                tryAgain = true;
                if (BoundaryLogger != null)
                    BoundaryLogger.Log(BoundaryLoggerDirection.Outgoing, payload, tex);

            }
            catch (MessagingException dex)
            {
                //OK, something has gone wrong with the Azure fabric.
                LogException("Messaging Exception (Transmit)", dex);
                //Let's reinitialise the client
                if (FabricInitialize != null)
                {
                    FabricInitialize();
                    tryAgain = true;
                }
                else
                    throw;
            }
            catch (Exception ex)
            {
                LogException("Unhandled Exception (Transmit)", ex);
                if (BoundaryLogger != null)
                    BoundaryLogger.Log(BoundaryLoggerDirection.Outgoing, payload, ex);

                throw;
            }
            finally
            {
                if (fail)
                    mStatistics.ExceptionHitIncrement();
            }

            try
            {
                if (tryAgain)
                    await Transmit(payload, retry++);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion

        #region MessageComplete(TransmissionPayload payload)
        /// <summary>
        /// This method is used to signal that a message has completed to the underlying fabric.
        /// </summary>
        /// <param name="payload">The payload to singnal completion.</param>
        public override void MessageComplete(TransmissionPayload payload)
        {
            // Remove message from queue if the message cannot be signalled.
            if (!payload.MessageCanSignal)
                payload.SignalSuccess();
        } 
        #endregion

        #region MessagesPull(int? count, int? wait)
        /// <summary>
        /// This method pulls a set of messages from the fabric and unpacks them
        /// in to a TransmissionPayload message.
        /// </summary>
        /// <param name="count">The number of messages to pull.</param>
        /// <param name="wait">The maximum wait time.</param>
        /// <returns>Returns a list of messages or null if the request times out.</returns>
        public override async Task<List<TransmissionPayload>> MessagesPull(int? count, int? wait, string mappingChannel = null)
        {
            List<TransmissionPayload> batch = null;
            Guid? batchId = null;
            try
            {
                var intBatch = await MessageReceive(count, wait);

                int items = intBatch != null ? intBatch.Count() : 0;

                if (BoundaryLogger != null)
                    batchId = BoundaryLogger.BatchPoll(count ?? -1, items, mappingChannel ?? Name);
                //string.Format("{0}/{1}@{3} [{2}]", Type, Name, mappingChannel, Priority));

                batch = intBatch
                    .Select((m) => TransmissionPayloadUnpack(m, Priority, mappingChannel, batchId))
                    .ToList();
            }
            catch (MessagingException dex)
            {
                //OK, something has gone wrong with the Azure fabric.
                LogException("Messaging Exception (Transmit)", dex);
                //Let's reinitialise the client
                if (FabricInitialize != null)
                {
                    FabricInitialize();
                }
                else
                    throw;
            }
            catch (TimeoutException tex)
            {
                LogException("MessagesPull (Timeout)", tex);
                if (batch == null)
                    batch = new List<TransmissionPayload>();
            }

            LastTickCount = Environment.TickCount;

            return batch;
        }
        #endregion
        #region TransmissionPayloadUnpack(M message, int priority, string mappingChannel = null)
        /// <summary>
        /// This method wraps the incoming fabric message in to a generic payload.
        /// </summary>
        /// <param name="message">The incoming fabric message.</param>
        /// <returns>Returns the payload with the service message.</returns>
        protected virtual TransmissionPayload TransmissionPayloadUnpack(M message, int priority, string mappingChannel = null, Guid? batchId = null)
        {
            ServiceMessage serviceMessage = MessageUnpack(message);
            serviceMessage.ChannelPriority = priority;

            if (mappingChannel != null)
                serviceMessage.ChannelId = mappingChannel;

            //Get the payload message with the associated metadata for transmission
            var payload =  PayloadRegisterAndCreate(message, serviceMessage);

            //Get the boundary logger to log the metadata.
            if (BoundaryLogger != null)
                BoundaryLogger.Log(BoundaryLoggerDirection.Incoming, payload, batchId: batchId);

            return payload;
        }
        #endregion


    }
}