using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core
{
    using System.Buffers.Binary;
    using System.Text;
    using System.Text.Json;

    public class PipeSerializer
    {
        private readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public async Task SerializeAsync<T>(
            Stream stream,
            T value,
            CancellationToken ct = default)
        {
            // Serialize object to UTF8 JSON bytes
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

            // Create 4-byte length prefix
            byte[] lengthPrefix = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, payload.Length);

            // Write length + payload
            await stream.WriteAsync(lengthPrefix, ct);
            await stream.WriteAsync(payload, ct);

            await stream.FlushAsync(ct);
        }

        public async Task<T?> DeserializeAsync<T>(
            Stream stream,
            CancellationToken ct = default)
        {
            // Read 4-byte length prefix
            byte[] lengthPrefix = await ReadExactAsync(stream, 4, ct);

            int length = BinaryPrimitives.ReadInt32LittleEndian(lengthPrefix);

            if (length <= 0)
                throw new InvalidDataException($"Invalid payload length: {length}");

            // Read payload
            byte[] payload = await ReadExactAsync(stream, length, ct);

            return JsonSerializer.Deserialize<T>(payload, JsonOptions);
        }

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
