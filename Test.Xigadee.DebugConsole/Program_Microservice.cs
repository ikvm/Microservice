﻿using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Auth;
using Xigadee;

namespace Test.Xigadee
{
    static partial class Program
    {
        static Dictionary<string, string> sServerSettings = new Dictionary<string, string>();

        static Dictionary<string, string> sClientSettings = new Dictionary<string, string>();

        static void MicroserviceClientStart()
        {
            sContext.Persistence = sContext.Client.Persistence;

            sContext.Client.Service.StatusChanged += ClientStatusChanged;

            sContext.Client.Populate(ResolveClientSetting, true);
            sContext.Client.Start();
        }

        static void MicroserviceServerStart()
        {
            sContext.Server.OnRegister += Server_OnRegister;

            sContext.Server.Service.StatusChanged += ServerStatusChanged;

            sContext.Server.Service.StartRequested += ServerStartRequested;
            sContext.Server.Service.StopRequested += ServerStopRequested;

            sContext.Server.Populate(ResolveServerSetting, true);
            sContext.Server.Start();
        }

        static string ResolveServerSetting(string key, string value)
        {
            if (sServerSettings.ContainsKey(key))
                return sServerSettings[key];

            return null;
        }

        static string ResolveClientSetting(string key, string value)
        {
            if (sClientSettings.ContainsKey(key))
                return sServerSettings[key];

            return null;
        }

        static void MicroserviceLoadSettings()
        {

        }

        private static void Server_OnRegister(object sender, CommandRegisterEventArgs e)
        {
            switch (sContext.PersistenceType)
            {
                case PersistenceOptions.Sql:
                    //sContext.Server.Service.RegisterCommand(
                    //    new PersistenceMondayMorningBluesBlob(sContext.Server.Config.Storage));
                    break;
                case PersistenceOptions.Blob:
                    e.Service.RegisterCommand(
                        new PersistenceMondayMorningBluesBlob(e.Config.Storage
                        , sContext.Server.VersionMondayMorningBlues)
                        { ChannelId = Channels.TestB });
                    break;
                case PersistenceOptions.DocumentDb:
                    e.Service.RegisterCommand(
                        new PersistenceMondayMorningBluesDocDb(e.Config.DocDbCredentials, e.Config.DocumentDbName
                        , sContext.Server.VersionMondayMorningBlues)
                        { ChannelId = Channels.TestB });
                    break;
                case PersistenceOptions.RedisCache:
                    e.Service.RegisterCommand(
                        new PersistenceMondayMorningBluesRedis(e.Config.RedisCacheConnection
                        , sContext.Server.VersionMondayMorningBlues)
                        { ChannelId = Channels.TestB });
                    break;
            }
        }
    }
}