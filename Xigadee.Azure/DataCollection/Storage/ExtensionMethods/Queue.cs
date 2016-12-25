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
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;

namespace Xigadee
{
    public static partial class AzureStorageDCExtensions
    {
        public static AzureStorageContainerQueue DefaultQueueConverter(this EventBase e, MicroserviceId id)
        {
            var cont = new AzureStorageContainerQueue();

            var jObj = JObject.FromObject(e);
            var body = jObj.ToString();

            cont.Blob = Encoding.UTF8.GetBytes(body);

            return cont;
        }
    }
}
