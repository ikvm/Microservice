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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#endregion
namespace Xigadee
{
    public partial class CommunicationContainer: IChannelService
    {
        #region Declarations
        private Dictionary<string, Channel> mContainerIncoming;
        private Dictionary<string, Channel> mContainerOutgoing;
        #endregion

        #region Channels
        /// <summary>
        /// This is a list of the incoming and outgoing channels.
        /// </summary>
        public IEnumerable<Channel> Channels
        {
            get
            {
                return mContainerIncoming.Values.Union(mContainerOutgoing.Values);
            }
        }
        #endregion
        #region Add(Channel item)
        /// <summary>
        /// This method adds a channel to the collection
        /// </summary>
        /// <param name="item">The channel to add.</param>
        public virtual void Add(Channel item)
        {
            switch (item.Direction)
            {
                case ChannelDirection.Incoming:
                    if (mContainerIncoming.ContainsKey(item.Id))
                        throw new DuplicateChannelException(item.Id, item.Direction);

                    mContainerIncoming.Add(item.Id, item);
                    break;
                case ChannelDirection.Outgoing:
                    if (mContainerOutgoing.ContainsKey(item.Id))
                        throw new DuplicateChannelException(item.Id, item.Direction);

                    mContainerOutgoing.Add(item.Id, item);
                    break;
            }
        }
        #endregion
        #region Remove(Channel item)
        /// <summary>
        /// This method removes a channel from the collection.
        /// </summary>
        /// <param name="item">The channel item.</param>
        /// <returns>True if the channel is removed.</returns>
        public virtual bool Remove(Channel item)
        {
            switch (item.Direction)
            {
                case ChannelDirection.Incoming:
                    if (mContainerIncoming.ContainsKey(item.Id))
                        return mContainerIncoming.Remove(item.Id);
                    break;
                case ChannelDirection.Outgoing:
                    if (mContainerOutgoing.ContainsKey(item.Id))
                        return mContainerOutgoing.Remove(item.Id);
                    break;
            }

            return false;
        }
        #endregion

        /// <summary>
        /// This method checks whether a channel has been adeed.
        /// </summary>
        /// <param name="channelId">The channel Id.</param>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public bool Exists(string channelId, ChannelDirection direction)
        {
            switch (direction)
            {
                case ChannelDirection.Incoming:
                    if (mContainerIncoming.ContainsKey(channelId))
                        return true;
                    break;
                case ChannelDirection.Outgoing:
                    if (mContainerOutgoing.ContainsKey(channelId))
                        return true;
                    break;
            }

            return false;
        }
        /// <summary>
        /// This method attempts to retrieve and existing channel.
        /// </summary>
        /// <param name="channelId">The channel Id.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="channel">Returns the channel object.</param>
        /// <returns></returns>
        public bool TryGet(string channelId, ChannelDirection direction, out Channel channel)
        {
            switch (direction)
            {
                case ChannelDirection.Incoming:
                    return mContainerIncoming.TryGetValue(channelId, out channel);
                case ChannelDirection.Outgoing:
                    return mContainerOutgoing.TryGetValue(channelId, out channel);
            }

            channel = null;
            return false;
        }
    }
}