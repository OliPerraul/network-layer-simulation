using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TP1
{
    public struct UserData
    {
        public const int Size = 16;
        public static int HeaderSize => sizeof(byte) + sizeof(byte);
        public static int ContentSize => Size - HeaderSize;

        public string String => Encoding.UTF8.GetString(Content);

        private byte isEOF;
        public bool IsEOF {
            get => Convert.ToBoolean(isEOF);
            set => isEOF = Convert.ToByte(value);
        }

        public byte OccupiedSize;
        public byte[] Content;

        public byte[] ToBytes()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(isEOF);
            writer.Write(OccupiedSize);
            writer.Write(Content);

            return stream.ToArray();
        }

        public static UserData FromBytes(byte[] bytes)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));

            var s = default(UserData);

            s.isEOF = reader.ReadByte();
            s.OccupiedSize = reader.ReadByte();
            s.Content = reader.ReadBytes(Convert.ToInt32(ContentSize));         

            return s;
        }
    }

    // NOTE: no event needed
    public abstract class UserDataLayer : MachineLayer<object>
    {
        public override SimpleSyncedBuffer DownTransferBuffer { get; set; } = new SimpleSyncedBuffer(UserData.Size);

        protected FileStream _stream;

        protected BinaryWriter _writer;

        protected FrameDataManipulationLayer _a2;

        public virtual void Set(FrameDataManipulationLayer a2)
        {
            _a2 = a2;
        }

        public UserDataLayer( ) : base() { }
              
    }

    public class A3_UserDataLayer : UserDataLayer
    {
        public A3_UserDataLayer() :
            base()
        { }

        public override void Start()
        {
            if (!File.Exists(Parameters.FullSource))
            {
                Console.WriteLine("Error: File not found.");
                return;
            }

            _stream = File.Open(Parameters.FullSource, FileMode.Open);

            base.Start();
        }

        public override void Update()
        {
            byte[] userDataBytes = new byte[UserData.Size];
            byte[] userDataContentBytes = new byte[UserData.ContentSize];
            long progress = 0;

            while (true)
            {
                // Send file by 16 byte formats
                // byte 0: is the number of meaningful byte
                // byte ^0: are content bytes                
                Array.Clear(userDataContentBytes, 0, userDataContentBytes.Length);
                int read;
                if ((read = _stream.Read(
                    userDataContentBytes,
                    0,
                    UserData.ContentSize)) > 0)
                {
                    progress += read;

                    Array.Clear(userDataBytes, 0, userDataBytes.Length);
                    userDataContentBytes.CopyTo(userDataBytes, UserData.HeaderSize);
                    userDataBytes[0] = Convert.ToByte(progress == _stream.Length);
                    userDataBytes[1] = Convert.ToByte(read);

                    DownTransferBuffer.TrySend(userDataBytes);
                    //Console.WriteLine("Thread 3 - Sending down : " + userDataBytes.ToBinaryString());
                }
                else
                {
                    Console.WriteLine("Finished Reading..");
                    break;
                }
            }

        }
    }


    public class B3_UserData : UserDataLayer
    {
        public B3_UserData() : base()
        {
        }

        public override void Start()
        {
            if (!File.Exists(Parameters.FullDestination))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Parameters.FullDestination));
            }
            else
            {
                File.Delete(Parameters.FullDestination);
            }

            _writer = new BinaryWriter(File.Create(Parameters.FullDestination));

            base.Start();

        }


        public override void Set(FrameDataManipulationLayer a2)
        {
            base.Set(a2);

        }

        public override void Update()
        {
            byte[] userDataBytes = new byte[UserData.Size];
            while (true)
            {
                Array.Clear(userDataBytes, 0, userDataBytes.Length);
                _a2.UpTransferBuffer.TryReceive(userDataBytes);
                var userData = UserData.FromBytes(userDataBytes);
                if(Parameters.Debug_B3_Written) Console.WriteLine("[B3, Written to file] bytes: " + userData.ToBytes().ToBitString());

                _writer.Write(userData.Content, 0, Convert.ToInt32(userData.OccupiedSize));
                _writer.Flush();

                if (userData.IsEOF)
                {
                    _writer.Close();
                    Console.WriteLine("Finished Writing..");
                    Console.WriteLine("Press Enter to terminate.");
                    Console.ReadLine();
                    Environment.Exit(1);

                    break;
                }
            }

        }
    }
}
