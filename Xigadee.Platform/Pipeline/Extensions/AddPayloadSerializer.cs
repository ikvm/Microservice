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
        public static MicroservicePipeline AddPayloadSerializerDefaultJson(this MicroservicePipeline pipeline)
        {
            var component = pipeline.Service.RegisterPayloadSerializer(new JsonContractSerializer());

            return pipeline;
        }

        public static MicroservicePipeline AddPayloadSerializer(this MicroservicePipeline pipeline, IPayloadSerializer serializer)
        {
            pipeline.Service.RegisterPayloadSerializer(serializer);

            return pipeline;
        }

        public static MicroservicePipeline ClearPayloadSerializers(this MicroservicePipeline pipeline, IPayloadSerializer serializer)
        {
            

            return pipeline;
        }

        public static MicroservicePipeline AddPayloadSerializer(this MicroservicePipeline pipeline, Func<IEnvironmentConfiguration, IPayloadSerializer> creator)
        {
            pipeline.Service.RegisterPayloadSerializer(creator(pipeline.Configuration));

            return pipeline;
        }
    }
}
