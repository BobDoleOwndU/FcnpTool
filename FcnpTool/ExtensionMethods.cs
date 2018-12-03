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
    } //class
} //namespace
