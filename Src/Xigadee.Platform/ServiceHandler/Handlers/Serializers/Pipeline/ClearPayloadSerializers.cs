﻿namespace Xigadee
{
    public static partial class CorePipelineExtensions
    {
        /// <summary>
        /// Clears the payload serializers collection..
        /// </summary>
        /// <typeparam name="P">The pipeline type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <returns>The pipeline.</returns>
        public static P ClearPayloadSerializers<P>(this P pipeline)
            where P : IPipeline
        {
            pipeline.Service.Serialization.ClearPayloadSerializers();

            return pipeline;
        }
    }
}
