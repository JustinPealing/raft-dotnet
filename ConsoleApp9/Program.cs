using System;
using System.Net.Sockets;
using System.Text;

namespace ConsoleApp8
{
    class Program
    {
        static void Main(string[] args)
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
