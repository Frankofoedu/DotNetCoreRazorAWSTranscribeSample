using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace awsTest
{
    class Program
    {
         static  List<string> lis = new List<string>();
        public static void Main()
        {
            var mp3Path = @"C:\Users\Frank\Documents\musicTest\Toast.mp3";

            var mp3Length = new Mp3FileReader(mp3Path).TotalTime;

            #region get date time part of the file name

            var s = "Toast.trimmed--637002750572206987";

            var t = new DateTime(Convert.ToInt64(s.Substring(s.LastIndexOf('-') + 1)));

            #endregion

            DoTrim(mp3Path, mp3Length);

            var files = Directory.GetFiles(Path.GetDirectoryName(mp3Path));

            foreach (var item in files)
            {

            }

            DeleteFiles(files);

        }

        private static void DeleteFiles(string[] x)
        {
            foreach (var item in x)
            {
                if (!item.Contains("trimmed"))
                    continue;
                else
                    File.Delete(item);
            }
        }

        static void TrimMp3(string inputPath, string outputPath, TimeSpan? begin, TimeSpan? end)
        {
            if (begin.HasValue && end.HasValue && begin > end)
                throw new ArgumentOutOfRangeException("end", "end should be greater than begin");

            using var reader = new Mp3FileReader(inputPath);

            using var writer = File.Create(outputPath);
            Mp3Frame frame;
            while ((frame = reader.ReadNextFrame()) != null)
                if (reader.CurrentTime >= begin || !begin.HasValue)
                {
                    if (reader.CurrentTime <= end || !end.HasValue)
                        writer.Write(frame.RawData, 0, frame.RawData.Length);
                    else break;
                }
        }


        static void DoTrim(string inputPath, TimeSpan length)
        {
           
            var skip = new TimeSpan(0, 0, 10);


            if (length > skip)
            {
                var t = Convert.ToInt32(length.Ticks / skip.Ticks);

                var begin = new TimeSpan();

                for (int i = 0; i < t; i++)
                {

                    var end = (i + 1) * skip;
                  
                    var outputPath = Path.ChangeExtension(inputPath, $".trimmed--{DateTime.Now.Ticks.ToString()}");
                    TrimMp3(inputPath, outputPath, begin, end);
                    lis.Add(Path.GetFileName(outputPath));
                    begin = end;
                }

                var remaninder = length - (Convert.ToInt32(t) * skip);

                if (remaninder != new TimeSpan(0, 0, 0))
                {
                    var outputPath = Path.ChangeExtension(inputPath, $".trimmed--{DateTime.Now.Ticks.ToString()}");
                    TrimMp3(inputPath, outputPath, length - remaninder, length);
                    lis.Add(Path.GetFileName(outputPath));
                }

            }
            else
            {
                var outputPath = Path.ChangeExtension(inputPath, $".trimmed--{DateTime.Now.Ticks.ToString()}");
                TrimMp3(inputPath, outputPath, new TimeSpan(), length);
                lis.Add(Path.GetFileName(outputPath));
                //do work with length
            }


        }
    }
}
