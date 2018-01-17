﻿using System;

namespace Xigadee
{
    /// <summary>
    /// This interface is responsible for the Microservice security.
    /// </summary>
    public interface IMicroserviceSerialization
    {
        /// <summary>
        /// Registers the payload serializer.
        /// </summary>
        /// <param name="fnSerializer">The serializer function.</param>
        /// <returns>The serializer.</returns>
        IPayloadSerializer RegisterPayloadSerializer(Func<IPayloadSerializer> fnSerializer);
        /// <summary>
        /// Registers the payload serializer.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The serializer.</returns>
        IPayloadSerializer RegisterPayloadSerializer(IPayloadSerializer serializer);
        /// <summary>
        /// Registers the payload compressor.
        /// </summary>
        /// <param name="compressor">The compressor creation function.</param>
        /// <returns>Returns the compressor.</returns>
        IPayloadCompressor RegisterPayloadCompressor(Func<IPayloadCompressor> compressor);
        /// <summary>
        /// Registers the payload compressor.
        /// </summary>
        /// <param name="compressor">The compressor.</param>
        /// <returns>Returns the compressor.</returns>
        IPayloadCompressor RegisterPayloadCompressor(IPayloadCompressor compressor);
        /// <summary>
        /// Clears the payload serializers.
        /// </summary>
        void ClearPayloadSerializers();
        /// <summary>
        /// Clears the payload compressors.
        /// </summary>
        void ClearPayloadCompressors();
        /// <summary>
        /// Gets the payload serializer count.
        /// </summary>
        int PayloadSerializerCount { get; }
        /// <summary>
        /// Gets the payload compressor count.
        /// </summary>
        int PayloadCompressorCount { get; }
    }
}
