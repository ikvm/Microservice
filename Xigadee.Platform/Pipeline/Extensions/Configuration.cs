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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xigadee
{
    public static partial class CorePipelineExtensions
    {
        /// <summary>
        /// This method adds an override setting and clears the cache.
        /// </summary>
        /// <typeparam name="P">The pipeline type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="key">The key.</param>
        /// <param name="value"></param>
        /// <returns>Returns the pipeline.</returns>
        public static P ConfigurationOverrideSet<P>(this P pipeline, string key, string value) 
            where P : MicroservicePipeline
        {
            pipeline.Configuration.OverrideSettings.Add(key,value);
            pipeline.Configuration.CacheFlush();
            return pipeline;
        }

        /// <summary>
        /// This method clears all the registered ConfigResolvers in the configuration.
        /// </summary>
        /// <typeparam name="P">The pipeline type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <returns>Returns the pipeline.</returns>
        public static P ConfigResolversClear<P>(this P pipeline)
            where P : MicroservicePipeline
        {
            pipeline.Configuration.ResolversClear();

            return pipeline;
        }

        public static P ConfigResolverSet<P>(this P pipeline, int priority, ConfigResolver resolver, Action<ConfigResolver> assign = null)
            where P : MicroservicePipeline
        {
            if (pipeline == null)
                throw new ArgumentNullException("pipeline cannot be null");

            assign?.Invoke(resolver);

            pipeline.Configuration.ResolverSet(priority, resolver);

            return pipeline;
        }

        public static P ConfigResolverSet<P,R>(this P pipeline, int priority, Action<R> assign = null)
            where P : MicroservicePipeline
            where R : ConfigResolver, new()
        {
            if (pipeline == null)
                throw new ArgumentNullException("pipeline cannot be null");

            var resolver = new R();

            assign?.Invoke(resolver);

            pipeline.Configuration.ResolverSet(priority, resolver);

            return pipeline;
        }
    }
}