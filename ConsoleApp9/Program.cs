using System;
using System.Net.Sockets;
using System.Text;

namespace ConsoleApp8
{
    class Program
    {
        static void Main(string[] args)
        {
            //SendMessages();

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

        private static void SendMessages()
        {
            using (var client = new TcpClient("localhost", 13000))
            {
                Write("Hello, World!", client);
                Write("Hello, Again!", client);
            }
        }

        private static void Write(string message, TcpClient client)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var header = BitConverter.GetBytes(buffer.Length);
            var stream = client.GetStream();
            stream.Write(header, 0, header.Length);
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
