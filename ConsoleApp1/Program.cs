using System;
using ConsoleApp8;

namespace ConsoleApp1
{
    class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Press enter to reset timer");
            var timeout = new ElectionTimeout();
            timeout.TimeoutReached += (sender, eventArgs) =>
            {
                Console.WriteLine("Timeout!");
            };
            while (true)
            {
                Console.ReadLine();
                timeout.Reset(TimeSpan.FromSeconds(2));
            }
        }
    }
}
