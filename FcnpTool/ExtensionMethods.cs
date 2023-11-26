using System.Collections.Generic;
using System.IO;

namespace FcnpTool
{
    public static class ExtensionMethods
    {
        public static void WriteZeroes(this BinaryWriter writer, int count)
        {
            if (count > 0)
            {
                byte[] array = new byte[count];
                writer.Write(array);
            } //if
        } //WriteZeroes
        public static string ReadCString(this BinaryReader reader)
        {
            var chars = new List<char>();
            var @char = reader.ReadChar();
            while (@char != '\0')
            {
                chars.Add(@char);
                @char = reader.ReadChar();
            }

            return new string(chars.ToArray());
        }
        public static void WriteCString(this BinaryWriter writer, string iString)
        {
            char[] stringChars = iString.ToCharArray();
            foreach (var chara in stringChars)
                writer.Write(chara);
        }
    } //class
} //namespace
