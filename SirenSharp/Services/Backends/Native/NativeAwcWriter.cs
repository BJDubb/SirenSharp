using System.Text;

namespace SirenSharp.Services.Backends.Native
{
    /// <summary>
    /// Writes AWC (Audio Wave Container) bytes from scratch for the siren case
    /// (mono 16-bit PCM, one stream per sound), with no dependency on CodeWalker. The
    /// binary layout is taken from the RAGE wave format and verified against known-good
    /// banks. Each stream carries a format chunk, an optional peak chunk, and a data chunk.
    /// </summary>
    public static class NativeAwcWriter
    {
        private const uint Magic = 0x54414441; // 'ADAT'
        private const ushort Version = 1;
        private const ushort FlagsChunkIndices = 0xFF01; // 0xFF00 base | chunk-indices bit

        // Chunk type = last byte of the Jenkins hash of the chunk name.
        private const byte ChunkFormat = 0xFA;
        private const byte ChunkData = 0x55;
        private const byte ChunkPeak = 0x36;

        private const int PeakWindow = 4096;
        private const short SirenHeadroom = -200; // millibels

        public sealed class Wave
        {
            public required string Name { get; init; }
            public required ushort SampleRate { get; init; }
            public required byte[] Pcm { get; init; } // mono 16-bit little-endian
            public uint SampleCount => (uint)(Pcm.Length / 2);
        }

        /// <summary>Jenkins one-at-a-time hash (lower-cased), matching RAGE/CodeWalker.</summary>
        public static uint JenkHash(string text)
        {
            var s = text.ToLowerInvariant();
            uint h = 0;
            foreach (var ch in Encoding.ASCII.GetBytes(s))
            {
                h += ch;
                h += h << 10;
                h ^= h >> 6;
            }
            h += h << 3;
            h ^= h >> 11;
            h += h << 15;
            return h;
        }

        public static byte[] Write(IEnumerable<Wave> waves)
        {
            // One stream per wave, ordered by (masked) hash like real banks.
            var streams = waves
                .Select(w => new StreamLayout(w))
                .OrderBy(s => s.Id)
                .ToList();

            var streamCount = streams.Count;
            var totalChunks = streams.Sum(s => s.Chunks.Count);

            // Region offsets (relative to file start).
            var infoStart = 16 + streamCount * 2;            // header + chunk indices (u16 each)
            var dataStart = infoStart + streamCount * 4 + totalChunks * 8; // + stream infos + chunk infos

            // Assign each chunk an absolute offset, applying 16-byte alignment to data chunks.
            var pos = dataStart;
            foreach (var s in streams)
            {
                foreach (var c in s.Chunks)
                {
                    if (c.Align > 0)
                    {
                        var pad = (c.Align - (pos % c.Align)) % c.Align;
                        pos += pad;
                    }
                    c.Offset = pos;
                    pos += c.Size;
                }
            }

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);

            // Header
            w.Write(Magic);
            w.Write(Version);
            w.Write(FlagsChunkIndices);
            w.Write(streamCount);
            w.Write(dataStart);

            // Chunk indices: index of each stream's first chunk in the global chunk list.
            ushort runningChunkIndex = 0;
            foreach (var s in streams)
            {
                w.Write(runningChunkIndex);
                runningChunkIndex += (ushort)s.Chunks.Count;
            }

            // Stream infos: (Id & 0x1FFFFFFF) | (chunkCount << 29)
            foreach (var s in streams)
            {
                var raw = (s.Id & 0x1FFFFFFF) | ((uint)s.Chunks.Count << 29);
                w.Write(raw);
            }

            // Chunk infos: (offset & 0xFFFFFFF) | ((size & 0xFFFFFFF) << 28) | ((ulong)type << 56)
            foreach (var s in streams)
            {
                foreach (var c in s.Chunks)
                {
                    var raw = ((ulong)(uint)c.Offset & 0x0FFFFFFF)
                              | (((ulong)(uint)c.Size & 0x0FFFFFFF) << 28)
                              | ((ulong)c.Type << 56);
                    w.Write(raw);
                }
            }

            // Chunk data, in the same order as the chunk infos, honouring alignment padding.
            foreach (var s in streams)
            {
                foreach (var c in s.Chunks)
                {
                    if (c.Align > 0)
                    {
                        var pad = (c.Align - ((int)ms.Position % c.Align)) % c.Align;
                        if (pad > 0) w.Write(new byte[pad]);
                    }
                    c.WriteData(w);
                }
            }

            w.Flush();
            return ms.ToArray();
        }

        private sealed class StreamLayout
        {
            public uint Id { get; }
            public List<Chunk> Chunks { get; }

            public StreamLayout(Wave wave)
            {
                Id = JenkHash(wave.Name) & 0x1FFFFFFF;

                var (peak0, peaks) = ComputePeaks(wave);

                var format = Chunk.Format(wave.SampleCount, wave.SampleRate, peak0);
                var data = Chunk.Data(wave.Pcm);

                Chunks = new List<Chunk> { format };
                if (peaks.Length > 0) Chunks.Add(Chunk.Peak(peaks));
                Chunks.Add(data);
            }

            // Mirrors RAGE/CodeWalker peak generation: peak0 is the first-window peak stored
            // in the format chunk; the remaining windows go in the peak chunk.
            private static (ushort first, ushort[] rest) ComputePeaks(Wave wave)
            {
                var samples = (int)wave.SampleCount;
                var pcm = wave.Pcm;

                ushort Sample(int i)
                {
                    var ei = i * 2;
                    if (ei + 2 >= pcm.Length) return 0;
                    var smp = BitConverter.ToInt16(pcm, ei);
                    return (ushort)Math.Min(Math.Abs((int)smp) * 2, 65535);
                }

                ushort Window(int n)
                {
                    var o = n * PeakWindow;
                    ushort p = 0;
                    for (var i = 0; i < PeakWindow; i++) p = Math.Max(p, Sample(i + o));
                    return p;
                }

                var first = Window(0);
                var count = (samples - PeakWindow) / PeakWindow;
                if (count < 0) count = 0;
                var rest = new ushort[count];
                for (var n = 0; n < count; n++) rest[n] = Window(n + 1);
                return (first, rest);
            }
        }

        private sealed class Chunk
        {
            public byte Type { get; private init; }
            public int Align { get; private init; }
            public int Size { get; private init; }
            public int Offset { get; set; }

            private Action<BinaryWriter> writer = null!;
            public void WriteData(BinaryWriter w) => writer(w);

            public static Chunk Format(uint sampleCount, ushort sampleRate, ushort peakVal) => new()
            {
                Type = ChunkFormat,
                Align = 0,
                Size = 24,
                writer = w =>
                {
                    w.Write(sampleCount);          // Samples (u32)
                    w.Write(0);                    // LoopPoint (i32)
                    w.Write(sampleRate);           // SamplesPerSecond (u16)
                    w.Write(SirenHeadroom);        // Headroom (i16, mB)
                    w.Write((ushort)0);            // LoopBegin
                    w.Write((ushort)0);            // LoopEnd
                    w.Write((ushort)0);            // PlayEnd
                    w.Write((byte)0);              // PlayBegin
                    w.Write((byte)0);              // Codec (0 = PCM)
                    w.Write((uint)peakVal);        // Peak (PeakUnk=0 | PeakVal)
                },
            };

            public static Chunk Peak(ushort[] peaks) => new()
            {
                Type = ChunkPeak,
                Align = 0,
                Size = peaks.Length * 2,
                writer = w => { foreach (var p in peaks) w.Write(p); },
            };

            public static Chunk Data(byte[] pcm) => new()
            {
                Type = ChunkData,
                Align = 16,
                Size = pcm.Length,
                writer = w => w.Write(pcm),
            };
        }
    }
}
