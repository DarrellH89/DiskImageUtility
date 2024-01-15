using DiskUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;

namespace CPM
{
    public class CPMFile
    {
        // test bit 7 for the following filename chars
        private const byte FRead = 9; //  Read only
        private const byte FSys = 10; // 1 = true
        private const byte FChg = 11; // bit 7 means file changed
        private const byte FMask = 0x80; // bit mask
        //private const int BuffLen = 0x2000; // buffer size

        // Disk types
        public int H37disktype = 5; // location of H37 disk type

        /********** data values for reading .IMD disks       */
        private const int bufferSize = 160*512*18+ 1024;
        public byte[] buf = new byte[bufferSize];
        //public string addFilename = "";
        private const int sectorMax = 18 * 160; // max number of tracks

        private int[]
            diskMap = new int[sectorMax]; // an array of buffer pointers in buf[] for each sector on the disk starting with track 0, side 0

        private int albNumSize = 2; // size of ALB size in directory
        private int albSize = 512; // size of an alloction block
        private int dirSectStart = 15; // starting sector for disk directory counting from 0. Also first ALB.
        private string DiskImageImdActive = "";
        private long diskSize = 0;

        private int diskType = 0,
            //bufPtr = 0,
            dirStart = 0,
            dirSize = 0,
            intLv = 0,
            spt = 0,
            sectorSize = 0,
            numTrack = 0,
            numHeads =0,
            sptStart = 0;
    

        public List<DirList> fileNameList = new List<DirList>();

        /*
        Disk type: byte 5 in sector 0 on H-37 disks (starting from 0) to define disk parameters
        Allocation Block size: number of bytes in an the smallest block used by CP/M on the disk. must be a multiple of 128 (0x80)
                AB numbers start with 0. The directory starts in AB 0.
        Directory Stat: start of directory entries in bytes
        Allocation Block Number Size: number of bytes used in directory entry to reference an allocation block
        Dir Size: number of bytes used for the directory
         0040 =         DPEH17	EQU	01000000B
         0060 =         DPEH37	EQU	01100000B
         0008 =         DPE96T	EQU	00001000B
         0004 =         DPEED	EQU	00000100B
         0002 =         DPEDD	EQU	00000010B
         0001 =         DPE2S	EQU	00000001B

        */
        // 0.Disk type, 1.Allocation block size, 2.Directory start, 3.Allocation block byte size, 4.dir size, 5.interleave,
        // 6.Sectors per Track, 7.Sector Size, 8.# Tracks, 9. # heads, 10 Skew Start Sector (0 based)
        public int[,] DiskType =
        {
            // 0    1       2     3     4    5  6   7       8  9 10
            {0xff, 0x800, 0x4800, 2, 0x2000, 6, 18, 0x200, 80, 2, 10}, // 0 1.44 MB Small Z-80 
            {0x00, 0x800, 0x4800, 2, 0x1000, 4, 9, 0x200, 80, 2, 0}, //   1 720k RC2014 80tpi DD DS
            {0x6f, 0x800, 0x2800, 2, 0x2000, 3, 5, 0x400, 80, 2, 4}, //   2 800k H37 96tpi ED DS
            {0x6b, 0x800, 0x2000, 2, 0x2000, 3, 16, 0x100, 80, 2, 12}, // 3 640k H37 96tpi DD DS
            {0x67, 0x800, 0x2800, 1, 0x2000, 3, 5, 0x400, 40, 2, 4}, //   4 400k H37 48tpi ED DS
            {0x23, 0x800, 0x2000, 1, 0x2000, 1, 8, 0x200, 40, 2, 0}, //   5 320k Z100 48tpi DD DS
            {0x62, 0x400, 0x2000, 1, 0x1000, 3, 16, 0x100, 40, 1, 12}, // 6 160k H37 48tpi DD SS 2=1000
            {0x63, 0x800, 0x2000, 1, 0x2000, 3, 16, 0x100, 40, 2, 4}, //   7 320k H37 48tpi DD DS
            {0x60, 0x400, 0x1e00, 1, 0x800, 3, 10, 0x100, 40, 1, 0}, //   8 100k H37 48tpi DD SS
            {0xE5, 0x400, 0x1e00, 1, 0x800, 4, 10, 0x100, 40, 1, 0}, //   9 100k Default H17 48tpi SD SS
            {0x00, 0x400, 0x1e00, 1, 0x800, 4, 10, 0x100, 40, 1, 0}, //   10 100k Default H17 48tpi SD SS

        };


        /*
        H37 disk identification at byte 6 on the first sector
        MSB = 6 for H37
        LSB
        Bit 4 1 = 48tpi in 96tpi drive
        Bit 3 0 = 48 tpi drive, 1 = 96 tpi
        Bit 2 1 = extended double density, used in conjunction with bit 1 (0110B)
        Bit 1 1 = double density, 0 = single density
        Bit 0 1 = double sided, 0 = single sided
        */
        private string fname;
        private byte[] fnameb;
        private bool readOnly; // Read only file
        private bool sys; // system file
        private bool chg; // disk changed - not used
        private uint fsize; // file size 
        private List<FCBlist> FCBfirst;

        public class DirList : IComparable<DirList>
        {
            public string fname; // filename plus extension in 8 + " " + 3 format
            public byte[] fnameB = new byte[11]; // byte array version of file name
            public byte userArea;
            public int fsize; // file size in Kb
            public string flags; // flags for system and R/O
            public int fcbNumSize;
            public List<FCBlist> fcbList;

            public DirList()
            {
                fname = "";
                fsize = 0;
                fcbList = new List<FCBlist>();
                fcbNumSize = 1;
                userArea = 0;
            }

            public DirList(string tFname, int tFsize, string tFlags, byte tuser)
            {
                fname = tuser.ToString("00")+tFname;
                fsize = tFsize;
                userArea = tuser;
                flags = tFlags;
                fcbList = new List<FCBlist>();
                fcbNumSize = 1;
            }

            public int CompareTo(DirList other)
            {
                if (other == null) return 1;
                return string.Compare(fname, other.fname);
            }

            public bool Equals(DirList other)
            {
                if (other == null) return false;
                return fname.Equals(other.fname);
            }
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FCBlist : IComparable<FCBlist>
        {
            public int[] fcb { get; set; } // 16 file control block numbers

            public int fcbnum { get; set; } // number of 128 byte records in this extant
            public int extantnum { get; set; } // extant number

            public FCBlist()
            {
                fcb = new int[16];
                fcb[0] = 0;
                fcbnum = 0;
                extantnum = 0;
            }

            public int Compare(FCBlist x, FCBlist other)
            {
                if (other == null) return 1;
                if (x.extantnum > other.extantnum) return 1;
                else if (x.extantnum == other.extantnum)
                    return 0;
                else return -1;
            }

            public int CompareTo(FCBlist other)
            {
                if (other == null) return 1;
                if (extantnum > other.extantnum) return 1;
                else if (extantnum == other.extantnum)
                    return 0;
                else return -1;
            }
        }


        public CPMFile()
        {
            fname = "";
            fnameb = new byte[11] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            readOnly = false;
            sys = false;
            chg = false;
            FCBfirst = new List<FCBlist>();
        }


        // destructor
        ~CPMFile()
        {
        }

        //*************** Read IMD Directory
        public int ReadImdDir(string diskFileName, ref int diskTotal)
        {
            // Check if file already in memory. If not, then process
            // open file: fileName
            // check H37 file type in byte 6
            // get disk parameters
            // Read directory gathering file names and sizes
            // update fcbList with fcb list for each file
            // add file names listBox2.Items
            // update file count and total file size
            var sectorSizeList = new int[] { 128, 256, 512, 1024, 2048, 4096, 8192 }; // IMD values
            var result = 0;
            var encoding = new UTF8Encoding();
            var fileLen = 0;

            if (diskFileName != DiskImageImdActive) // check if data already in memory
            {
                var file = File.OpenRead(diskFileName); // read entire file into an array of byte
                var fileByte = new BinaryReader(file);
                fileLen = (int)file.Length;
                //byte[] buf = new byte[bufferSize];
                try
                {
                    if (fileByte.Read(buf, 0, bufferSize) != fileLen || fileLen < 256)
                    {
                        MessageBox.Show("IMD file read error", diskFileName, MessageBoxButtons.OK);
                        return result;
                    }
                }
                catch
                {
                    MessageBox.Show("File buffer too small", diskFileName, MessageBoxButtons.OK);
                    return result;
                }

                DiskImageImdActive = diskFileName;
                diskSize = fileLen;
                fileNameList.Clear();
            }
            else
            {
                return result; // list is current do nothing
            }

            var bufPtr = 0;
            while (buf[bufPtr] != 0x1a && bufPtr <4096) bufPtr++; // look for end of text comment in IMD file
            if (bufPtr < bufferSize && buf[bufPtr + 1] < 6) // process as IMD file
            {
                bufPtr += 4;
                spt = buf[bufPtr++]; // sectors per track
                if (spt == 0)
                {
                    DiskImageImdActive = "";  // data read fail
                    MessageBox.Show("Sector Per Track read as 0", diskFileName,
                        MessageBoxButtons.OK);
                    return result;
                }
                sectorSize = sectorSizeList[buf[bufPtr]];
                var skewMap = new int[spt];

                for (var i = 0; i < spt; i++) skewMap[i] = buf[++bufPtr]; // load skew map from IMD image
                bufPtr++; // point to first sector marker
                //firstSector = bufPtr;
                int ctr,
                    dirSizeD = 0,
                    sptD = 0,
                    sectorSizeD = 0;


                //
                // map sectors
                // bufPtr already points to first sector marker

                var sectorCnt = 0;
                while (sectorCnt < sectorMax && bufPtr < diskSize)
                {
                    // debug
                    int t1 = sectorCnt % spt;
                    int t2 = skewMap[sectorCnt % spt];
                    int t3 = (sectorCnt / spt) * spt + skewMap[sectorCnt % spt]-1;
                    diskMap[(sectorCnt / spt) * spt + skewMap[sectorCnt % spt] - 1] =
                        bufPtr; // bufPtr points to sector marker

                    int t4 = buf[bufPtr];
                    switch (buf[bufPtr])
                    {
                        case 1:
                        case 3:
                        case 5:
                        case 7:
                            bufPtr += sectorSize + 1;
                            break;
                        case 2:
                        case 4:
                        case 6:
                        case 8:
                            bufPtr += 2;
                            break;
                        case 0:
                            bufPtr++;
                            break;
                        default:
                            MessageBox.Show("IMD sector marker out of scope "+buf[bufPtr].ToString("X2")
                                +" at location "+ bufPtr.ToString("X8"), diskFileName,MessageBoxButtons.OK);
                            DiskImageImdActive = "";        // disk read failed, mark disk image inactive
                            return result;
                    }

                    if ((sectorCnt + 1) % spt == 0 && sectorCnt > 0)
                        bufPtr += 5 + spt; // skip track header and interleave info
                    sectorCnt++;
                }
                //
                if (sectorCnt == 18*160)
                    diskType = 0xff;
                else 
                    if (fileLen < 104000)
                        diskType = DiskType[DiskType.GetLength(0) - 2, 0];
                    else
                        diskType = (int) buf[diskMap[0] + 6];


                for (ctr = 0; ctr < DiskType.GetLength(0); ctr++) // search DiskType array for values
                    if (diskType == DiskType[ctr, 0])
                    {
                        albSize = DiskType[ctr, 1]; // ALB Size
                        albNumSize = DiskType[ctr, 3]; // size of ALB size in directory
                        dirStart = DiskType[ctr, 2]; // physical start of directory
                        dirSizeD = DiskType[ctr, 4]; // size of the directory
                        sptD = DiskType[ctr, 6]; // sectors per track  
                        sectorSizeD = DiskType[ctr, 7]; // sector size
                        numTrack = DiskType[ctr, 8];
                        numHeads = DiskType[ctr, 9];
                        diskSize = diskTotal = numTrack* numHeads * spt * sectorSize  / 1024;
                        dirSectStart = dirStart / sectorSize;
                        break;
                    }


                // error if no match found
                if (ctr == DiskType.GetLength(0))
                    MessageBox.Show("CP/M Disk Type not found in IMD File", diskFileName, MessageBoxButtons.OK);
                else
                    result = 1;


                if ((spt != sptD || sectorSize != sectorSizeD) && result == 1)
                {
                    MessageBox.Show("Sector/track or sector size mismatch", diskFileName, MessageBoxButtons.OK);
                    result = 0;
                }

                var t0 = 0;
                if (result == 1) // done error checking, read directory
                {
                    // Read Dir
                    var diskUsed = 0;
                    for (var i = 0; i < dirSizeD / sectorSize; i++)
                    {
                        bufPtr = diskMap[(int)(dirStart / sectorSize) + i];
                        if (buf[bufPtr++] % 2 > 0) // IMD sector marker is odd. data should contain sector size
                            for (var dirPtr = 0; dirPtr < sectorSize; dirPtr += 32)
                                if (buf[bufPtr + dirPtr] != 0xe5)
                                {
                                    var flagStr = "";
                                    if ((buf[bufPtr + dirPtr + 9] & 0x80) > 0) flagStr += "R/O";
                                    if ((buf[bufPtr + dirPtr + 10] & 0x80) > 0) flagStr += " S";
                                    if ((buf[bufPtr + dirPtr + 11] & 0x80) > 0) flagStr += " W";
                                    for (var k = 9; k < 12; k++)
                                        buf[bufPtr + dirPtr + k] &= 0x7f; // mask high bit for string conversion
                                    var UserArea = buf[bufPtr + dirPtr];
                                    var fnameStr = encoding.GetString(buf, bufPtr + dirPtr+1 , 11);
                                    var fileDirSize = 0; //buf[bufPtr + dirPtr + 15] * 128;
                                    var temp = new DirList(fnameStr, fileDirSize, flagStr, UserArea); // temp storage
                                    Array.Copy(buf, bufPtr + dirPtr + 1, temp.fnameB, 0, 11); // copy byte filename

                                    temp.fcbNumSize = albNumSize;
                                    /********************************* Question this code **************/
                                    var tempFcb = new FCBlist
                                    {
                                        extantnum = buf[bufPtr + dirPtr + 12],
                                        fcbnum = buf[bufPtr + dirPtr + 15]
                                    };
                                    /* if albNumSize = 1, then k goe from 16 to 31, else if albNumSize = 2, k goes from 16 to 23 ( reads 16 bytes versus 8 words)*/
                                    // albNumSize is 1 or 2
                                    for (var k = 16; k < 32 - (albNumSize - 1) * 8; k++)
                                    {
                                        tempFcb.fcb[k - 16] = (int) buf[bufPtr + dirPtr + 16 + (k - 16) * albNumSize];
                                        if (albNumSize > 1) // add in high order byte
                                            tempFcb.fcb[k - 16] +=
                                                (int)buf[bufPtr + dirPtr + 16 + (k - 16) * albNumSize+1] * 256;
                                        if (tempFcb.fcb[k - 16] > 0)
                                        {
                                            fileDirSize += albSize;
                                        }
                                    }
                                    diskUsed += fileDirSize;
                                    tempFcb.fcbnum = fileDirSize / 128;             // # of 128 byte records which on a Z100 CPM disk could more thn 80h, e.g. 100h
                                    temp.fcbList.Add(tempFcb);
                                    var obj = fileNameList.FirstOrDefault(x => x.fname == UserArea.ToString("00")+ fnameStr);
                                    if (obj != null) // directory entry exists
                                    {
                                        obj.fsize += fileDirSize; // update file size
                                        obj.fcbList.Add(tempFcb); // add file control block
                                    }
                                    else
                                    {
                                        temp.fsize = fileDirSize;
                                        fileNameList.Add(temp);
                                    }
                                }
                    }

                    fileNameList.Sort();
                    //debug
                    //foreach (var f in fileNameList)
                    //{
                    //    var testStr = f.fname + " ";
                    //    foreach (var t in f.fcbList)
                    //    {
                    //        for (var i = 0; i < 16/f.fcbNumSize; i++)
                    //            testStr = testStr + t.fcb[i].ToString() + " ";
                    //    }

                    //    Console.WriteLine(testStr);
                    //}
                }
            }

            if (result == 0) // clear instance data
            {
                diskSize = 0;
                DiskImageImdActive = "";
                fileNameList.Clear();
            }

            return result;
        }


        //***************** Extract File CP/M IMD
        // Call ReadImdDisk to make sure image is in memory
        // Check to make sure file is in DirList
        public int ExtractFileCPMImd(Form1.DiskFileEntry diskFileEntry)
        {
            var diskImage = diskFileEntry.DiskImageName;
            //var fileNameListtemp = new List<CPMFile.DirList>();
            //int diskUsed = 0, 
            var diskTotal = 0;
            var result = 0;


            ReadImdDir(diskImage, ref diskTotal);
            var obj = fileNameList.FirstOrDefault(x => x.fname == diskFileEntry.FileName);
            if (obj != null)
            {
                var encoding = new UTF8Encoding();
                var dir = string.Format("{0}_Files", diskImage); // create directory name and check if directory exists
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                fnameb = encoding.GetBytes(diskFileEntry.FileName);

                // Create output File
                var name = diskFileEntry.FileName.Substring(2, 8).Trim(' ');
                var ext = diskFileEntry.FileName.Substring(10, 3).Trim(' ');
                var file_name = string.Format("{0}\\{1}.{2}", dir, name, ext);

                if (File.Exists(file_name))
                    if (MessageBox.Show("File exists, Overwrite it?", file_name, MessageBoxButtons.YesNo) ==
                        DialogResult.No)
                    {
                        result = 0;
                        return result;
                    }


                var file_out = File.Create(file_name);
                var bin_out = new BinaryWriter(file_out);


                // Read file data from memory buffer
                var wBuff = new byte[obj.fsize * 1024 + 256]; // write buffer = file size plus a buffer
                var wBPtr = 0;
                var t0 = 0;
                foreach (var f in obj.fcbList)
                {
                    t0++;
                    var fcbNum = f.fcbnum; // number of 128 byte CP/M records in the FCB
                    for (var i = 0; i < 16 / albNumSize && fcbNum > 0; i++) // read each fcb block in record. may be 8 or 16 values
                        if (f.fcb[i] > 0) // only process valid allocation blocks
                        {
                            var sectPerAlb = albSize / sectorSize;
                            //var t2 = f.fcb[i] * sectPerAlb + dirSectStart; // debug
                            for (var albCnt = 0; albCnt < sectPerAlb; albCnt++) // number of sectors to read in this allocation block
                            {
                                var t3 = f.fcb[i] * sectPerAlb + dirSectStart + albCnt;
                                var bufPtr = diskMap[f.fcb[i] * sectPerAlb + dirSectStart + albCnt]; // location of sector in buf[]
                                var bufData = buf[bufPtr]; // get IMD sector marker. If odd, a sector worth of data follows

                                if (bufData % 2 > 0) // IMD sector marker. odd number equals sector worth of data
                                {
                                    bufPtr++; // point to first data byte
                                    var k = 0; // declared outside for loop to preserve value
                                    for (; k < sectorSize / 128 && k < fcbNum; k++) // read only one sector or the number of fcb records left
                                        for (var j = 0; j < 128; j++)
                                            wBuff[wBPtr++] = buf[bufPtr++];
                                    fcbNum -= k; // decrement fcbnum counter by number of records read
                                }
                                else
                                // IMD marker even, sector is compressed. next byte equals sector data
                                {
                                    bufPtr++;
                                    var k = 0;

                                    for (; k < sectorSize / 128 && k < fcbNum; k++)
                                        for (var j = 0; j < 128; j++)
                                            wBuff[wBPtr++] = buf[bufPtr];
                                    fcbNum -= k; // decrement fcbnum counter by number of records read
                                }
                            }
                        }
                }

                wBPtr--;
                bin_out.Write(wBuff, 0, wBPtr);
                bin_out.Close();
                file_out.Close();
                result = 1;
            }
            else
            {
                MessageBox.Show(diskFileEntry.FileName + " error. File not found in DirList", "Error",
                    MessageBoxButtons.OK);
            }

            return result;
        }


        // ************** Insert File CP/M *********************
        /*
         * Directory entries are written sequentially
         * buf = destination file image buffer
         * fileBuff = buffer of file to add to image
         * len = input file length
         * filename = CP/M filename
         * diskType = offset to DiskType Array
         * ref byte[] fileBuff
         * Assumes ReadCpmDir already populated image parameters
         */
        public int InsertFileCpm(string filename)
        {
            var result = 1;
            long diski = dirStart, // CP/M disk index
                diskItemp,
                filei = 0; // file buffer index
            byte  extentNum = 0;
            var dirList = new int[dirSize / 32];
            var dirListi = 0;

            // Read file to add to image
            var file = File.OpenRead(filename);
            var len = file.Length; // read entire file into buffer
            var filebuf = new byte[len];
            var bin_file = new BinaryReader(file);
            bin_file.Read(filebuf, 0, (int)len);
            bin_file.Close();
            file.Dispose();

            // write the file to the disk image
            var filename8 = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrEmpty(filename8)) return 0;
            var skewMap = BuildSkew(intLv, spt, sptStart);
            // build CP/M version of file name for directory
            var encoding = new ASCIIEncoding();
            filename8 = filename8.Substring(0, Math.Min(filename8.Length, 8));
            filename8 = filename8.PadRight(8, ' ');
            var ext3 = Path.GetExtension(filename);
            ext3 = string.IsNullOrEmpty(ext3) ? ext3 = "   " : ext3 = ext3.Substring(1, Math.Min(ext3.Length - 1, 3));
            ext3 = ext3.PadRight(3, ' ');
            var filenameb = string.Format(filename8 + ext3).ToUpper();

            // Build allocation Block map and directory map
            //var totalSectors = spt * totalTrack;
            //var tt0 = dirStart / allocBlock;
            var allocationBlockMap = new int[numTrack *numHeads* spt / (albSize / sectorSize) + 1];
            for (var i = 0; i < allocationBlockMap.Length; i++) allocationBlockMap[i] = 0;
            var dirMap = new int[dirSize / 32];
            for (var i = 0; i < dirMap.Length; i++) dirMap[i] = 0;
            var dirCnt = 0;

            for (var numSect = 0; numSect < dirSize / sectorSize; numSect++) // number of sectors in directory
            {
                diski = diskItemp =
                    dirStart + numSect / spt * sectorSize * spt +
                    skewMap[numSect % spt] * sectorSize; // buffer offset for sector start
                var t0 = 0;
                while (diski < diskItemp + sectorSize) // process one sector
                {
                    // t0 = buf[diski];
                    if (buf[diski] < 15)            // check if user area is less than 15. greater indicates empty entry
                    {
                        var fn = filenameb.ToCharArray();   // check if file is in directory
                        var fcPtr = 1;
                        for (; fcPtr < 12; fcPtr++)
                            if (buf[diski + fcPtr] != (byte)fn[fcPtr - 1])
                                break; // compare filename to filename in directory
                        if (fcPtr == 12)
                        {
                            MessageBox.Show("File already in Directory. Skipping", fn.ToString(), MessageBoxButtons.OK);
                            return 0;
                        }

                        var cnt = Math.Ceiling((double)buf[diski + 15] * 128 / albSize); // # of allocation blocks to get in directory record

                        for (var i = 0; i < cnt; i++)       // build allocation block map
                            if (albNumSize == 1)
                            {
                                t0 = buf[diski + 16 + i];
                                allocationBlockMap[buf[diski + 16 + i]] = 1;
                            }
                            else
                            {
                                t0 = buf[diski + 16 + i * 2] + buf[diski + 16 + i * 2 + 1] * 256;
                                allocationBlockMap[buf[diski + 16 + i * 2] + buf[diski + 16 + i * 2 + 1] * 256]
                                    = 1;
                            }
                    }
                    else
                    {
                        dirMap[dirCnt] = (int)diski;
                        dirCnt++;
                    }

                    diski += 32;
                }
            }

            var sectorBlock = albSize / sectorSize; // sectors per AB
            var trackSize = spt * sectorSize; // # bytes in a track
            var trackCnt = (float)trackSize / (float)albSize; // # Allocation blocks in a track
            if (trackCnt % 2 != 0) trackCnt = 2;
                else trackCnt = 1; // number of tracks for skew calculation
            var minBlock = trackSize * (int)trackCnt; // minimum disk size to deal with due to skewing

            // copy data to correct place in disk buffer using skew information
            //var basePtr = dirStart; // albNum * allocBlock + dirStart - (albNum * allocBlock) % minBlock;
            var dirNext = 0;
            var diskPtr = 0;
            var bytesWritten = 0;           // debug
            while (filei < len) // process file
            {
                // find empty directory entry
                if (dirNext < dirCnt)
                {
                    diski = dirMap[dirNext++];
                }
                else
                {
                    // not enough room on disk, erase directory entries used so far

                    result = 0;
                    break;
                }

                // write up to 0x80 128 byte CP/M records
                dirList[dirListi++] = (int)diski; // list of disk entries to erase in case of failure
                buf[diski] = 0; // mark directory entry in use
                var fn = filenameb.ToCharArray();
                for (var i = 1; i < 12; i++) buf[diski + i] = (byte)fn[i - 1]; // copy file name to dir entry
                for (var i = 12; i < 32; i++)
                    buf[diski + i] = 0; // zero out extent list and remaining bytes in directory entry

                // update extent number and records in this extent

                var albCnt = dirSize / albSize;
                var albDirCnt = 0;
                var sectorCPMCnt = 0;

                while (filei < len && albDirCnt < 16 && result > 0) // write up to 16 allocation blocks for this directory entry
                // check for end of data to write, ALB < 16, for failure (result == 0)
                {
                    // look for available allocation block
                    for (; albCnt < allocationBlockMap.Length; albCnt++)
                        if (allocationBlockMap[albCnt] == 0)
                        {
                            allocationBlockMap[albCnt] = 1;
                            break;
                        }
                    // didn't find one, so quit
                    if (albCnt >= allocationBlockMap.Length)
                    {
                        result = 0;
                        break;
                    }
                    // write # of sectors in allocation block
                    for (var i = 0; i < sectorBlock; i++)
                    {
                        var sectOffset = albCnt * albSize / sectorSize + i;
                        diskPtr = dirStart + sectOffset /spt * sectorSize * spt +
                                  skewMap[sectOffset % spt] * sectorSize;

                        for (var ctrIndex = 0; ctrIndex < sectorSize; ctrIndex++)
                            if (filei < len)
                            {
                                buf[diskPtr++] = filebuf[filei++];
                                sectorCPMCnt++;
                                bytesWritten++;
                            }
                            else
                                buf[diskPtr++] = 0x1a;

                    }

                    // update FCB in directory
                    if (albNumSize == 1)
                    {
                        buf[diski + 16 + albDirCnt++] = (byte)albCnt;
                    }
                    else
                    {
                        buf[diski + 16 + albDirCnt++] = (byte)albCnt;
                        buf[diski + 16 + albDirCnt++] = (byte)(albCnt / 256);
                    }

                    // Only write 8 ALB for a 400k disk
                    if ((diskType == 0x63 || diskType == 0x67) && albDirCnt == 8)
                        break;
                }

                if (diskType == 0x23 && albDirCnt > 8)
                {
                    extentNum++; // Type 23 is for Z100 CP/M. Loads 32k in one extant
                    buf[diski + 15] = (byte)Math.Ceiling((double)sectorCPMCnt / 256);
                }
                else
                {
                    buf[diski + 15] = (byte)Math.Ceiling((double)sectorCPMCnt / 128);
                }
                buf[diski + 12] = extentNum++;  // who knew I was this smart. Didn't remember covering this important feature
                //var t2 = sectorCPMCnt / 128;
                //var t3 = Math.Ceiling((double)sectorCPMCnt / 128);
                //buf[diski + 15] = (byte)Math.Ceiling((double)sectorCPMCnt / 128);
            }

            if (result == 0)        // not enough directory entries or allocation blocks
            {
                while (--dirListi >= 0)
                    if (dirList[dirListi] > 0)
                        buf[dirList[dirListi]] = 0xe5;
            }
            return result;
        }
        //*********************ReadCPMdDir(fileName, ref diskTotal)
        //

        public void ReadCpmDir(string diskFileName, ref int diskTotal)
        {
            // Check if file already in memory. If not, then process
            // open file: fileName
            // check H37 file type in byte 6
            // get disk parameters
            // Read directory gathering file names and sizes
            // update fcbList with fcb list for each file
            // update file count and total file size

            int result = 0, fileLen = 0;
            var encoding = new UTF8Encoding();
         
            if (diskFileName != DiskImageImdActive) // check if data already in memory
            {
                var file = File.OpenRead(diskFileName); // read entire file into an array of byte
                var fileByte = new BinaryReader(file);
                fileLen = (int)file.Length;
                try
                {
                    if (fileByte.Read(buf, 0, bufferSize) != fileLen||fileLen < 256)
                    {
                        MessageBox.Show("File read error",diskFileName, MessageBoxButtons.OK);
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("File buffer too small", diskFileName, MessageBoxButtons.OK);
                    return;
                }

                DiskImageImdActive = diskFileName;
                diskTotal = fileLen;
                fileNameList.Clear();
                file.Close();
                file.Dispose();
            }
            else
            {
                return; // list is current do nothing
            }
            int ctr,
                bufPtr = 0;
          
            ctr = DiskTypeCheck(ref buf, fileLen, diskFileName.ToUpper().EndsWith("IMG"));
            // if (diskType == DiskType[ctr, 0] || ctr == DiskType.GetLength(0) - 1)
            // H8D LLL disk check
            bool H8dLLL = false;
            if (ctr == DiskType.GetLength(0))
            {
                if (diskFileName.EndsWith(".H8D") && (fileLen == 204800 || fileLen == 409600))
                {   // Treat as LLL disk
                    ctr = DiskType.GetLength(0) - 1;
                    H8dLLL = true;
                }
            }
            if (ctr != DiskType.GetLength(0))
            {
                // if ctr equals last value, use as default
                diskType = DiskType[ctr, 0];        // disk type marker
                albSize = DiskType[ctr, 1]; // ALB Size
                albNumSize = DiskType[ctr, 3]; // size of ALB size in directory
                dirStart = DiskType[ctr, 2]; // physical start of directory
                dirSize = DiskType[ctr, 4]; // size of the directory
                intLv = DiskType[ctr, 5]; // interleave
                spt = DiskType[ctr, 6]; // sectors per track  
                sectorSize = DiskType[ctr, 7]; // sector size
                numTrack = DiskType[ctr, 8]; // number of tracks on the disk
                sptStart = DiskType[ctr, 10]; // start for skew map
                numHeads = DiskType[ctr, 9];
                diskSize = numTrack * numHeads * spt * sectorSize / 1024;
                dirSectStart = dirStart / sectorSize;
                if (diskFileName.EndsWith(".IMG"))     // Check if disk type is in first physical sector
                {
                    sptStart = 0;
                    intLv = 1;
                }
                if (diskFileName.EndsWith(".H37") && buf[5] == DiskType[ctr, 0])     // Check if disk type is in first physical sector
                    sptStart = 0;

            }
            else
            {
                MessageBox.Show("Could not determine CP/M Disk type", diskFileName, MessageBoxButtons.OK);
                return;
            }
            if (H8dLLL) // using default, check file size
            {
                if (fileLen == 204800)
                {
                    albSize = 0x800;
                    dirSize = albSize * 2; // larger directory for 400k H17 disks
                    diskSize = 204800; //160 * spt * sectorSize / 1024;
                }
                if (fileLen == 409600)
                {
                    albSize = 0x800;
                    dirSize = albSize * 2; // larger directory for 400k H17 disks
                    diskSize = 409600; // 160 * spt * sectorSize / 1024;
                }
            }

            // error if no match found
            if (ctr == DiskType.GetLength(0))
                MessageBox.Show("Error - CP/M Disk Type not found in File", diskFileName, MessageBoxButtons.OK);
            else
                result = 1;
            if (Form1.IsHDOSDisk(ref buf))          // don't handle HDOS disks currently
                result = 0;
            
            if (result == 1) // done error checking, read directory
            {
                // Read Dir .IMG files are in sector number order, not skewed
                var diskUsed = 0;
                var skewMap = BuildSkew(intLv, spt, sptStart);
                for (var i = 0; i < dirSize / sectorSize; i++) // loop through # sectors in directory
                {
                    var t = skewMap[i % spt] * 512;
                    var t1 = i / spt * sectorSize * spt;
                    var t2 = dirStart;
                    var t3 = t + t1 + t2;
                    bufPtr = dirStart + i / spt * sectorSize * spt + skewMap[i % spt] * sectorSize;
                    //bufPtr = dirStart + i  * sectorSize ;
                    for (var dirPtr = 0; dirPtr < sectorSize; dirPtr += 32) // loop through sector checking DIR entries
                        if (buf[bufPtr + dirPtr] != 0xe5)
                        {
                            // process flag data
                            var flagStr = "";
                            if ((buf[bufPtr + dirPtr + 9] & 0x80) > 0) flagStr += "R/O";
                            if ((buf[bufPtr + dirPtr + 10] & 0x80) > 0) flagStr += " S";
                            if ((buf[bufPtr + dirPtr + 11] & 0x80) > 0) flagStr += " W";
                            for (var k = 9; k < 12; k++)
                                buf[bufPtr + dirPtr + k] &= 0x7f; // mask high bit for string conversion

                            // get file name in both string and byte format
                            var UserArea = buf[bufPtr + dirPtr];
                            var fnameStr = encoding.GetString(buf, bufPtr + dirPtr +1, 11);
                            var fileDirSize = 0;
                            var temp = new DirList(fnameStr, fileDirSize, flagStr, UserArea); // temp storage
                            Array.Copy(buf, bufPtr + dirPtr + 1, temp.fnameB, 0, 11); // copy byte filename
  
                            temp.fcbNumSize = albNumSize;
                            var tempFcb = new FCBlist
                            {
                                extantnum = buf[bufPtr + dirPtr + 12],
                                fcbnum = buf[bufPtr + dirPtr + 15]
                            };
                            // albNumSize can be 1 or 2
                            for (var k = 16; k < 32 - (albNumSize - 1) * 8; k++)
                            {
                                tempFcb.fcb[k - 16] = (int)buf[bufPtr + dirPtr + 16 + (k - 16) * albNumSize];
                                if (albNumSize > 1) // add in high order byte
                                    tempFcb.fcb[k - 16] +=
                                        (int)buf[bufPtr + dirPtr + 16 + (k - 16) * albNumSize + 1] * 256;
                                if (tempFcb.fcb[k - 16] > 0)
                                {
                                    fileDirSize += albSize;
                                }

                            }
                            diskUsed += fileDirSize;
                            tempFcb.fcbnum = fileDirSize / 128;     // # of 128 byte records which on a Z100 CPM disk could more thn 80h, e.g. 100h
                            temp.fcbList.Add(tempFcb);
                            var obj = fileNameList.FirstOrDefault(x => x.fname == UserArea.ToString("00") + fnameStr);
                            if (obj != null) // directory entry exists
                            {
                                obj.fsize += fileDirSize; // update file size
                                obj.fcbList.Add(tempFcb); // add file control block
                            }
                            else
                            {
                                temp.fsize = fileDirSize;
                                fileNameList.Add(temp);
                            }
                        }

                    fileNameList.Sort();
                }
            }
        }

        // ************** Extract File CP/M  *********************
        // inputs: path and filename, disk entry structure
        // output: requested file
        public int ExtractFileCPM(Form1.DiskFileEntry disk_file_entry)
        {
            var result = 1; // assume success
            var maxBuffSize = 0x2000; // largest allocation block size
            var diskTotal = 0;

            var disk_image_file = disk_file_entry.DiskImageName;
            if (disk_image_file != DiskImageImdActive) ReadCpmDir(disk_image_file, ref diskTotal);

            var encoding = new UTF8Encoding();
            var dir = string.Format("{0}_Files",disk_image_file); // create directory name and check if directory exists
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            fnameb = encoding.GetBytes(disk_file_entry.FileName);

            // Create output File
            var name = disk_file_entry.FileName.Substring(2, 8).Trim(' ');
            var ext = disk_file_entry.FileName.Substring(10, 3).Trim(' ');
            var file_name = string.Format("{0}\\{1}.{2}", dir, name, ext);

            if (File.Exists(file_name))
                if (MessageBox.Show("File exists, Overwrite it?", name+'.'+ext, MessageBoxButtons.YesNo) ==
                    DialogResult.No)
                {
                    result = 0;
                    return result;
                }

            var file_out = File.Create(file_name);
            var bin_out = new BinaryWriter(file_out);

            var skewMap = BuildSkew(intLv, spt, sptStart);

            // Find filename in DIRList
            var obj = fileNameList.FirstOrDefault(x => x.fname == disk_file_entry.FileName);
            if (obj != null)
            {
                var rBptr = 0; // read buffer ptr
                var wBptr = 0; // write buffer ptr
                var wBuff = new byte[obj.fsize + 256]; //   write buffer
                var fileType = buf[rBptr];

                foreach (var f in obj.fcbList)
                {
                    var fcbNum = f.fcbnum;      // number of 128 byte records to get
                    for (var i = 0; i < 16; i++)
                        if (f.fcb[i] > 0) // allocation block to get
                        {

                            for (var k = 0; k < albSize / sectorSize; k++) // read the sectors in the allocation block
                            {
                                //GetALB(ref buff, 0, bin_file, f.fcb[i], dirStart, allocBlock, sectorSize, spt, skewMap);
                                var sectOffset = f.fcb[i] * albSize/sectorSize+k;
                                var tracks = (dirStart + sectOffset) / (spt * sectorSize);
                                var baseSect = (dirStart + sectOffset - tracks * (spt * sectorSize)) / sectorSize;
                                rBptr = dirStart + sectOffset / spt * sectorSize * spt + skewMap[sectOffset % spt] * sectorSize;
                                // + (sectOffset % spt) * sectorSize;
                                var j = 0;
                                for (; j < sectorSize / 128 && j < fcbNum; j++)
                                    for (var l = 0; l < 128; l++)
                                        wBuff[wBptr++] = buf[rBptr++];
                                fcbNum -= j;
                            }
                        }
                }
                if(fileType == 0xc3)        // get rid of EOF marker
                    while (buf[--wBptr] == 0x1a)
                        ;
                bin_out.Write(wBuff, 0, wBptr);
            }

            bin_out.Close();
            file_out.Close();

            return result;
        }

        //********************* View File CPM *********************************
        // View files from H37 or H8D images

        public byte[] ViewFileCPM(string fileName)
        {

            if (DiskImageImdActive.Length > 0)
            {
                var skewMap = BuildSkew(intLv, spt, sptStart);

                var obj = fileNameList.FirstOrDefault(x => x.fname == fileName);
                if (obj != null)
                {
                    var rBptr = 0; // read buffer ptr
                    var wBptr = 0; // write buffer ptr
                    var buffSize = obj.fsize;
                    var wBuff = new byte[buffSize]; //   write buffer + 256 removed

                    foreach (var f in obj.fcbList)
                    {
                        var fcbNum = f.fcbnum;
                        for (var i = 0; i < 16; i++)
                            if (f.fcb[i] > 0) // allocation block to get
                                for (var k = 0;
                                    k < albSize / sectorSize;
                                    k++) // read the sectors in the allocation block
                                {
                                    //GetALB(ref buff, 0, bin_file, f.fcb[i], dirStart, allocBlock, sectorSize, spt, skewMap);
                                    var t0 = f.fcb[i];
                                    var sectOffset = f.fcb[i] * albSize / sectorSize + k; // sector to get
                                                                                          //var t1 = sectOffset % spt; // sector to get on the track
                                                                                          //var t2 = sectOffset / spt; // # of tracks
                                    // it looks like you can take out the spt but the order does make a difference in the result
                                    rBptr = dirStart + sectOffset / spt * sectorSize * spt +
                                            skewMap[sectOffset % spt] * sectorSize;

                                    var j = 0;
                                    for (; j < sectorSize / 128 && j < fcbNum; j++)
                                        for (var l = 0; l < 128; l++)
                                            wBuff[wBptr++] = buf[rBptr++];
                                    fcbNum -= j;
                                }
                    }
                    // look for 0x1A to indicate end of text file
                    var chk = 0;
                    for (; chk < buffSize; chk++)
                    {
                        if (wBuff[chk] == 0x1a)
                            break;
                    }
                    if(chk == buffSize)
                        return wBuff;
                    else
                    {
                        var wBuff1 = new byte[chk];
                        for (var i = 0; i < chk; i++)
                            wBuff1[i] = wBuff[i];
                        return wBuff1;
                    }
                }

            }
            var fileImage = new byte[1];
            fileImage[0] = 0;
            return fileImage; 

        }
        //****************** Disk Type Check *****************
        // checks for CP/M disk type

        public int DiskTypeCheck(ref byte[] buffer, int filelen, bool isImg)
        {
            var ctr = 0;
      
            for (; ctr < DiskType.GetLength(0); ctr++) // search DiskType array for to match disk size with buffer size
            {
                var tsizebuff = buffer[5] ;
                    var tsize = DiskType[ctr, 0];
                    var diskLen = DiskType[ctr, 6] * DiskType[ctr, 7] * DiskType[ctr, 8] * DiskType[ctr, 9];
                    if ((filelen == diskLen)|| filelen == diskLen+32)       // allows for H37 format
                    {
                        if (buffer[5] == DiskType[ctr, 0])
                            break;
                        if (filelen == 1474560)     // assume small Z80 disk
                            break;
                        if (filelen == 737280)     // assume RC2014 disk
                            break;
                } 
               
                 
            }

            if (filelen < 102500) // Smallest H37 formats
            {
                // check if disk type value matches. If not, most likely an H8D disk
                if (ctr >= DiskType.GetLength(0)) 
                    ctr = DiskType.GetLength(0)-1;        // make sure array value is in bounds
                if (buffer[5] != DiskType[ctr, 0])
                    switch (buffer[5])
                    {                       // assign values for the last two table entries
                        case 0xe5:
                            ctr = DiskType.GetLength(0)-2;
                            break;

                        default:
                            ctr = DiskType.GetLength(0)-1; // no match
                            break;
                    }
                if(isImg)
                    ctr = DiskType.GetLength(0) - 2;
            }

            return ctr;
        }

//******************** Build Skew *************************************
// returns an integer array of size spt with the requested interleave intLv
// array is in logical to physical format
// logical sector is array index, value is physical order
// start is starting array location from 0
    public int[] BuildSkew(int intLv, int spt, int start)
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
        //
    //**************************** Get Spt Start ***********************
    // returns private value sptStart
        public int GetSptStart()
        {
        return sptStart;
        }
    }

}