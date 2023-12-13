using System;
using System.Diagnostics;

namespace FloatingPointPerformance
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define the number of iterations
            int loop = 6*1000000;

            // Define the variables for each data type
            float a = 1000, b = 45, c = 12000, d = 2, e = 7, f = 1024;
            double g = 1000, h = 45, i = 12000, j = 2, k = 7, l = 1024;
            decimal m = 1000, n = 45, o = 12000, p = 2, q = 7, r = 1024;

            Console.WriteLine("Working...");

            // Create a stopwatch to measure the execution time
            Stopwatch stopwatch = new Stopwatch();

            // Test the float operations
            stopwatch.Start();
            for (int x = 0; x < loop; x++)
            {
                a = (float)Math.Sin(a);
                b = (float)Math.Asin(b);
                c = (float)Math.Sqrt(c);
                d = d + d - d + d;
                e = e * e + e * e;
                f = f / f / f / f / f;
            }
            stopwatch.Stop();
            Console.WriteLine("Float: {0} ms", stopwatch.ElapsedMilliseconds);

            // Test the double operations
            stopwatch.Restart();
            for (int x = 0; x < loop; x++)
            {
                g = Math.Sin(g);
                h = Math.Asin(h);
                i = Math.Sqrt(i);
                j = j + j - j + j;
                k = k * k + k * k;
                l = l / l / l / l / l;
            }
            stopwatch.Stop();
            Console.WriteLine("Double: {0} ms", stopwatch.ElapsedMilliseconds);

            // Test the decimal operations
            stopwatch.Restart();
            for (int x = 0; x < loop; x++)
            {
                m = (decimal)Math.Sin((double)m);
                n = (decimal)Math.Asin((double)n);
                o = (decimal)Math.Sqrt((double)o);
                p = p + p - p + p;
                q = q * q + q * q;
                r = r / r / r / r / r;
            }
            stopwatch.Stop();
            Console.WriteLine("Decimal: {0} ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
