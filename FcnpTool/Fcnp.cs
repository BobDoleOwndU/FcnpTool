using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Xml.Serialization;
using static FcnpTool.Fcnp.StringOffsetStorageManager;

namespace FcnpTool
{
    [XmlType]
    public class Fcnp
    {
        [XmlType]
        public class ConnectPoint
        {
            [XmlAttribute]
            public string Name;

            [XmlElement]
            public Vector4 Translation;

            [XmlElement]
            public Vector4 Rotation;

            [XmlElement]
            public Vector4 Scale;
        } //struct
        [XmlAttribute]
        public string Name;
        [XmlType]
        public class Bone
        {
            [XmlAttribute]
            public string Name;

            [XmlArray]
            public List<ConnectPoint> ConnectPoints = new List<ConnectPoint>(0);
        }

        [XmlArray]
        public List<Bone> Bones = new List<Bone>(0);

        public void Read(FileStream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            long offsetToNode;

            reader.BaseStream.Position = 4;
            uint dataOffset = reader.ReadUInt32();

            reader.BaseStream.Position = 16;
            uint offsetToCnpNameString = reader.ReadUInt32();
            reader.BaseStream.Position = 12 + offsetToCnpNameString;
            Name = ExtensionMethods.ReadCString(reader);

            reader.BaseStream.Position = dataOffset;

            while (true)
            {
                ConnectPoint entry = new ConnectPoint();

                offsetToNode = reader.BaseStream.Position;
                reader.BaseStream.Position += 4;
                uint offsetToNameString = reader.ReadUInt32();
                reader.BaseStream.Position = offsetToNode + offsetToNameString;
                entry.Name = ExtensionMethods.ReadCString(reader);

                reader.BaseStream.Position = offsetToNode + 12;
                uint offsetToPayload = reader.ReadUInt32();
                reader.BaseStream.Position = offsetToNode + offsetToPayload;
                entry.Translation = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                entry.Rotation = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                entry.Scale = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                reader.BaseStream.Position = offsetToNode + 36;
                uint offsetToParameters = reader.ReadUInt32();
                reader.BaseStream.Position = offsetToNode + offsetToParameters + 16;
                uint offsetToParentBoneString = reader.ReadUInt32();
                reader.BaseStream.Position = offsetToNode + offsetToParameters + 12 + offsetToParentBoneString;
                string parentBoneName = ExtensionMethods.ReadCString(reader);

                Bone ParentBone = Bones.Find(x => x.Name.Equals(parentBoneName));
                if (ParentBone==null)
                {
                    ParentBone = new Bone
                    {
                        Name = parentBoneName
                    };
                    Bones.Add(ParentBone);
                }

                ParentBone.ConnectPoints.Add(entry);

                reader.BaseStream.Position = offsetToNode + 32;
                uint offsetToNextNode = reader.ReadUInt32();

                if (offsetToNextNode != 0)
                    reader.BaseStream.Position = offsetToNode + offsetToNextNode;
                else
                    break;
            } //while
        } //Read

        public void Write(FileStream stream)
        {
            
            BinaryWriter writer = new BinaryWriter(stream);

            //header
            writer.WriteZeroes(4);

            writer.Write(32);

            uint fileSize_offset = (uint)writer.BaseStream.Position;
            writer.WriteZeroes(4);

            StringOffsetStorageManager storageMan = new StringOffsetStorageManager();

            writer.WriteGeoNameHash(Name);
            AddString(storageMan, Name);
            AddOnWrite(writer, storageMan, Name);
            writer.WriteZeroes(4);

            writer.WriteZeroes(12);

            int cnpIndex = 0;
            int cnpCount = 0;
            for (int i = 0; i < Bones.Count; i++)
            {
                cnpCount += Bones[i].ConnectPoints.Count;
            }

            uint[] parameter_offsets = new uint[cnpCount];
            uint[] parameterOffsets_offset = new uint[cnpCount];

            for (int i = 0; i < Bones.Count; i++)
            {
                for (int j = 0; j < Bones[i].ConnectPoints.Count; j++)
                {
                    AddString(storageMan, Bones[i].ConnectPoints[j].Name);
                    AddString(storageMan, "Parent");
                    AddString(storageMan, Bones[i].Name);
                }
            }

            for (int i = 0; i < Bones.Count; i++)
            {
                for (int j = 0; j < Bones[i].ConnectPoints.Count; j++)
                {
                    var entry = Bones[i].ConnectPoints[j];

                    writer.WriteGeoNameHash(entry.Name);
                    AddOnWrite(writer, storageMan, entry.Name);
                    writer.WriteZeroes(4);

                    writer.WriteZeroes(4);//flags

                    writer.Write(48);//dataoffset
                    writer.Write(48);//datasize

                    writer.WriteZeroes(8);

                    //previousnodeoffset
                    if (cnpIndex>0)
                        writer.Write(-96);
                    else
                        writer.Write(0);

                    //nextnodeoffset
                    if (cnpIndex < cnpCount-1)
                        writer.Write(96);
                    else
                        writer.Write(0);

                    parameterOffsets_offset[cnpIndex] = (uint)writer.BaseStream.Position;
                    writer.WriteZeroes(4);

                    writer.WriteZeroes(8);

                    for (int k = 0; k < 4; k++)
                        writer.Write(entry.Translation[k]);
                    for (int k = 0; k < 4; k++)
                        writer.Write(entry.Rotation[k]);
                    for (int k = 0; k < 4; k++)
                        writer.Write(entry.Scale[k]);

                    cnpIndex++;
                }
            }

            int paramIdx = 0;
            for (int i = 0; i < Bones.Count; i++)
            {
                for (int j = 0; j < Bones[i].ConnectPoints.Count; j++) 
                {
                    parameter_offsets[paramIdx] = (uint)writer.BaseStream.Position;
                    writer.BaseStream.Position = parameterOffsets_offset[paramIdx];
                    writer.Write(parameter_offsets[paramIdx] - (parameterOffsets_offset[paramIdx] - 36));
                    writer.BaseStream.Position = parameter_offsets[paramIdx];

                    writer.Write((short)1);
                    writer.Write((short)0);

                    writer.WriteGeoNameHash("Parent");
                    AddOnWrite(writer, storageMan, "Parent");
                    writer.WriteZeroes(4);

                    writer.WriteGeoNameHash(Bones[i].Name);
                    AddOnWrite(writer, storageMan, Bones[i].Name);
                    writer.WriteZeroes(4);

                    paramIdx++;
                }
            }

            uint continueWriteOffset = (uint)writer.BaseStream.Position;
            for (int i = 0; i < storageMan.Storages.Count; i++)
            {
                uint stringOffset = (uint)writer.BaseStream.Position;
                writer.WriteCString(storageMan.Storages[i].String); writer.WriteZeroes(1);
                continueWriteOffset = (uint)writer.BaseStream.Position;
                for (int j = 0; j < storageMan.Storages[i].OffsetsToWriteTo.Count; j++)
                {
                    writer.BaseStream.Position = storageMan.Storages[i].OffsetsToWriteTo[j];
                    writer.Write(stringOffset - storageMan.Storages[i].OffsetsToWriteTo[j] + 4);
                }
                writer.BaseStream.Position = continueWriteOffset;
            }

            uint fileSize = (uint)writer.BaseStream.Length;
            writer.BaseStream.Position = fileSize_offset;
            writer.Write(fileSize);
        } //write
        public class StringOffsetStorageManager
        {
            public class StringOffsetStorage
            {
                public string String;
                public ulong Offset;
                public List<uint> OffsetsToWriteTo = new List<uint>(0);
            }
            public List<StringOffsetStorage> Storages = new List<StringOffsetStorage>(0);
            public static void AddString(StringOffsetStorageManager storageMan, string name)
            {
                StringOffsetStorage storage = storageMan.Storages.Find(x => x.String.Equals(name));
                if (storage == null)
                {
                    storage = new StringOffsetStorage
                    {
                        String = name
                    };
                    storageMan.Storages.Add(storage);
                }
            }
            public static void AddOnWrite(BinaryWriter writer, StringOffsetStorageManager storageMan, string name)
            {
                storageMan.Storages.Find(x => x.String.Equals(name)).OffsetsToWriteTo.Add((uint)writer.BaseStream.Position);
            }
        }
    } //class
} //namespace
