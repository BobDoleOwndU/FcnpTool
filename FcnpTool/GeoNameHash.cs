using System.IO;

namespace FcnpTool
{
    public static class GeoNameHash
    {
        public static void WriteGeoNameHash(this BinaryWriter writer, string text)
        {
            uint nameLength = (uint)text.Length;
            uint hash = nameLength;

            for (int i = (int)(nameLength - 1); i >= 0; i--)
                hash ^= text[i] + hash * 0x20 + (hash >> 2);
            writer.Write(hash);
        }
    }
}
