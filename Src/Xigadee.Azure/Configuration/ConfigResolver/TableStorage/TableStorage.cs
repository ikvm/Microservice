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
    public static partial class AzureExtensionMethods
    {
        /// <summary>
        /// This is the key definition for the table storage config holder.
        /// </summary>
        [ConfigSettingKey("TableStorage")]
        public const string KeyTableStorageConfigSASKey = "TableStorageConfigSASKey";

        /// <summary>
        /// This shortcut setting can be used to resolve the SAS key.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>Returns the string.</returns>
        [ConfigSetting("TableStorage")]
        public static string TableStorageConfigSASKey(this IEnvironmentConfiguration config) => config.PlatformOrConfigCache(KeyTableStorageConfigSASKey);


    }
}
