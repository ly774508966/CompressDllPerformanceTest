using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ComparePressAndSerailzer
{
    class Program
    {
        static void Main(string[] args)
        {


            string str = File.ReadAllText(@"source.txt");
            Console.WriteLine("SourceFileSize\t"+str.Length.ToString("N0"));

            var snappybin = SnappyCompress(str);
            var lz4bin = LZ4Compress(str);
            var gzipbin = GZipCompress(str);

            #region codetimer 测试代码性能
            CodeTimer.Initialize();

            var count = 100;

            CodeTimer.Time("SnappyCompress\t" + snappybin.Length.ToString("N0"), count, () => { SnappyCompress(str); });
            CodeTimer.Time("LZ4Compress\t" + lz4bin.Length.ToString("N0"), count, () => { LZ4Compress(str); });
            CodeTimer.Time("GZipCompress\t" + gzipbin.Length.ToString("N0"), count, () => { GZipCompress(str); });


            CodeTimer.Time("SnappyUnCompress", count, () => { SnappyUnCompress(snappybin); });
            CodeTimer.Time("LZ4UnCompress", count, () => { LZ4UnCompress(lz4bin); });
            CodeTimer.Time("GZipUnCompress", count, () => { GZipUnCompress(gzipbin); });

            #endregion

            Console.Read();
        }



        private static byte[] LZ4Compress(string input)
        {

            byte[] buffer = Encoding.UTF8.GetBytes(input);
            //  var aaa = LZ4.LZ4Codec.CodecName;
            var result = LZ4.LZ4Codec.Wrap(buffer);
            return result;
        }


        public static string LZ4UnCompress(byte[] data)
        {
            var result = LZ4.LZ4Codec.Unwrap(data);
            return Encoding.UTF8.GetString(result);
        }



        private static byte[] SnappyCompress(string input)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(input);
            var result = Snappy.SnappyCodec.Compress(buffer);
            return result;
        }


        public static string SnappyUnCompress(byte[] data)
        {
            var result = Snappy.SnappyCodec.Uncompress(data);
            return Encoding.UTF8.GetString(result);
        }



        private static byte[] GZipCompress(string input)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(input);

            using (MemoryStream msTemp = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(msTemp, CompressionMode.Compress, true))
                {
                    gz.Write(buffer, 0, buffer.Length);
                }
                return msTemp.GetBuffer();
            }


        }


        public static string GZipUnCompress(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                byte[] buffer = new byte[0x1000];
                int length = 0;

                using (GZipStream gz = new GZipStream(stream, CompressionMode.Decompress))
                {
                    using (MemoryStream msTemp = new MemoryStream())
                    {
                        while ((length = gz.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            msTemp.Write(buffer, 0, length);
                        }

                        return Encoding.UTF8.GetString(msTemp.ToArray());
                    }
                }
            }
        }








        public static class CodeTimer
        {
            public static void Initialize()
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                Time("", 1, () => { });
            }

            public static void Time(string name, int iteration, Action action)
            {
                if (String.IsNullOrEmpty(name)) return;

                // 1.
                ConsoleColor currentForeColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(name);

                // 2.
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                int[] gcCounts = new int[GC.MaxGeneration + 1];
                for (int i = 0; i <= GC.MaxGeneration; i++)
                {
                    gcCounts[i] = GC.CollectionCount(i);
                }

                // 3.
                Stopwatch watch = new Stopwatch();
                watch.Start();
                ulong cycleCount = GetCycleCount();
                for (int i = 0; i < iteration; i++) action();
                ulong cpuCycles = GetCycleCount() - cycleCount;
                watch.Stop();

                // 4.
                Console.ForegroundColor = currentForeColor;
                Console.WriteLine("\tTime Elapsed:\t" + watch.ElapsedMilliseconds.ToString("N0") + "ms");
                Console.WriteLine("\tTime AVG:\t" + (watch.ElapsedMilliseconds / iteration).ToString("N0") + "ms");
                Console.WriteLine("\tCPU Cycles:\t" + cpuCycles.ToString("N0"));

                // 5.
                for (int i = 0; i <= GC.MaxGeneration; i++)
                {
                    int count = GC.CollectionCount(i) - gcCounts[i];
                    Console.WriteLine("\tGen " + i + ": \t\t" + count);
                }

                Console.WriteLine();
            }

            private static ulong GetCycleCount()
            {
                ulong cycleCount = 0;
                QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
                return cycleCount;
            }

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool QueryThreadCycleTime(IntPtr threadHandle, ref ulong cycleTime);

            [DllImport("kernel32.dll")]
            static extern IntPtr GetCurrentThread();


        }
    }
}
