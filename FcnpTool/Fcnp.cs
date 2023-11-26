using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace FcnpTool
{
    [XmlType]
    public class Fcnp
    {

        [XmlType]
        public class ConnectPoint
        {
            [XmlAttribute]
            public string CnpName;

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
                entry.CnpName = ExtensionMethods.ReadCString(reader);

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

            List<uint> Hashes_offsets = new List<uint>();
            List<string> Strings = new List<string>();

            writer.WriteGeoNameHash(Name);
            uint nameStringOffset_offset = (uint)writer.BaseStream.Position;
            writer.WriteZeroes(4);

            writer.WriteZeroes(12);


            for (int i = 0; i < Bones.Count; i++)
            {
                for (int j = 0; j < Bones[i].ConnectPoints.Count; j++)
                {
                    var entry = Bones[i].ConnectPoints[j];

                    if (!Strings.Contains(entry.CnpName))
                    {
                        Strings.Add(entry.CnpName);
                        Hashes_offsets.Add((uint)writer.BaseStream.Position + 4);
                    }

                    writer.WriteGeoNameHash(entry.CnpName);

                    writer.WriteZeroes(4);

                    writer.WriteZeroes(4);//flags

                    writer.Write(48);//dataoffset
                    writer.Write(48);//datasize

                    writer.WriteZeroes(8);

                    //previousnodeoffset
                    if (i > 0)
                        writer.Write(-96);
                    else
                        writer.Write(0);

                    //nextnodeoffset
                    if (i < Bones[i].ConnectPoints.Count - 1)
                        writer.Write(96);
                    else
                        writer.Write(0);

                    //parameterOffsets_offset[i] = (uint)writer.BaseStream.Position;
                    writer.WriteZeroes(4);

                    writer.WriteZeroes(8);

                    for (int k = 0; k < 4; k++)
                        writer.Write(entry.Translation[k]);
                    for (int k = 0; k < 4; k++)
                        writer.Write(entry.Rotation[k]);
                    for (int k = 0; k < 4; k++)
                        writer.Write(entry.Scale[k]);
                }
            }
            /*
             * 
            uint[] parameterOffsets_offset;
            uint[] parameterOffsets;

            //nodes
            for (int i = 0; i < ConnectPoints.Count; i++)
            {
                var entry = ConnectPoints[i];

                if (!Strings.Contains(entry.CnpName))
                {
                    Strings.Add(entry.CnpName);
                    Hashes_offsets.Add((uint)writer.BaseStream.Position+4);
                }

                writer.WriteGeoNameHash(entry.CnpName);

                writer.WriteZeroes(4);

                writer.WriteZeroes(4);//flags

                writer.Write(48);//dataoffset
                writer.Write(48);//datasize

                writer.WriteZeroes(8);

                //previousnodeoffset
                if (i > 0)
                    writer.Write(-96);
                else
                    writer.Write(0);

                //nextnodeoffset
                if (i < ConnectPoints.Count-1)
                    writer.Write(96);
                else
                    writer.Write(0);

                parameterOffsets_offset[i] = (uint)writer.BaseStream.Position;
                writer.WriteZeroes(4);

                writer.WriteZeroes(8);

                for (int j = 0; j < 4; j++)
                    writer.Write(entry.Translation[j]);
                for (int j = 0; j < 4; j++)
                    writer.Write(entry.Rotation[j]);
                for (int j = 0; j < 4; j++)
                    writer.Write(entry.Scale[j]);
            }
            */
        } //write
    } //class
} //namespace
