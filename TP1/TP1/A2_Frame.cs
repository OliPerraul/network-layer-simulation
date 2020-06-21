using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TP1
{
    public enum FrameType : byte
    {
        None = 0,
        Data,
        EndOfTransmission,
        NAK,
        ACK,
        ACK_EndOfTransmission,
    }

    public struct Frame
    {
        public const int Size = 64;

        public static int HeaderSize =>
            sizeof(int) +
            sizeof(FrameType) +
            sizeof(int);

        public static int ContentSize => Size - HeaderSize;

        public string String => Encoding.UTF8.GetString(Content);

        public int SequenceID;
        public FrameType Type;
        public int OccupiedSize;
        public byte[] Content;

        public byte[] ToBytes()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(SequenceID);
            writer.Write((byte)Type);
            writer.Write(OccupiedSize);
            writer.Write(Content == null ? new byte[0] : Content);

            writer.Flush();
            return stream.ToArray();
        }

        public static Frame FromBytes(byte[] bytes)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));

            var s = default(Frame);
            s.SequenceID = reader.ReadInt32();
            s.Type = (FrameType)reader.ReadByte();
            s.OccupiedSize = reader.ReadInt32();
            s.Content = reader.ReadBytes(ContentSize);

            return s;
        }
    }
}
