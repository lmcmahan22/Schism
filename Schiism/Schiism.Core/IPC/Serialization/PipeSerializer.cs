// <copyright file="PipeSerializer.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.IPC.Serialization
{
    using System.Buffers.Binary;
    using System.Text.Json;

    /// <summary>
    /// Pipe Serializer class for serializing and deserializing objects send/received along named pipes.
    /// Use a length-prefixed protocol: first send 4 bytes indicating the length of the JSON payload, followed by the UTF8-encoded JSON bytes.
    /// </summary>
    public class PipeSerializer
    {
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        /// <summary>
        /// Method to serialize an object to a stream using a length-prefixed protocol.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="stream">The stream to write the serialized data to.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task representing the asynchronous serializing operation.</returns>
        public async Task SerializeAsync<T>(
            Stream stream,
            T value,
            CancellationToken ct = default)
        {
            // Serialize object to UTF8 JSON bytes
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, jsonOptions);

            // Create 4-byte length prefix
            byte[] lengthPrefix = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, payload.Length);

            // Write length + payload
            await stream.WriteAsync(lengthPrefix, ct);
            await stream.WriteAsync(payload, ct);

            await stream.FlushAsync(ct);
        }

        /// <summary>
        /// Method to deserlialize an object from a stream using a length-prefixed protocol.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="stream">The stream to read the serialized data from.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task representing the asynchronous deserializing operation, with the deserialized object as the result.</returns>
        /// <exception cref="InvalidDataException">Thrown when the payload length is invalid.</exception>
        public async Task<T?> DeserializeAsync<T>(
            Stream stream,
            CancellationToken ct = default)
        {
            byte[] lengthPrefix = await ReadExactAsync(stream, 4, ct);

            int length = BinaryPrimitives.ReadInt32LittleEndian(lengthPrefix);

            if (length <= 0)
            {
                throw new InvalidDataException($"Invalid payload length: {length}");
            }

            byte[] payload = await ReadExactAsync(stream, length, ct);

            return JsonSerializer.Deserialize<T>(payload, jsonOptions);
        }

        /// <summary>
        /// Read the exact number of bytes from the stream, handling cases where stream.ReadAsync may return fewer bytes than requested.
        /// Throws EndOfStreamException if the stream ends before the expected number of bytes are read.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task representing the asynchronous read operation, with the read bytes as the result.</returns>
        /// <exception cref="EndOfStreamException">Thrown when the stream ends before the expected number of bytes are read.</exception>
        private async Task<byte[]> ReadExactAsync(
            Stream stream,
            int length,
            CancellationToken ct)
        {
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                int read = await stream.ReadAsync(
                    buffer.AsMemory(offset, length - offset),
                    ct);

                if (read == 0)
                {
                    throw new EndOfStreamException("Pipe closed before expected bytes were read");
                }

                offset += read;
            }

            return buffer;
        }
    }
}
