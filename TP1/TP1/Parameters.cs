using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TP1
{
    public static class Parameters
    {
        public static string Source { get; set; }

        public static string FullSource => Path.Combine(Utils.CurrentDirectory, Source);

        public static string Destination { get; set; }

        public static string FullDestination => Path.Combine(Utils.CurrentDirectory, Destination);

        public static int[] BitErrorPositions { get; set; }

        public static ErrorChangeType ErrorChangeType { get; set; }

        public static int[] FramesToChange { get; set; }

        public static int ThreadBufferSize { get; set; }

        public static int ThreadBufferDelay { get; set; }

        public static int WinSizeA { get; set; }

        // The size of the sending and receiving windows must be equal, and half the maximum sequence number (assuming that
        public static int MaxSequenceID => WinSizeA * 2;

        public static int WinSizeB { get; set; }

        public static int TimeOutA { get; set; }

        public static int TimeOutB { get; set; }

        public static bool IsHammingCorrecting { get; set; }

        public static char EmittingMachine { get; set; }

        public static bool Debug_A3_A2 { get; set; } = false;

        public static bool Debug_A2_A1 { get; set; } = false;

        public static bool Debug_A2_Retransmitted { get; set; } = false;

        public static bool Debug_A2_Timeout { get; set; } = false;

        public static bool Debug_A1_A2 { get; set; } = false;

        public static bool Debug_A1_C { get; set; } = false;

        public static bool Debug_C_A1 { get; set; } = false;

        public static bool Debug_C_Error { get; set; } = false;

        public static bool Debug_C_B1 { get; set; } = false;

        public static bool Debug_B1_C { get; set; } = false;

        public static bool Debug_B1_B2 { get; set; } = false;

        public static bool Debug_B1_Detected { get; set; } = false;

        public static bool Debug_B1_Corrected { get; set; } = false;

        public static bool Debug_B2_B1 { get; set; } = false;

        public static bool Debug_B2_B3 { get; set; } = false;

        public static bool Debug_B3_Written { get; set; } = false;

        public static bool TryDeserialize(
            string file)
        {
            StreamReader streamReader = new StreamReader(file);

            string line = "";
            if (!((line = streamReader.ReadLine()) != null))
                return false;

            Source = line;

            if (!((line = streamReader.ReadLine()) != null))
                return false;

            Destination = line;

            if (!((line = streamReader.ReadLine()) != null))
                return false;

            try
            {
                List<string> split = !line.Any(char.IsDigit) ? new List<string>() : line.Split(",").ToList();
                BitErrorPositions = split.Count == 0 ?
                    new int[0] :
                    split.Select(x => int.Parse(x)).ToArray();
            }
            catch (Exception e)
            {
                return false;
            }

            if (!((line = streamReader.ReadLine()) != null))
                return false;

            if (line == "A") ErrorChangeType = ErrorChangeType.A_AllFrames;
            else if (line == "B") ErrorChangeType = ErrorChangeType.B_RandomFrames;
            else if (line == "C") ErrorChangeType = ErrorChangeType.C_SpecifiedFrames;
            else if (line == "D") ErrorChangeType = ErrorChangeType.D_NoError;
            else return false;

            if (!((line = streamReader.ReadLine()) != null))
                return false;
            try
            {
                List<string> split = !line.Any(char.IsDigit) ? new List<string>() : line.Split(",").ToList();
                FramesToChange = split.Count == 0 ?
                    new int[0] :
                    split.Select(x => int.Parse(x)).ToArray();
            }
            catch (Exception e)
            {
                return false;
            }

            if (!((line = streamReader.ReadLine()) != null))
                return false;

            try
            {
                ThreadBufferSize = int.Parse(line);
            }
            catch
            {
                return false;
            }

            if (!((line = streamReader.ReadLine()) != null))
                return false;

            try
            {
                ThreadBufferDelay = int.Parse(line);
            }
            catch
            {
                return false;
            }


            if (!((line = streamReader.ReadLine()) != null))
                return false;

            try
            {
                WinSizeA = int.Parse(line);
            }
            catch
            {
                return false;
            }


            if (!((line = streamReader.ReadLine()) != null))
                return false;

            try
            {
                WinSizeB = int.Parse(line);
            }
            catch
            {
                return false;
            }

            if (!((line = streamReader.ReadLine()) != null))
                return false;

            try
            {
                TimeOutA = int.Parse(line);
            }
            catch
            {
                return false;
            }

            if (!((line = streamReader.ReadLine()) != null))
                return false;

            try
            {
                TimeOutB = int.Parse(line);
            }
            catch
            {
                return false;
            }

            if (!((line = streamReader.ReadLine()) != null))
                return false;

            if (line == "D") IsHammingCorrecting = false;
            else if (line == "R") IsHammingCorrecting = true;
            else return false;

            if (!((line = streamReader.ReadLine()) != null))
                return false;

            if (line == "A") EmittingMachine = 'A';
            else if (line == "B") EmittingMachine = 'B';
            else return false;
        
            // READ DEBUG LINES

            int res = 0;
            
            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;       
            Debug_A3_A2 = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_A2_A1 = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_A2_Retransmitted = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_A2_Timeout = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_A1_A2 = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_A1_C = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_C_A1 = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_C_Error = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_C_B1 = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_B1_C = res == 2;

            ////
            
            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_B1_B2 = res == 2;


            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_B1_Detected = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_B1_Corrected = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_B2_B1 = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_B2_B3 = res == 2;

            ////

            if ((res = ReadDebugLine(streamReader)) == 0)
                return true;
            Debug_B3_Written = res == 2;

            return true;
        }
        
        // If we read anything except "-" enabled next debug
        // 0: No more line
        // 1: Next Debug disabled
        // 2: Next Debug enabled
        public static int ReadDebugLine(StreamReader reader)
        {
            string line;
            if ((line = reader.ReadLine()) == null)
            {
                return 0;
            }
            else if (line == "-") return 1;
            else return 2;
        }
    }
}
