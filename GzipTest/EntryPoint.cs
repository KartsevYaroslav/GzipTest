using System;
using System.Diagnostics;
using GzipTest.Model;
using GzipTest.Processor;

namespace GzipTest
{
    public class EntryPoint
    {
        public static int Main(string[] args)
        {
            var arguments = UserArgs.ParseAndValidate(args);
            if (arguments == null)
                return 1;

            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var processor = Gzip.Processor(arguments);
                processor.Process();
                
                stopwatch.Stop();
                Console.WriteLine($"elapsed {stopwatch.ElapsedMilliseconds}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }

            return 0;
        }
    }
}