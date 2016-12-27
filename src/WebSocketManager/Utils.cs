using System;

namespace WebSocketManager
{
    public static class Utils
    {
        public static byte[] GetBytes(string str, int bufferSize)
        {
            byte[] bytes = new byte[bufferSize];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new String(chars);
        }
    }
}