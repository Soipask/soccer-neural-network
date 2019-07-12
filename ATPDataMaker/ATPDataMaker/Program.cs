using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace ATPDataMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            if (File.Exists(args[0]))
            {
                TextReader reader = new StreamReader(args[0]);
                TextReader ranks = new StreamReader("atprankings.csv");
                TextWriter writer = new StreamWriter("atp2.csv");
                TextWriter resWriter = new StreamWriter("atpres2.csv");
                TextWriter testWriter = new StreamWriter("atpres_final2.csv");

                var ig = new InputGetter(reader, ranks, writer, resWriter, testWriter);
                ig.MakeData();

                writer.Close();
                resWriter.Close();
                testWriter.Close();
            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
            Console.ReadLine();
        }
    }
}
