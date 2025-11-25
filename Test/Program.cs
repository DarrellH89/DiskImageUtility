using System;
using System.Diagnostics;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var dirSize = 0x2000;
            var sectorSize = 0x400;
            var dirStart = 0x2800;
            var spt = 5; // sectors per track
            var bufPtr = 0;
            var skewMap = BuildSkew(3, spt, 4); // interleave of 2 starting at sector 0

            Console.WriteLine("Dir Size {0:X4}", dirSize);
            Console.WriteLine("Sector Size {0:X4}", sectorSize);
            Console.WriteLine("Dir Start {0:X6}", dirStart);
            Console.WriteLine("Sectors Per Track {0}", spt);

            Console.WriteLine("Skew Map:");
            for (var i = 0; i < spt; i++) // loop through # sectors in directory
                                                           // Console.WriteLine("Logical Sector : Physical Sector");
            {
                Console.WriteLine("     {0:X2}", skewMap[i ]);
            }
            Console.WriteLine("Cnt    Cnt/spt  Sector Offset    Skew offset    BufPtr");
            for (var i = 0; i < dirSize / sectorSize; i++) // loop through # sectors in directory
            {
                var t = skewMap[i % spt] * sectorSize;
                var t1 = i / spt * sectorSize * spt;
                var t1a = i / spt * sectorSize;
                var t2 = dirStart;
                var t3 = t + t1 + t2;
                bufPtr = dirStart + i / spt * sectorSize * spt + skewMap[i % spt] * sectorSize;
                Console.WriteLine("{0:d3}      {1:X2} {2:X4}  {3:X5}               {4:X4}            {5:X4}", i, i/spt, t1a, t1, t, bufPtr);
            }
            Console.WriteLine("NCR Track Test\n");
            var tracks = 0;
            var numTrack = 80;
            for (var i = 0; i < 80; i++) // loop through track count, print NCR order
            {
                tracks = i;
                if (tracks < numTrack / 2)    // 
                    tracks = tracks * 2;
                else
                    tracks = (tracks - numTrack / 2) * 2 + 1;
                Console.WriteLine("{0:d3}      {1:d2} ", i, tracks);
            }
        }

        //******************** Build Skew *************************************
        // returns an integer array of size spt with the requested interleave intLv
        // array is in logical to physical format
        // logical sector is array index, value is physical order
        // start is starting array location from 0
        public static int[] BuildSkew(int intLv, int spt, int start)
        {
            var physicalS = start;
            var logicalS = 0;
            var count = new int[spt];
            var skew = new int[spt];
            var t = 0;
            // initialize table
            for (var i = 0; i < spt; i++) // initialize skew table
            {
                skew[i] = 32;
                count[i] = i;
            }

            while (logicalS < spt) // build physical to logical skew table
            {
                if (skew[physicalS] > spt) // logical position not yet filled
                {
                    skew[physicalS] = (byte)logicalS++;
                    physicalS += intLv;
                }
                else
                {
                    physicalS++; // bump to next physical position
                }

                if (physicalS >= spt) physicalS = physicalS - spt;
            }

            Array.Sort(skew, count); // sort both arrays using skew values and return count array for offset
            return count;
        }
    }
}
