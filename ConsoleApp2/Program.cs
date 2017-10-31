using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp8
{
    class Program
    {
        static void Main(string[] args)
        {
            Listen();
            Console.WriteLine("Reading, press enter to exit.");
            Console.ReadLine();
        }
        
        private static void Listen()
        {
            var listener = new TcpListener(IPAddress.Loopback, 13000);
            listener.Start();
            BeginAcceptClient(listener);
        }

        private static void BeginAcceptClient(TcpListener listener)
        {
            listener.BeginAcceptTcpClient(ar =>
            {
                var client = listener.EndAcceptTcpClient(ar);
                ClientLoop(client);
                BeginAcceptClient(listener);
            }, null);
        }

        private static async Task ClientLoop(TcpClient client)
        {
            try
            {
                await ClientLoopInner(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("Client disconnected");
        }

        private static async Task ClientLoopInner(TcpClient client)
        {
            Console.WriteLine("Client connected");
            using (var stream = client.GetStream())
            {
                while (client.Connected)
                {
                    var header = await stream.ReadExactlyAsync(4);
                    if (header.Length < 4)
                    {
                        // Means that the client disconnected
                        return;
                    }
                    var size = BitConverter.ToInt32(header, 0);
                    var body = await stream.ReadExactlyAsync(size);
                    if (body.Length < size)
                    {
                        // Means that the client disconnected
                        return;
                    }
                    Console.WriteLine(Encoding.UTF8.GetString(body));
                }
            }
        }
    }
}
