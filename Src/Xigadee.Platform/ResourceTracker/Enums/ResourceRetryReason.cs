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

namespace Xigadee
{
    /// <summary>
    /// This is the reason for a retry attempt on a resource
    /// </summary>
    public enum ResourceRetryReason
    {
        /// <summary>
        /// The underlying resource is throttling traffic.
        /// </summary>
        Throttle,
        /// <summary>
        /// These was a timeout when accessing the resource.
        /// </summary>
        Timeout,
        /// <summary>
        /// An exception occurred when accessing the resource.
        /// </summary>
        Exception,
        /// <summary>
        /// The retry reason was not specified.
        /// </summary>
        Other
    }
}