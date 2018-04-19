using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FcnpParser
{
    class Fcnp
    {
        private struct HeaderEntry
        {
            public string name;
            public long offset;

            public uint unkStrCode32;
            public ulong nameOffset;
        } //struct

        private struct Entry
        {
            public string name;
            public long offset;

            public uint unkStrCode32;
            public ulong nameOffset;
            public uint unknown0; //Always 0x30?
            public uint unknown1; //Always 0x30?
            public uint unknown2; //Always 0xFFFFFFA0 except for first entry?
            public uint unknown3; //Always 0x60?
            public ulong parentSectionOffset;
            public Vector4[] vectors;
        } //struct

        private struct ParentInfo
        {
            public string parentString;
            public string parentName;
            public long offset0;
            public long offset1;

            public uint unknown0; //Always 0x1? I think this might just be an indicator for the previous section to end. It might also be number of things to parent though.
            public uint unkStrCode32_0;
            public uint nameOffset0;
            public uint unkStrCode32_1;
            public uint nameOffset1;
        } //struct

        private const int NUM_VECTORS = 3;

        private HeaderEntry headerEntry;
        private List<Entry> entries = new List<Entry>(0);
        private List<ParentInfo> parentInfoList = new List<ParentInfo>(0);

        private uint dataOffset;
        private uint fileLength;

        public void Read(FileStream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            long offset;

            reader.BaseStream.Position += 4;
            dataOffset = reader.ReadUInt32();
            fileLength = reader.ReadUInt32();

            headerEntry.offset = reader.BaseStream.Position;
            headerEntry.unkStrCode32 = reader.ReadUInt32();
            headerEntry.nameOffset = reader.ReadUInt64();
            headerEntry.name = ReadName(reader, headerEntry.offset + (long)headerEntry.nameOffset);
            reader.BaseStream.Position = dataOffset;

            while (true)
            {
                uint breakInt = reader.ReadUInt32();

                if (breakInt == 1)
                    break;

                Entry entry = new Entry();
                entry.offset = reader.BaseStream.Position - 4;
                entry.unkStrCode32 = breakInt;
                entry.nameOffset = reader.ReadUInt64();
                entry.unknown0 = reader.ReadUInt32();
                entry.unknown1 = reader.ReadUInt32();
                reader.BaseStream.Position += 0x8;
                entry.unknown2 = reader.ReadUInt32();
                entry.unknown3 = reader.ReadUInt32();
                entry.parentSectionOffset = reader.ReadUInt64();
                reader.BaseStream.Position += 0x4;
                entry.vectors = new Vector4[NUM_VECTORS];

                for (int i = 0; i < NUM_VECTORS; i++)
                    for (int j = 0; j < 4; j++)
                        entry.vectors[i][j] = reader.ReadSingle();

                offset = reader.BaseStream.Position;
                entry.name = ReadName(reader, entry.offset + (long)entry.nameOffset);
                reader.BaseStream.Position = offset;

                entries.Add(entry);
            } //while

            reader.BaseStream.Position -= 4;

            int entryCount = entries.Count;

            for (int i = 0; i < entryCount; i++)
            {
                ParentInfo parentInfo = new ParentInfo();

                parentInfo.unknown0 = reader.ReadUInt32();
                parentInfo.offset0 = reader.BaseStream.Position;
                parentInfo.unkStrCode32_0 = reader.ReadUInt32();
                parentInfo.nameOffset0 = reader.ReadUInt32();
                parentInfo.offset1 = reader.BaseStream.Position;
                parentInfo.unkStrCode32_1 = reader.ReadUInt32();
                parentInfo.nameOffset1 = reader.ReadUInt32();
                offset = reader.BaseStream.Position;
                parentInfo.parentString = ReadName(reader, parentInfo.offset0 + parentInfo.nameOffset0);
                parentInfo.parentName = ReadName(reader, parentInfo.offset1 + parentInfo.nameOffset1);
                reader.BaseStream.Position = offset;

                parentInfoList.Add(parentInfo);
            } //for
        } //Read

        public void WriteOutputToTextFile(FileStream stream)
        {
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append($"{headerEntry.name}");
            stringBuilder.Append($"\nStrCode32: 0x{headerEntry.unkStrCode32.ToString("x")}");
            stringBuilder.Append($"\n----------------------------------------------------------------");

            int entryCount = entries.Count;

            for (int i = 0; i < entryCount; i++)
            {
                stringBuilder.Append($"\n\nEntry: {i}");
                stringBuilder.Append($"\n----------------------------------------------------------------");
                stringBuilder.Append($"\nName: {entries[i].name}");
                stringBuilder.Append($"\nStrCode32: 0x{entries[i].unkStrCode32.ToString("x")}");
                stringBuilder.Append($"\nUnknown0: 0x{entries[i].unknown0.ToString("x")}");
                stringBuilder.Append($"\nUnknown1: 0x{entries[i].unknown1.ToString("x")}");
                stringBuilder.Append($"\nUnknown2: 0x{entries[i].unknown2.ToString("x")}");
                stringBuilder.Append($"\nUnknown3: 0x{entries[i].unknown3.ToString("x")}");

                for (int j = 0; j < NUM_VECTORS; j++)
                    stringBuilder.Append($"\nVector{j}: [{entries[i].vectors[j].x}, {entries[i].vectors[j].y}, {entries[i].vectors[j].z}, {entries[i].vectors[j].w}]");

                stringBuilder.Append("\n\nParent Info:");
                stringBuilder.Append($"\nUnknown0: {parentInfoList[i].unknown0}");
                stringBuilder.Append($"\nStrCode32_0: 0x{parentInfoList[i].unkStrCode32_0.ToString("x")}");
                stringBuilder.Append($"\nParent String: {parentInfoList[i].parentString}");
                stringBuilder.Append($"\nStrCode32_1: 0x{parentInfoList[i].unkStrCode32_1.ToString("x")}");
                stringBuilder.Append($"\nParent Name: {parentInfoList[i].parentName}");
            } //for

            writer.Write(stringBuilder);
        } //WriteOutputToTextFile

        private string ReadName(BinaryReader reader, long nameOffset)
        {
            reader.BaseStream.Position = nameOffset;

            string s = "";

            while (true)
            {
                char c = reader.ReadChar();

                if (c != 0)
                    s += c;
                else
                    break;
            } //while

            return s;
        } //ReadName
    } //class
} //namespace
