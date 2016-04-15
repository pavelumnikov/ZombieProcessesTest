using System;
using System.Diagnostics;
using System.Threading;

namespace TestProcessChild
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Child[{Process.GetCurrentProcess().Id}]: I am child process! I'm still running!");
            }
        }
    }
}
