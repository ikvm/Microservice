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
using Microsoft.Azure.ServiceBus;
using System;
#endregion
namespace Xigadee
{
    /// <summary>
    /// This is the Service Bus Queue Listener harness.
    /// </summary>
    public class AzureServiceBusQueueListenerHarness: ListenerHarness<AzureServiceBusQueueListener>
    {
        /// <summary>
        /// Configures the specified configuration for the Azure Service Bus.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="mappingChannelId">The actual channel id for the Azure Service Bus queue.</param>
        /// <param name="boundaryLoggingActive">Sets boundary logging as active.</param>
        public override void Configure(IEnvironmentConfiguration configuration
            , string channelId
            , string mappingChannelId = null
            , bool boundaryLoggingActive = true)
        {
            base.Configure(configuration, channelId, boundaryLoggingActive);
            Service.MappingChannelId = mappingChannelId;
            Service.AzureServiceBusPropertiesSet(configuration);
        }
    }
    /// <summary>
    /// This is the Service Bus Topic Listener harness
    /// </summary>
    public class AzureServiceBusTopicListenerHarness : ListenerHarness<AzureServiceBusTopicListener>
    {
        /// <summary>
        /// Configures the specified configuration for the Azure Service Bus.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="mappingChannelId">The actual channel id for the Azure Service Bus queue.</param>
        /// <param name="boundaryLoggingActive">Sets boundary logging as active.</param>
        public override void Configure(IEnvironmentConfiguration configuration
            , string channelId
            , string mappingChannelId = null
            , bool boundaryLoggingActive = true)
        {
            base.Configure(configuration, channelId, boundaryLoggingActive);
            Service.MappingChannelId = mappingChannelId;
            Service.AzureServiceBusPropertiesSet(configuration);
        }
    }
}
