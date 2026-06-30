using SirenSharp.Models;

namespace SirenSharp.Services.Backends.Native
{
    /// <summary>
    /// Reads an AWC back and validates it without CodeWalker, so the native backend can
    /// verify its own output end to end. Parses the header/stream/chunk tables (see
    /// <see cref="NativeAwcWriter"/>) and checks each stream is non-empty, PCM, and not silent.
    /// </summary>
    public static class NativeAwcValidator
    {
        private const uint Magic = 0x54414441;
        private const byte ChunkFormat = 0xFA;
        private const byte ChunkData = 0x55;
        private const int MinHealthyBytes = 2048;

        public static AwcVerificationResult Verify(string soundSetName, string awcFilePath)
        {
            var result = new AwcVerificationResult { SoundSetName = soundSetName, FilePath = awcFilePath };

            if (!File.Exists(awcFilePath))
            {
                result.ErrorMessage = "AWC file was not created.";
                return result;
            }

            var bytes = File.ReadAllBytes(awcFilePath);
            result.FileSizeBytes = bytes.Length;

            if (bytes.Length < MinHealthyBytes)
            {
                result.ErrorMessage = $"AWC is only {bytes.Length} bytes - likely silent or broken.";
                return result;
            }

            try
            {
                if (BitConverter.ToUInt32(bytes, 0) != Magic)
                {
                    result.ErrorMessage = "Not an AWC (bad magic).";
                    return result;
                }

                var flags = BitConverter.ToUInt16(bytes, 6);
                var streamCount = BitConverter.ToInt32(bytes, 8);
                result.StreamCount = streamCount;
                if (streamCount <= 0)
                {
                    result.ErrorMessage = "AWC contains no audio streams.";
                    return result;
                }

                var chunkIndices = (flags & 1) == 1;
                var pos = 16 + (chunkIndices ? streamCount * 2 : 0);

                // Stream infos: chunk count packed in the top 3 bits.
                var chunkCounts = new int[streamCount];
                var ids = new uint[streamCount];
                for (int i = 0; i < streamCount; i++)
                {
                    var raw = BitConverter.ToUInt32(bytes, pos);
                    pos += 4;
                    ids[i] = raw & 0x1FFFFFFF;
                    chunkCounts[i] = (int)(raw >> 29);
                }

                for (int i = 0; i < streamCount; i++)
                {
                    int dataOffset = 0, dataSize = 0, rate = 0;
                    uint samples = 0;
                    var sawFormat = false;

                    for (int j = 0; j < chunkCounts[i]; j++)
                    {
                        var raw = BitConverter.ToUInt64(bytes, pos);
                        pos += 8;
                        var type = (byte)(raw >> 56);
                        var size = (int)((raw >> 28) & 0x0FFFFFFF);
                        var offset = (int)(raw & 0x0FFFFFFF);

                        if (type == ChunkFormat)
                        {
                            samples = BitConverter.ToUInt32(bytes, offset);
                            rate = BitConverter.ToUInt16(bytes, offset + 8);
                            sawFormat = true;
                        }
                        else if (type == ChunkData)
                        {
                            dataOffset = offset;
                            dataSize = size;
                        }
                    }

                    if (!sawFormat || dataSize == 0)
                    {
                        result.ErrorMessage = $"Stream {ids[i]:X} is missing format or data.";
                        return result;
                    }

                    var streamResult = new AwcStreamVerification
                    {
                        Name = $"0x{ids[i]:X}",
                        DataBytes = dataSize,
                        SampleRate = rate,
                        SampleCount = samples,
                        IsSilent = dataSize < 256 || IsMostlySilent(bytes, dataOffset, dataSize),
                    };
                    result.Streams.Add(streamResult);
                }

                if (result.Streams.All(s => s.IsSilent))
                {
                    result.ErrorMessage = "All streams appear silent or have no PCM data.";
                    return result;
                }
                if (result.Streams.Any(s => s.IsSilent))
                {
                    var silent = string.Join(", ", result.Streams.Where(s => s.IsSilent).Select(s => s.Name));
                    result.ErrorMessage = $"Some streams appear silent: {silent}";
                    return result;
                }

                result.IsHealthy = true;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private static bool IsMostlySilent(byte[] data, int offset, int size)
        {
            var sampleCount = Math.Min(size / 2, 8000);
            var loud = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                var smp = BitConverter.ToInt16(data, offset + i * 2);
                if (smp != 0) loud++;
            }
            return loud < sampleCount * 0.01;
        }
    }
}
