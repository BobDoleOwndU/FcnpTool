using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace FcnpTool
{
    [XmlType]
    public class Fcnp
    {
        [XmlType]
        public struct HeaderEntry
        {
            [XmlElement]
            public string name;

            [XmlIgnore]
            public long offset;

            [XmlElement]
            public uint unkStrCode32;

            [XmlIgnore]
            public ulong nameOffset;
        } //struct

        [XmlType]
        public class Entry
        {
            [XmlElement]
            public string name;

            [XmlIgnore]
            public long offset;

            [XmlElement]
            public uint unkStrCode32;

            [XmlIgnore]
            public ulong nameOffset;

            [XmlElement]
            public uint unknown0; //Always 0x30?

            [XmlElement]
            public uint unknown1; //Always 0x30?

            [XmlElement]
            public uint unknown2; //Always 0xFFFFFFA0 except for first entry?

            [XmlElement]
            public uint unknown3; //Always 0x60?

            [XmlIgnore]
            public ulong parentSectionOffset;

            [XmlElement]
            public Vector4 position;

            [XmlElement]
            public Vector4 rotation;

            [XmlElement]
            public Vector4 scale;

            [XmlElement]
            public ParentInfo parentInfo;
        } //struct

        [XmlType]
        public class ParentInfo
        {
            [XmlElement]
            public string parentString;

            [XmlElement]
            public string parentName;

            [XmlIgnore]
            public long offset0;

            [XmlIgnore]
            public long offset1;

            [XmlElement]
            public uint unknown0; //Always 0x1? I think this might just be an indicator for the previous section to end. It might also be number of things to parent though.

            [XmlElement]
            public uint unkStrCode32_0;

            [XmlIgnore]
            public uint nameOffset0;

            [XmlElement]
            public uint unkStrCode32_1;

            [XmlIgnore]
            public uint nameOffset1;
        } //struct

        [XmlElement]
        public HeaderEntry headerEntry;

        [XmlArray]
        public List<Entry> entries = new List<Entry>(0);

        [XmlIgnore]
        public uint dataOffset;

        [XmlIgnore]
        public uint fileLength;

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
                entry.position = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                entry.rotation = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                entry.scale = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
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

                entries[i].parentInfo = parentInfo;

                reader.BaseStream.Position = offset;
            } //for
        } //Read

        public void Write(FileStream stream)
        {
            List<string> strings = new List<string>(0);

            BinaryWriter writer = new BinaryWriter(stream);
            writer.WriteZeroes(0xC);
            headerEntry.offset = writer.BaseStream.Position;
            writer.Write(headerEntry.unkStrCode32);
            strings.Add(headerEntry.name);
            writer.WriteZeroes(0x10);
            dataOffset = (uint)writer.BaseStream.Position;

            int entryCount = entries.Count;

            for (int i = 0; i < entryCount; i++)
            {
                Entry entry = entries[i];

                entry.offset = writer.BaseStream.Position;
                writer.Write(entry.unkStrCode32);
                strings.Add(entry.name);
                writer.WriteZeroes(8);
                writer.Write(entry.unknown0);
                writer.Write(entry.unknown1);
                writer.WriteZeroes(0x8);
                writer.Write(entry.unknown2);
                writer.Write(entry.unknown3);
                writer.WriteZeroes(0xC);

                for (int j = 0; j < 4; j++)
                    writer.Write(entry.position[j]);

                for (int j = 0; j < 4; j++)
                    writer.Write(entry.rotation[j]);

                for (int j = 0; j < 4; j++)
                    writer.Write(entry.scale[j]);
            } //for

            for (int i = 0; i < entryCount; i++)
            {
                Entry entry = entries[i];
                ParentInfo parentInfo = entry.parentInfo;

                entry.parentSectionOffset = (ulong)(writer.BaseStream.Position - entry.offset);
                writer.Write(parentInfo.unknown0);
                parentInfo.offset0 = writer.BaseStream.Position;
                writer.Write(parentInfo.unkStrCode32_0);
                writer.WriteZeroes(4);
                parentInfo.offset1 = writer.BaseStream.Position;
                writer.Write(parentInfo.unkStrCode32_1);
                writer.WriteZeroes(4);
            } //for

            List<Tuple<string, long>> stringList = new List<Tuple<string, long>>(0);

            stringList.Add(new Tuple<string, long>(headerEntry.name, writer.BaseStream.Position));
            headerEntry.nameOffset = (ulong)(writer.BaseStream.Position - headerEntry.offset);
            writer.Write(headerEntry.name.ToCharArray());
            writer.WriteZeroes(1);

            for(int i = 0; i < entryCount; i++)
            {
                Entry entry = entries[i];
                ParentInfo parentInfo = entry.parentInfo;

                if (!stringList.Contains(stringList.Find(x => x.Item1 == entry.name)))
                {
                    stringList.Add(new Tuple<string, long>(entry.name, writer.BaseStream.Position));
                    entry.nameOffset = (ulong)(writer.BaseStream.Position - entry.offset);
                    writer.Write(entry.name.ToCharArray());
                    writer.WriteZeroes(1);
                } //if
                else
                {
                    Tuple<string, long> tuple = stringList.Find(x => x.Item1 == entry.name);
                    entry.nameOffset = (ulong)(tuple.Item2 - entry.offset);
                } //else

                if (!stringList.Contains(stringList.Find(x => x.Item1 == parentInfo.parentString)))
                {
                    stringList.Add(new Tuple<string, long>(parentInfo.parentString, writer.BaseStream.Position));
                    parentInfo.nameOffset0 = (uint)(writer.BaseStream.Position - parentInfo.offset0);
                    writer.Write(parentInfo.parentString.ToCharArray());
                    writer.WriteZeroes(1);
                } //if
                else
                {
                    Tuple<string, long> tuple = stringList.Find(x => x.Item1 == parentInfo.parentString);
                    parentInfo.nameOffset0 = (uint)(tuple.Item2 - parentInfo.offset0);
                } //else

                if (!stringList.Contains(stringList.Find(x => x.Item1 == parentInfo.parentName)))
                {
                    stringList.Add(new Tuple<string, long>(parentInfo.parentName, writer.BaseStream.Position));
                    parentInfo.nameOffset1 = (uint)(writer.BaseStream.Position - parentInfo.offset1);
                    writer.Write(parentInfo.parentName.ToCharArray());
                    writer.WriteZeroes(1);
                } //if
                else
                {
                    Tuple<string, long> tuple = stringList.Find(x => x.Item1 == parentInfo.parentName);
                    parentInfo.nameOffset1 = (uint)(tuple.Item2 - parentInfo.offset1);
                } //else
            } //for

            fileLength = (uint)writer.BaseStream.Position;

            //Offset writing time!
            writer.BaseStream.Position = 4;
            writer.Write(dataOffset);
            writer.Write(fileLength);

            writer.BaseStream.Position = headerEntry.offset + 4;
            writer.Write(headerEntry.nameOffset);

            for(int i = 0; i < entryCount; i++)
            {
                Entry entry = entries[i];
                ParentInfo parentInfo = entry.parentInfo;

                writer.BaseStream.Position = entry.offset + 4;
                writer.Write(entry.nameOffset);
                writer.BaseStream.Position += 0x18;
                writer.Write(entry.parentSectionOffset);

                writer.BaseStream.Position = parentInfo.offset0 + 4;
                writer.Write(parentInfo.nameOffset0);
                writer.BaseStream.Position = parentInfo.offset1 + 4;
                writer.Write(parentInfo.nameOffset1);
            } //for
        } //write

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
