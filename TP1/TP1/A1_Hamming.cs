using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TP1
{
    public class Hamming
    {
        private static int CheckParity(byte[] encodedBytes, int parityBitSequenceIndex)
        {
            // get parity bit within that byte
            int parityBitValue = (1 << parityBitSequenceIndex);
            int parityBitIndex = parityBitValue - 1;
            int localParityBitIndex = parityBitIndex % 8;

            int parityByteIndex = parityBitIndex / 8;

            int parity = 0;
            // for all bit following parity use check skip method illustrated here
            // https://www.youtube.com/watch?v=XcqSU-wOIls
            // For all byte from parity byte
            // For all bit following parity bit
            for (
                int parityByteSuffixIndex = parityByteIndex;
                parityByteSuffixIndex < encodedBytes.Length;
                parityByteSuffixIndex++)
            {
                // For all bit following parity bit (starting at n for first byte, otherwise 0)
                int startIndex = parityByteSuffixIndex == parityByteIndex ?
                    localParityBitIndex :
                    0;

                for (
                int parityBitSuffixIndex = startIndex;
                parityBitSuffixIndex < 8;
                parityBitSuffixIndex++)
                {
                    int bitValue = ((parityByteSuffixIndex * 8) + parityBitSuffixIndex) + 1;
                    // Use check skip method
                    if ((bitValue & parityBitValue) != 0)
                    {
                        parity +=
                            ((encodedBytes[parityByteSuffixIndex] &
                            (1 << parityBitSuffixIndex))
                            != 0) ?
                                1 : 0;
                    }
                }
            }

            return parity % 2;
        }


        public static void Encode(byte[] messageBytes, byte[] encodedBytes)
        {
            // Assign message bits inbetween powers of two..
            for (
                // int i = 0;
                int encodedBitIndex = 0,
                messageBitIndex = 0;
                // i < size;
                encodedBitIndex < encodedBytes.Length * 8 &&
                (encodedBitIndex / 8) < encodedBytes.Length &&
                messageBitIndex < messageBytes.Length * 8 &&
                (messageBitIndex / 8) < encodedBytes.Length;
                // i++;
                encodedBitIndex++)
            {
                int encodedByteIndex = encodedBitIndex / 8;
                int localEncodedBitIndex = encodedBitIndex % 8;

                // Clear bit
                encodedBytes[encodedByteIndex] &= (byte)~(1 << localEncodedBitIndex);

                // start first byte at 1
                if (!Utils.IsPowerOfTwo(encodedBitIndex + 1))
                {
                    int messageByteIndex = messageBitIndex / 8;
                    int localMessageBitIndex = messageBitIndex % 8;
                    messageBitIndex++;

                    encodedBytes[encodedByteIndex] &= (byte)~(1 << localEncodedBitIndex);
                    encodedBytes[encodedByteIndex] |=
                        (byte)
                        (((messageBytes[messageByteIndex] & (1 << localMessageBitIndex)) != 0) ?
                        1 << localEncodedBitIndex :
                        0);
                }
            }

            //Set parity bits...
            // Now use the parity bits as a mask to check for parity with all bit position that are masked
            int parityBitSequenceLength = Utils.HighestPowerOf2(encodedBytes.Length * 8);
            for (
                int parityBitSequenceIndex = 0; 
                parityBitSequenceIndex < parityBitSequenceLength; 
                parityBitSequenceIndex++)
            {
                // get parity bit within that byte
                int parityBitValue = (1 << parityBitSequenceIndex);
                int parityBitIndex = parityBitValue - 1;
                int localParityBitIndex = parityBitIndex % 8;
                int parityByteIndex = parityBitIndex / 8;

                int parity = CheckParity(encodedBytes, parityBitSequenceIndex);

                // Assign correct parity bit
                encodedBytes[parityByteIndex] &= (byte)~(1 << localParityBitIndex);
                encodedBytes[parityByteIndex] |=
                    (byte)(parity != 0 ?
                    1 << localParityBitIndex :
                    0);
            }
        }

        public static void Decode(byte[] encodedBytes, byte[] messageBytes)//, byte[] cmp)
        {
            //int messageBitIndex = 0;
            // iterate over all bits for every byte
            for (
                // int i = 0;
                int encodedBitIndex = 0,
                messageBitIndex = 0;
                // i < size;
                encodedBitIndex < encodedBytes.Length * 8 &&
                (encodedBitIndex / 8) < encodedBytes.Length &&
                messageBitIndex < messageBytes.Length * 8 &&
                (messageBitIndex / 8) < encodedBytes.Length;
                // i++;
                encodedBitIndex++)
            {
                if (!Utils.IsPowerOfTwo(encodedBitIndex + 1))
                {
                    int messageByteIndex = messageBitIndex / 8;
                    int localMessageBitIndex = messageBitIndex % 8;
                    messageBitIndex++;

                    int encodedByteIndex = encodedBitIndex / 8;
                    int localEncodedBitIndex = encodedBitIndex % 8;

                    messageBytes[messageByteIndex] &= (byte)~(1 << localMessageBitIndex);
                    messageBytes[messageByteIndex] |=
                        (byte)
                        ((encodedBytes[encodedByteIndex] & (1 << localEncodedBitIndex)) != 0 ?
                        1 << localMessageBitIndex :
                        0);
                }
            }
        }

        // https://www.geeksforgeeks.org/hamming-code-in-computer-network/
        public static int DetectError(byte[] encodedBytes)
        {
            int error = 0;
            int parityBitSequenceLength = Utils.HighestPowerOf2(encodedBytes.Length * 8);

            // For each parity bit
            for (
                int parityBitSequenceIndex = 0; 
                parityBitSequenceIndex < parityBitSequenceLength; 
                parityBitSequenceIndex++)
            {
                int parity = CheckParity(encodedBytes, parityBitSequenceIndex);
                error |=
                    (byte)(parity != 0 ?
                    1 << parityBitSequenceIndex :
                    0);
            }

            return error;
        }
    }    
}
