using System.IO;
using System.Threading.Tasks;

namespace ConsoleApp8
{
    public static class ReadExactlyExtension
    {
        public static byte[] ReadExactly(this Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += read;
            }
            System.Diagnostics.Debug.Assert(offset == count);
            return buffer;
        }

        public static async Task<byte[]> ReadExactlyAsync(this Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer, offset, count - offset);
                if (read == 0)
                {
                    return new byte[0];
                }
                offset += read;
            }
            System.Diagnostics.Debug.Assert(offset == count);
            return buffer;
        }
    }
}
