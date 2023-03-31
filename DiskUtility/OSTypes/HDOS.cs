using DiskUtility;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;

namespace HDOS
{
    public class HDOSFile
    {
        // test bit 7 for the following filename chars
        private const byte FRead = 9; //  Read only
        private const byte FSys = 10; // 1 = true
        private const byte FChg = 11; // bit 7 means file changed

        private const byte FMask = 0x80; // bit mask
        //private const int BuffLen = 0x2000; // buffer size


        ///********** data values for reading disks       */
        private const int bufferSize = 80 * 2 * 16 * 256 + 1024; // Tracks * Head * SPT * Sector Size + extra

        public byte[] buf = new byte[bufferSize];

        public string addFilename = "";
        //private const int sectorMax = 18 * 160; // max number of tracks
        //private int[]
        //    diskMap = new int[sectorMax]; // an array of buffer pointers in buf[] for each sector on the disk starting with track 0, side 0

        //private int diskVol = 2; // size of ALB size in directory
        //private int albSize = 512; // size of an alloction block
        //private int dirSectStart = 15; // starting sector for disk directory counting from 0. Also first ALB.
        private string DiskImageActive = "";
        private long diskSize = 0;

        private int
            diskInfoStart = 9 * 256, // Disk image values in Track 0, sector 9
            diskVol = 0,
            diskDate = 0,
            diskDirectStart = 0,
            diskGrtStart = 0,
            diskSectorPerGroup = 0,
            diskVolType = 0, // 0 = Data Volume Only, 1 = System Volume, 2 = Volumne has no directory
            diskInitVer = 0, // 30 = HDOS 3, 20 = HDOS 2, 16 = HDOS 1.6
            diskRGTSector = 0, //
            diskSectorSize = 256,
            diskVolData = 0,
            diskVolFlags = 0, // Binary flags: 1 = 2 head, 2 = 96tpi, 4 = media fixed, 
            numTrack = 0,
            numHeads = 0,
            dirLen = 0x17,
            dirCnt = 22;

        public string diskLabel = "";

        public List<DirList> fileNameList = new List<DirList>();


        // 0.Disk type, 1.# Track, 2. Sectors per Track, 3. Groups, 4. SPG 5. Available Sectors, 6. Size, 7 Heads
        public int[,] DiskType =
        {
            // 0    1  2    3   4   5    6        7
            { 0x6b, 80, 16, 255, 10, 2550, 655360, 2 }, // 1 640k H37 96tpi DD DS
            { 0x00, 40, 16, 213, 6, 1278, 327680, 2 }, //  6 320k H37 48tpi DD DS
            { 0x60, 40, 10, 200, 2, 400, 102400, 1 }, //   7 100k H37 48tpi DD SS

        };


        //private string fname;
        //private byte[] fnameb;
        //private bool readOnly; // Read only file
        //private bool sys; // system file
        //private bool chg; // disk changed - not used
        //private uint fsize; // file size 



        public class DirList : IComparable<DirList>
        {
            public string fname; // filename plus extension in 8 + " " + 3 format
            public byte[] fnameB = new byte[11]; // byte array version of file name
            public int userArea;

            public int
                fsize, // file size in Kb
                fCluster = 0;

            public int
                fGroup = 0,
                lGroup = 0,
                lastGroupSector = 0;

            public string flags; // flags for system and R/O
            public string createDate;

            public string modDate;
            //public List<int> grtList ;    

            public DirList()
            {
                fname = "";
                fsize = 0;
                userArea = 0;
                //grtList = new List<int> { 0 };    // initialize to 0
                flags = createDate = modDate = "";
                fGroup = lGroup = lastGroupSector = 0;

            }

            public DirList(string tFname, int tFsize, string tFlags, int tuser, int fgroup, int lgroup,
                int lastgroupsector)
            {
                fname = tFname;
                fsize = tFsize;
                userArea = tuser;
                flags = tFlags;
                //grtList = new List<int>{0};
                createDate = modDate = "";
                fGroup = fgroup;
                lGroup = lgroup;
                lastGroupSector = lastgroupsector;

            }

            public DirList(string tFname, int tFsize, string tFlags, int tuser)
            {
                fname = tFname;
                fsize = tFsize;
                userArea = tuser;
                flags = tFlags;
                //grtList = new List<int>{0};
                createDate = modDate = "";
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


        public HDOSFile()
        {
            //fileNameList = new List<DirList>();
            var fname = "";
            var fnameb = new byte[11] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var readOnly = false;
            //sys = false;
            //chg = false;

        }


        // destructor
        ~HDOSFile()
        {
        }




        // ************** Insert File HDOS *********************
        /*
         * Directory entries are written sequentially
         * buf = destination file image buffer
         * fileBuff = buffer of file to add to image
         * len = input file length
         * filename = HDOS filename
         * ref byte[] fileBuff
         * Assumes ReadHdosDir already populated image parameters
         */
        public int InsertFileHdos(string filename)
        {
            var result = 1;
            long
                diskItemp,
                filei = 0; // file buffer index
            //byte extentNum = 0;
            //var dirList = new int[dirSize / 32];
            //var dirListi = 0;

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
            if (string.IsNullOrEmpty(filename8))
                return 0;

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
            //var allocationBlockMap = new int[numTrack * numHeads * spt / (albSize / sectorSize) + 1];
            //for (var i = 0; i < allocationBlockMap.Length; i++) allocationBlockMap[i] = 0;
            //var dirMap = new int[dirSize / 32];
            //for (var i = 0; i < dirMap.Length; i++) dirMap[i] = 0;
            //var dirCnt = 0;

            //for (var numSect = 0; numSect < dirSize / sectorSize; numSect++) // number of sectors in directory
            //{
            //    diski = diskItemp =
            //        dirStart + numSect / spt * sectorSize * spt +
            //        skewMap[numSect % spt] * sectorSize; // buffer offset for sector start
            //    var t0 = 0;
            //    while (diski < diskItemp + sectorSize) // process one sector
            //    {
            //        // t0 = buf[diski];
            //        if (buf[diski] < 15)            // check if user area is less than 15. greater indicates empty entry
            //        {
            //            var fn = filenameb.ToCharArray();   // check if file is in directory
            //            var fcPtr = 1;
            //            for (; fcPtr < 12; fcPtr++)
            //                if (buf[diski + fcPtr] != (byte)fn[fcPtr - 1])
            //                    break; // compare filename to filename in directory
            //            if (fcPtr == 12)
            //            {
            //                MessageBox.Show("File already in Directory. Skipping", "File Exists", MessageBoxButtons.OK);
            //                return 0;
            //            }

            //            var cnt = Math.Ceiling((double)buf[diski + 15] * 128 / albSize); // # of allocation blocks to get in directory record

            //            for (var i = 0; i < cnt; i++)       // build allocation block map
            //                if (albNumSize == 1)
            //                {
            //                    t0 = buf[diski + 16 + i];
            //                    allocationBlockMap[buf[diski + 16 + i]] = 1;
            //                }
            //                else
            //                {
            //                    t0 = buf[diski + 16 + i * 2] + buf[diski + 16 + i * 2 + 1] * 256;
            //                    allocationBlockMap[buf[diski + 16 + i * 2] + buf[diski + 16 + i * 2 + 1] * 256]
            //                        = 1;
            //                }
            //        }
            //        else
            //        {
            //            dirMap[dirCnt] = (int)diski;
            //            dirCnt++;
            //        }

            //        diski += 32;
            //    }
            //}

            //var sectorBlock = albSize / sectorSize; // sectors per AB
            //var trackSize = spt * sectorSize; // # bytes in a track
            //var trackCnt = (float)trackSize / (float)albSize; // # Allocation blocks in a track
            //if (trackCnt % 2 != 0) trackCnt = 2;
            //else trackCnt = 1; // number of tracks for skew calculation
            //var minBlock = trackSize * (int)trackCnt; // minimum disk size to deal with due to skewing

            //// copy data to correct place in disk buffer using skew information
            ////var basePtr = dirStart; // albNum * allocBlock + dirStart - (albNum * allocBlock) % minBlock;
            //var dirNext = 0;
            //var diskPtr = 0;
            //var bytesWritten = 0;           // debug
            //while (filei < len) // process file
            //{
            //    // find empty directory entry
            //    if (dirNext < dirCnt)
            //    {
            //        diski = dirMap[dirNext++];
            //    }
            //    else
            //    {
            //        // not enough room on disk, erase directory entries used so far

            //        result = 0;
            //        break;
            //    }

            //    // write up to 0x80 128 byte CP/M records
            //    dirList[dirListi++] = (int)diski; // list of disk entries to erase in case of failure
            //    buf[diski] = 0; // mark directory entry in use
            //    var fn = filenameb.ToCharArray();
            //    for (var i = 1; i < 12; i++) buf[diski + i] = (byte)fn[i - 1]; // copy file name to dir entry
            //    for (var i = 12; i < 32; i++)
            //        buf[diski + i] = 0; // zero out extent list and remaining bytes in directory entry

            //    // update extent number and records in this extent

            //    var albCnt = dirSize / albSize;
            //    var albDirCnt = 0;
            //    var sectorCPMCnt = 0;

            //    while (filei < len && albDirCnt < 16 && result > 0) // write up to 16 allocation blocks for this directory entry
            //    // check for end of data to write, ALB < 16, for failure (result == 0)
            //    {
            //        // look for available allocation block
            //        for (; albCnt < allocationBlockMap.Length; albCnt++)
            //            if (allocationBlockMap[albCnt] == 0)
            //            {
            //                allocationBlockMap[albCnt] = 1;
            //                break;
            //            }
            //        // didn't find one, so quit
            //        if (albCnt >= allocationBlockMap.Length)
            //        {
            //            result = 0;
            //            break;
            //        }
            //        // write # of sectors in allocation block
            //        for (var i = 0; i < sectorBlock; i++)
            //        {
            //            var sectOffset = albCnt * albSize / sectorSize + i;
            //            diskPtr = dirStart + sectOffset / spt * sectorSize * spt +
            //                      skewMap[sectOffset % spt] * sectorSize;

            //            for (var ctrIndex = 0; ctrIndex < sectorSize; ctrIndex++)
            //                if (filei < len)
            //                {
            //                    buf[diskPtr++] = filebuf[filei++];
            //                    sectorCPMCnt++;
            //                    bytesWritten++;
            //                }
            //                else
            //                    buf[diskPtr++] = 0x1a;

            //        }

            //        // update FCB in directory
            //        if (albNumSize == 1)
            //        {
            //            buf[diski + 16 + albDirCnt++] = (byte)albCnt;
            //        }
            //        else
            //        {
            //            buf[diski + 16 + albDirCnt++] = (byte)albCnt;
            //            buf[diski + 16 + albDirCnt++] = (byte)(albCnt / 256);
            //        }

            //        // Only write 8 ALB for a 400k disk
            //        if ((diskType == 0x63 || diskType == 0x67) && albDirCnt == 8)
            //            break;
            //    }

            //    if (diskType == 0x23 && albDirCnt > 8)
            //    {
            //        extentNum++; // Type 23 is for Z100 CP/M. Loads 32k in one extant
            //        buf[diski + 15] = (byte)Math.Ceiling((double)sectorCPMCnt / 256);
            //    }
            //    else
            //    {
            //        buf[diski + 15] = (byte)Math.Ceiling((double)sectorCPMCnt / 128);
            //    }
            //    buf[diski + 12] = extentNum++;  // who knew I was this smart. Didn't remember covering this important feature
            //    //var t2 = sectorCPMCnt / 128;
            //    //var t3 = Math.Ceiling((double)sectorCPMCnt / 128);
            //    //buf[diski + 15] = (byte)Math.Ceiling((double)sectorCPMCnt / 128);
            //}

            //if (result == 0)        // not enough directory entries or allocation blocks
            //{
            //    while (--dirListi >= 0)
            //        if (dirList[dirListi] > 0)
            //            buf[dirList[dirListi]] = 0xe5;
            //}
            return result;
        }

        //*********************ReadHdosDir(fileName, ref diskTotal)
        // Input: Disk File Name, Disk Total clusters
        // Output: Updates FileNameList with directory entries
        // Check if file already in memory. If not, then process
        // open file: fileName
        // get disk parameters
        // Read directory gathering file names and sizes
        // update fileDetail with directory read
        // update file count and total file size
        public void ReadHdosDir(string diskFileName, ref int diskTotal)
        {
            int result = 0, fileLen = 0;
            var encoding = new UTF8Encoding();
            int
                dirBufPtr = 0,
                startDirBufPtr = 0,
                startGroupBufPtr = 0,
                dirGroup = 0;
            int diskUsed = 0;
            var t1 = 0;
            int
                firstGroup = 0,
                nextGroup = 0,
                lastGroup = 0,
                lastGroupSector = 0;

            if (diskFileName != DiskImageActive) // check if data already in memory
            {
                var file = File.OpenRead(diskFileName); // read entire file into an array of byte
                var fileByte = new BinaryReader(file);
                fileLen = (int)file.Length;
                try
                {
                    if (fileByte.Read(buf, 0, bufferSize) != fileLen || fileLen < 256)
                    {
                        MessageBox.Show("File read error", "Error", MessageBoxButtons.OK);
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("File buffer too small", "Error", MessageBoxButtons.OK);
                    return;
                }

                DiskImageActive = diskFileName;

                diskTotal = fileLen / 256; // size in clusters
                switch (diskTotal) // HDOS limits clusters to fit cluster map in one 256 byte sector
                {
                    case 1280:
                        diskTotal = 1278;
                        break;
                    case 2560:
                        diskTotal = 2550;
                        break;
                    default:
                        break;
                }

                diskTotal -= 20; // reserved HDOS
                fileNameList.Clear();
                file.Close();
                file.Dispose();
            }
            else
            {
                return; // list is current do nothing
            }

            // Get Disk information
            diskVol = buf[diskInfoStart];
            diskDate = buf[diskInfoStart + 1] + buf[diskInfoStart + 2] * 256;
            diskDirectStart = (buf[diskInfoStart + 3] + buf[diskInfoStart + 4] * 256) * 256;
            diskGrtStart = (buf[diskInfoStart + 5] + buf[diskInfoStart + 6] * 256) * 256;
            diskSectorPerGroup = buf[diskInfoStart + 7];
            diskVolType =
                buf[diskInfoStart + 8]; // 0 = Data Volume Only, 1 = System Volume, 2 = Volume has no directory
            diskInitVer = buf[diskInfoStart + 9]; // 30 = HDOS 3, 20 = HDOS 2, 16 = HDOS 1.6
            diskRGTSector = (buf[diskInfoStart + 0x0a] + buf[diskInfoStart + 0x0b] * 256) * 256; //
            diskSize = (buf[diskInfoStart + 0x0c] + buf[diskInfoStart + 0x0d] * 256) * 256;
            diskVolData = buf[diskInfoStart + 0x0e] + buf[diskInfoStart + 0xf] * 256;
            diskVolFlags = buf[diskInfoStart + 0x10]; // Binary flags: 1 = 2 head, 2 = 96tpi, 4 = media fixed, 
            numTrack = ((diskVolData & 2) > 0) ? 80 : 40;
            numHeads = ((diskVolData & 1) > 0) ? 2 : 1;
            diskLabel = "";
            diskLabel = encoding.GetString(buf, diskInfoStart + 0x11, 60);

            var tempLabel = new byte[5];
            var t4 = (((diskInitVer & 0xf0) >> 4) + 0x30);
            tempLabel[0] = (byte)(((diskInitVer & 0xf0) >> 4) + 0x30);
            tempLabel[1] = 0x2e;
            tempLabel[2] = (byte)(((diskInitVer & 0x0f) + 0x30));
            //tempLabel[2] = 0;
            diskLabel = "HDOS " + encoding.GetString(tempLabel, 0, 3) + ": " + diskLabel;

            result = 1; // stub error checking while I figure out if any checks make sense
            startGroupBufPtr = startDirBufPtr = dirBufPtr = diskDirectStart; // start of cluster
            dirGroup = startGroupBufPtr / diskSectorPerGroup / 256; // start Group for DIRECT.SYS
            var fnameStr = "";
            while (dirBufPtr != 0) // Read each sector
            {
                t1 = buf[dirBufPtr];

                if (buf[dirBufPtr] < 0xfe) // valid directory entry
                {
                    // user area
                    var userArea = (int)buf[dirBufPtr + 0xf];
                    if (diskInitVer < 0x30) // HDOS 2.0 reserved
                        userArea = 0;
                    // file name
                    byte[] temp1 = new byte[11];
                    for (var j = 0; j < 11; j++)
                        if (buf[dirBufPtr + j] > 0)
                            temp1[j] = buf[dirBufPtr + j];
                        else
                            temp1[j] = 0x20;
                    fnameStr = encoding.GetString(temp1, 0, 11);

                    // flags
                    var flagStr = HdosFlag(buf[dirBufPtr + 0xe], (byte)diskInitVer);

                    // Calculate file size
                    var fileDirSize = diskSectorPerGroup;

                    firstGroup = buf[dirBufPtr + 0x10];
                    nextGroup = firstGroup;
                    lastGroup = buf[dirBufPtr + 0x11];
                    lastGroupSector = buf[dirBufPtr + 0x12];

                    var fileNext = buf[dirBufPtr + 0x10];
                    var cnt = 0; // safety counter
                    while ((fileNext != 0) && (cnt++ < 255))
                    {
                        fileNext = buf[diskGrtStart + fileNext];
                        if (fileNext != 0)
                            fileDirSize += diskSectorPerGroup;
                        else
                        {
                            fileDirSize += buf[dirBufPtr + 0x12] - diskSectorPerGroup;
                        }
                    }

                    // File Creation Date
                    var fDate = buf[dirBufPtr + 0x13] * 256 + buf[dirBufPtr + 0x14];

                    // load data to list
                    var temp = new DirList(fnameStr, fileDirSize, flagStr, userArea, firstGroup,
                        lastGroup, lastGroupSector); // temp storage
                    temp.fCluster = cnt * diskSectorPerGroup;
                    Array.Copy(buf, dirBufPtr, temp.fnameB, 0, 11); // copy byte filename
                    temp.createDate = HdosDateStr(fDate);
                    diskUsed += fileDirSize;
                    temp.fsize = fileDirSize;
                    fileNameList.Add(temp);
                }

                dirBufPtr += dirLen; // advance to next directory error

                if (dirBufPtr + dirLen - startDirBufPtr > 2 * 256) // points past cluster end
                {
                    dirBufPtr = startDirBufPtr + 2 * 256 - 2; // Get next sector pointer from Directory
                    dirBufPtr = (buf[dirBufPtr] + buf[dirBufPtr + 1] * 256) * 256;
                    // check RGT
                    var tt1 = dirBufPtr - startGroupBufPtr;
                    var tt2 = diskSectorPerGroup * 256;
                    if (dirBufPtr - startGroupBufPtr == diskSectorPerGroup * 256) // get next group from GRT
                    {
                        dirGroup = buf[diskGrtStart + dirGroup];
                        if (dirGroup != 0 && dirGroup * diskSectorPerGroup * 256 != dirBufPtr)
                            Debug.WriteLine("dirGroup {0,4:X}!= dirBufPtr {1, 4:X}", dirGroup, dirBufPtr);
                        startGroupBufPtr = dirBufPtr;
                        if (dirGroup == 0)
                            dirBufPtr = 0;
                    }

                    if (dirBufPtr != 0)
                        startDirBufPtr = dirBufPtr;
                }
            }

            fileNameList.Sort();
        }
        // ************** HdosDateStr  *********************
        // inputs: Date int, Bit format "MMMDDDDDYYYYYYYM"
        // output: flag string

        string HdosDateStr(int date)
        {
            var result = "";
            string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var month = ((date & 0x001) << 3) + ((date & 0xe000) >> 13) - 1;
            var t1 = (date & 0x001) << 3;
            var t2 = ((date & 0xe000) >> 13);
            if (month > 11 || month < 0)
                month = 11;
            var day = (date & 0x1f00) >> 8;
            var year = ((date & 0x0fe) >> 1) + 70;

            result = string.Format("{0:D}-{1}-{2:D}", day, months[month], year);

            return result;
        }


        // ************** HDOS Flag  *********************
        // inputs: flag, HDOS version
        // output: flag string
        string HdosFlag(byte flag, byte ver)
        {
            byte flagTest = 0;
            char[] flagStrCh = { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' };

            int j = 0x80;
            while (j > 1)
            {
                flagTest = (byte)(flag & j);
                if (flagTest > 0)
                    switch (flagTest)
                    {
                        case 0x80:
                            flagStrCh[0] = 'S';
                            break;
                        case 0x40:
                            flagStrCh[2] = 'L';
                            break;
                        case 0x20:
                            flagStrCh[4] = 'W';
                            flagStrCh[5] = 'P';
                            break;
                        case 0x10:
                            flagStrCh[7] = 'C';
                            break;
                        default: break;
                    }

                if (ver > 0x20) // HDOS 3
                    switch (flagTest)
                    {
                        case 0x08:
                            flagStrCh[9] = 'A';
                            break;
                        case 0x04:
                            flagStrCh[11] = 'B';
                            break;
                        case 0x02:
                            flagStrCh[13] = 'D';
                            flagStrCh[14] = 'L';
                            break;
                        case 0x01:
                            flagStrCh[16] = 'U';
                            break;
                        default: break;
                    }
                else // HDOS 2 or earlier
                    switch (flagTest)
                    {
                        case 0x08:
                            flagStrCh[9] = '1';
                            break;
                        case 0x04:
                            flagStrCh[11] = '2';
                            break;
                        case 0x02:
                            flagStrCh[13] = '3';
                            break;
                        case 0x01:
                            flagStrCh[15] = '4';
                            break;
                    }

                j = j / 2;
            }

            string flagStr = new string(flagStrCh);
            return flagStr;
        }

        // **************  Extract File HDOS *********************
        // inputs: flag, HDOS version
        // output: flag string
        public int ExtractFileHDOS(Form1.DiskFileEntry disk_file_entry)
        {
            var result = 1; // assume success
            var maxBuffSize = 0x2000; // largest allocation block size
            var diskTotal = 0;

            //var disk_image_file = disk_file_entry.DiskImageName;
            //if (disk_image_file != DiskImageImdActive) ReadCpmDir(disk_image_file, ref diskTotal);

            //var encoding = new UTF8Encoding();
            //var dir = string.Format("{0}_Files", disk_image_file); // create directory name and check if directory exists
            //if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            //fnameb = encoding.GetBytes(disk_file_entry.FileName);

            //// Create output File
            //var name = disk_file_entry.FileName.Substring(2, 8).Trim(' ');
            //var ext = disk_file_entry.FileName.Substring(10, 3).Trim(' ');
            //var file_name = string.Format("{0}\\{1}.{2}", dir, name, ext);

            //if (File.Exists(file_name))
            //    if (MessageBox.Show("File exists, Overwrite it?", "File Exists", MessageBoxButtons.YesNo) ==
            //        DialogResult.No)
            //    {
            //        result = 0;
            //        return result;
            //    }

            //var file_out = File.Create(file_name);
            //var bin_out = new BinaryWriter(file_out);

            //var skewMap = BuildSkew(intLv, spt, sptStart);

            //// Find filename in DIRList
            //var obj = fileNameList.FirstOrDefault(x => x.fname == disk_file_entry.FileName);
            //if (obj != null)
            //{
            //    var rBptr = 0; // read buffer ptr
            //    var wBptr = 0; // write buffer ptr
            //    var wBuff = new byte[obj.fsize + 256]; //   write buffer

            //    foreach (var f in obj.fcbList)
            //    {
            //        var fcbNum = f.fcbnum;      // number of 128 byte records to get
            //        for (var i = 0; i < 16; i++)
            //            if (f.fcb[i] > 0) // allocation block to get
            //            {

            //                for (var k = 0; k < albSize / sectorSize; k++) // read the sectors in the allocation block
            //                {
            //                    //GetALB(ref buff, 0, bin_file, f.fcb[i], dirStart, allocBlock, sectorSize, spt, skewMap);
            //                    var sectOffset = f.fcb[i] * albSize / sectorSize + k;
            //                    var tracks = (dirStart + sectOffset) / (spt * sectorSize);
            //                    var baseSect = (dirStart + sectOffset - tracks * (spt * sectorSize)) / sectorSize;
            //                    rBptr = dirStart + sectOffset / spt * sectorSize * spt + skewMap[sectOffset % spt] * sectorSize;
            //                    // + (sectOffset % spt) * sectorSize;
            //                    var j = 0;
            //                    for (; j < sectorSize / 128 && j < fcbNum; j++)
            //                        for (var l = 0; l < 128; l++)
            //                            wBuff[wBptr++] = buf[rBptr++];
            //                    fcbNum -= j;
            //                }
            //            }
            //    }

            //    bin_out.Write(wBuff, 0, wBptr);
            //}

            //bin_out.Close();
            //file_out.Close();

            return result;
        }

        //********************* View File HDOS *********************************
        // View files from IMG or H8D images

        public byte[] ViewFileHDOS(string fileName)
        {

            //if (DiskImageImdActive.Length > 0)
            //{
            //    var skewMap = BuildSkew(intLv, spt, sptStart);

            //    var obj = fileNameList.FirstOrDefault(x => x.fname == fileName);
            //    if (obj != null)
            //    {
            //        var rBptr = 0; // read buffer ptr
            //        var wBptr = 0; // write buffer ptr
            //        var buffSize = obj.fsize;
            //        var wBuff = new byte[buffSize]; //   write buffer + 256 removed

            //        foreach (var f in obj.fcbList)
            //        {
            //            var fcbNum = f.fcbnum;
            //            for (var i = 0; i < 16; i++)
            //                if (f.fcb[i] > 0) // allocation block to get
            //                    for (var k = 0;
            //                        k < albSize / sectorSize;
            //                        k++) // read the sectors in the allocation block
            //                    {
            //                        //GetALB(ref buff, 0, bin_file, f.fcb[i], dirStart, allocBlock, sectorSize, spt, skewMap);
            //                        var t0 = f.fcb[i];
            //                        var sectOffset = f.fcb[i] * albSize / sectorSize + k; // sector to get
            //                                                                              //var t1 = sectOffset % spt; // sector to get on the track
            //                                                                              //var t2 = sectOffset / spt; // # of tracks
            //                                                                              // it looks like you can take out the spt but the order does make a difference in the result
            //                        rBptr = dirStart + sectOffset / spt * sectorSize * spt +
            //                                skewMap[sectOffset % spt] * sectorSize;

            //                        var j = 0;
            //                        for (; j < sectorSize / 128 && j < fcbNum; j++)
            //                            for (var l = 0; l < 128; l++)
            //                                wBuff[wBptr++] = buf[rBptr++];
            //                        fcbNum -= j;
            //                    }
            //        }
            //        // look for 0x1A to indicate end of text file
            //        var chk = 0;
            //        for (; chk < buffSize; chk++)
            //        {
            //            if (wBuff[chk] == 0x1a)
            //                break;
            //        }
            //        if (chk == buffSize)
            //            return wBuff;
            //        else
            //        {
            //            var wBuff1 = new byte[chk];
            //            for (var i = 0; i < chk; i++)
            //                wBuff1[i] = wBuff[i];
            //            return wBuff1;
            //        }
            //    }

            //}
            var fileImage = new byte[1];
            fileImage[0] = 0;
            return fileImage;

        }

        public bool InitHdosDisk(int diskTotal, string diskImageName)
        {
            var result = true;
            var encoding = new UTF8Encoding(); //byte[] bytes = Encoding. ASCII. GetBytes(author);
            int
                directSector = 0,
                grtSector = 0,
                sectorsCluster = 0,
                volType = 0,
                init = 0x20,
                rgtSector = 0,
                diskSize = diskTotal / 256,
                volData = 1,
                volFlags = 0,
                heads = 1;
            string diskLabel = "Micronics Technology HDOS 2.0 2023";

            for (var i = 0; i < diskTotal; i++) // Format disk
            {
                buf[i++] = 0x47;
                buf[i] = 0x4C;
            }

            // Setup as HDOS 2.0
            byte[] diskMark = { 0xc3,0xa0,0x22,0x20,0,0,0,0x62,0x18,0,0,0,0,0,0,0,0,
                                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            for (var i = 0; i < diskMark.Length; i++)
                buf[i] = diskMark[i];

            // Set up Disk info
            var diskDate = 0b110010100010110; // 10 Jun 1989
            var flag = 0b11100010; // SLW___D_ 
            switch (diskTotal)
            {
                case 102400:
                    directSector = 0x82;
                    grtSector = 0x94;
                    sectorsCluster = 2;
                    rgtSector = 0xA;
                    heads = 0;              // 0 = 1 head, 1 = 2 heads
                    break;
                case 327680:
                    directSector = 0x1A4;
                    grtSector = 0x1BC;
                    sectorsCluster = 0x6;
                    rgtSector = 0xC;
                    heads = 1;
                    diskSize = 1278;
                    break;
                case 655360:
                    directSector = 0x348;
                    grtSector = 0x366;
                    sectorsCluster = 0xA;
                    rgtSector = 0x14;
                    heads = 1;
                    diskSize = 2550;
                    break;
                default: // invalid disk size
                    result = false;
                    return result;
                    break;
            }

            var bufPtr = 0x900;              // load disk information
            buf[bufPtr] = 1;
            LoadBufLsMs(bufPtr+1,diskDate);   // Init Date
            LoadBufLsMs(bufPtr+3,directSector);   // DIRECT.SYS
            LoadBufLsMs(bufPtr+5,grtSector);      // GRT.SYS
            buf[bufPtr + 7] = (byte)(sectorsCluster);
            buf[bufPtr + 8] = (byte)(volType);
            buf[bufPtr + 9] = (byte)(init);
            LoadBufLsMs(bufPtr+0x0a, rgtSector );  // RGT Sector
            LoadBufLsMs(bufPtr+0xc,diskSize);   // Disk Size
            buf[bufPtr + 0xe] = 0; // Physical Sector Size
            buf[bufPtr + 0xf] = 1; // LS, MS Byte
            buf[bufPtr + 0x10] = (byte)heads; // Binary flags: 1 = 2 head, 2 = 96tpi, 4 = media fixed
            bufPtr += 0x11;
            byte[] bytes = Encoding.ASCII.GetBytes(diskLabel);
            var j = 0;
            for (j = 0; j < bytes.Length; j++)
                buf[bufPtr+j] = bytes[j];
            for( ;j <60; j++)
                buf[bufPtr+j] = 0x20;
            buf[bufPtr+j] = 0;

            // Set up GRT.SYS
            //   disk is formatted to 'GL'
            bufPtr = grtSector*256;
            byte grtGroup = (byte)(grtSector / sectorsCluster ); // HDOS 3, 640k disk values: 57
            byte directGroup = (byte)(directSector / sectorsCluster ); // 54
            byte rgtGroup = (byte)(rgtSector / sectorsCluster); // 2
            buf[bufPtr + grtGroup] = 0; // each file is one group
            buf[bufPtr + directGroup] = 0;
            buf[bufPtr + directGroup+1] = 0;                //
            buf[bufPtr + rgtGroup] = 0;
            buf[bufPtr] = 5;

            for (j = 5; j < 256; j++)
                if (buf[bufPtr+j+1] !=0)
                    buf[bufPtr + j] = (byte)(j + 1);
                else
                {
                    buf[bufPtr + j] = (byte)(j + 2);
                    j++;
                }

            buf[bufPtr + 0xfe] = 0;
            buf[bufPtr + 0x0ff] = 0xff;

            //RGT.SYS
            bufPtr = rgtSector*256;
            buf[bufPtr] = 0;
            buf[bufPtr + 1] = 0;
            for (j = 2; j < diskTotal/256/sectorsCluster; j++)
                buf[bufPtr + j] = 0x01;
            buf[bufPtr + 0xff] = 0xff;

            // DIRECT.SYS

            //var dirEntryLen = 22;
             
            var dirSize = sectorsCluster;
            if (diskTotal < 104000)
                dirSize = 9;
            else
                dirSize = sectorsCluster / 2;
          
            bufPtr = directSector*256;
            for (var k = 0; k < dirSize; k++)
            {
                for( j = 0;j < dirCnt; j++)         // Write blank directory entry 
                {
                    buf[bufPtr + j * dirLen] = 0xff;
                    for (var i = 1; i < dirLen; i++)
                        buf[bufPtr+ j*dirLen + i] = 0;
                }

                var l = 0x1fa;
                buf[bufPtr + l++ ] = 0;
                buf[bufPtr + l++] = 0x17;
                LoadBufLsMs(bufPtr+l, bufPtr/256);
                l += 2;
                LoadBufLsMs(bufPtr+l, (bufPtr+512)/256);
                bufPtr += 512;
            }

            buf[bufPtr] = 0xff;
            LoadBufLsMs(bufPtr -2, 0);
            bufPtr = directSector * 256;
            LoadDirHdos("DIRECT  SYS", bufPtr , flag, directGroup, directGroup, sectorsCluster, diskDate);
            LoadDirHdos("GRT     SYS", bufPtr += dirLen, flag, grtGroup, grtGroup, 1, diskDate);
            LoadDirHdos("RGT     SYS", bufPtr += dirLen, flag, rgtGroup, rgtGroup, 1, diskDate);




            // Write image to disk
            FileStream fso = new FileStream(diskImageName, FileMode.OpenOrCreate, FileAccess.Write);
            BinaryWriter fileOutByte = new BinaryWriter(fso);
            try
            {
                fileOutByte.Write(buf, 0, diskTotal);
                fileOutByte.Close();
                fso.Dispose();
            }
            catch (Exception e)
            {
                result = false;
                Debug.WriteLine(e);
                // throw;
            }


            return result;
        }
        // ************************ Load Buf LS MS ***********************
        // Loads Buf at location bPtr in LS, MS Order
        //
        private void LoadBufLsMs(int bPtr, int val)
        {
             buf[bPtr ] = (byte)(val & 0xff); // LS, MS Byte
             buf[bPtr +1] = (byte)((val & 0xff00) >> 8); // Disk Size

        }
        // ************************ Load Dir HDOS ***********************
        // Loads Directory information at location bPtr
        //
        private void LoadDirHdos(string fName, int bPtr, int flag, int fCluster, int lCluster, int lSector, int cDate)
        {
            var j = 0;

            for (j = 0; j < 11; j++)
            {
                if (j < fName.Length)
                    if (fName[j] != ' ')
                        buf[bPtr + j] = (byte)fName[j];
                    else
                        buf[bPtr + j] = 0;
                else
                    buf[bPtr + j] = 0;

            }

            buf[bPtr + 0xb] = 0;
            buf[bPtr + 0xc] = 0;
            buf[bPtr + 0xd] = 3;
            buf[bPtr + 0xe] = (byte)flag;
            buf[bPtr + 0xf] = 0;
            buf[bPtr + 0x10] = (byte)fCluster;
            buf[bPtr + 0x11] = (byte)lCluster;
            buf[bPtr + 0x12] = (byte)lSector;
            buf[bPtr + 0x13] = (byte)((cDate & 0xff00) >> 8);
            buf[bPtr + 0x14] = (byte)(cDate & 0xff);
            buf[bPtr + 0x15] = 0;
            buf[bPtr + 0x16] = 0;
        }
    }
}
