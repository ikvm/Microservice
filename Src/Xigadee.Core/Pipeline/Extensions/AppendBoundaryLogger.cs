﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xigadee
{
    /// <summary>
    /// These extension methods allow the channel to auto-set a boundary logger.
    /// </summary>
    public static partial class CorePipelineExtensions
    {
        /// <summary>
        /// This method adds a boundary logger to the channel.
        /// </summary>
        /// <typeparam name="P">The channel pipeline type - incoming or outgoing.</typeparam>
        /// <typeparam name="L">The boundary logger</typeparam>
        /// <param name="cpipe">The pipe.</param>
        /// <param name="boundaryLogger">The logger.</param>
        /// <param name="action">The action that is called when the logger is added.</param>
        /// <returns>Returns the pipe.</returns>
        public static P AppendBoundaryLogger<P,L>(this P cpipe
            , L boundaryLogger
            , Action<P,L> action = null
            )
            where P: ChannelPipelineBase
            where L: IBoundaryLogger
        {

            action?.Invoke(cpipe,boundaryLogger);
            cpipe.Channel.BoundaryLogger = boundaryLogger;

            return cpipe;
        }

        /// <summary>
        /// This method adds a boundary logger to the channel.
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <typeparam name="L"></typeparam>
        /// <param name="cpipe">The pipe.</param>
        /// <param name="creator">This function is used to create the boundary logger.</param>
        /// <param name="action">The action that is called when the logger is added.</param>
        /// <returns>Returns the pipe.</returns>
        public static P AppendBoundaryLogger<P,L>(this P cpipe
            , Func<IEnvironmentConfiguration, L> creator
            , Action<P,L> action = null
            )
            where P : ChannelPipelineBase
            where L : IBoundaryLogger
        {
            var bLogger = creator(cpipe.Pipeline.Configuration);

            return cpipe.AppendBoundaryLogger(bLogger, action);
        }
    }
}
