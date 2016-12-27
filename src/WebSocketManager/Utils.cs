using System;
using System.Text;

namespace WebSocketManager
{
    public static class Utils
    {
        public static byte[] GetBytes(string message)
        {
            return Encoding.ASCII.GetBytes(message);
        }

        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new String(chars);
        }
    }
}