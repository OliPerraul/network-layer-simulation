// Olivier Perrault
// 16212377

using System;
using System.IO;

namespace TP1
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Error: Wrong number of arguments..");
                return;
            }

            // Deserialize parameter file to be used by the threads
            if (!Parameters.TryDeserialize(Path.Combine(Directory.GetCurrentDirectory(), args[0])))
            {
                Console.WriteLine("Error: Bad parameter file..");
                return;
            }

            var c = new TransmissionSupportLayer();
            var a = new Machine(
                c,
                Parameters.EmittingMachine == 'A'
                );

            var b = new Machine(
                c,
                Parameters.EmittingMachine == 'B'
                );            

            a.Start();
            b.Start();
            c.Start();
        }
    }
}
