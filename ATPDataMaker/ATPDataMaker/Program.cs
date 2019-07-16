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

            TextReader reader = new StreamReader("atpresults.csv");
            TextReader ranks = new StreamReader("atprankings.csv");
            TextWriter writer = new StreamWriter("atp.csv");
            TextWriter resWriter = new StreamWriter("atpres.csv");
            TextWriter testWriter = new StreamWriter("atpres_final.csv");

            var ig = new InputGetter(reader, ranks, writer, resWriter, testWriter);
            ig.MakeData();

            writer.Close();
            resWriter.Close();
            testWriter.Close();
            
        }
    }
}
