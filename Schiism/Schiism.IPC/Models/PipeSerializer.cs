using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Schiism.IPC.Models
{
    public class PipeSerializer
    {
        public static async Task SerializeAsync<T>(Stream stream, T obj, CancellationToken ct)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(obj);
            var length = BitConverter.GetBytes(bytes.Length);

            // Write the length of the data first, followed by the actual data
            // Frame this, so you know how to read it back on the other side
            await stream.WriteAsync(length, ct);
            await stream.WriteAsync(bytes, ct);
        }

        public static async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken ct)
        {
            // Length should be defined as the first 4 bytes of the stream, so read that first to know how many bytes to read for the actual data
            var lengthBuffer = new byte[4];
            await stream.ReadExactlyAsync(lengthBuffer, ct);

            var length = BitConverter.ToInt32(lengthBuffer);
            var dataBuffer = new byte[length];

            await stream.ReadExactlyAsync(dataBuffer, ct);

            return JsonSerializer.Deserialize<T>(dataBuffer)!;
        }
    }
}
