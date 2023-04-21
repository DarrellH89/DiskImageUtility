using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DiskUtility;
using Microsoft.SqlServer.Server;

namespace MSDOS
{ 
class MsdosFile
    {
        // test location 0x0B for file attributes
        private const byte FRead = 0x01; //  Read only
        private const byte FSys = 0x04; // system
        private const byte FHidden = 0x04; // hidden file
        private const byte FLabel = 0x08; // disk label
        private const byte FSub = 0x10; // Subdirectory
        private const byte FArchive = 0x20; // Archive bit
        //private const int BuffLen = 0x2000; // buffer size

        // Disk types

        /********** data values for reading .IMD disks       */
        private const int bufferSize = 18 * 512 *160 + 2048;   // SPT * Sector size * # tracks + buffer
        public byte[] buf = new byte[bufferSize];
        //public string addFilename = "";
        private const int sectorMax = 18 * 160; // max number of sectors

        private int[]
            diskMap = new int[sectorMax];   // an array of buffer pointers in buf[] for each sector on the disk starting with track 0, side 0 - 
                                            //Used for IMD disks

        private int albNumSize = 2; // size of ALB size in directory
        private int albSize = 512; // size of an alloction block
        private int dirSectStart = 15; // starting sector for disk directory counting from 0. Also first ALB.
        private string DiskImageActive = "";
        private long diskSize = 0;

        private int diskType = 0,
            bufPtr = 0,
            dirStart = 0,
            dirSize = 0,
            intLv = 0,
            spt = 0,
            sectorSize = 0,
            fatPtr = 0,
            maxFat = 0,
            maxSect = 0,
            numTrack = 0,
            numHeads =0,
            fatSectors,
            sptStart = 0,
            dataStart;

        public List<DirList> fileNameList = new List<DirList>();

        //  Search used disk type in position 0 mostly, Looks for disk size of 320k first then disk ID in 0x15
        /// // FAT 12 disks: 1. boot sector, 2. FAT1 3. FAT2 FAT may be one or two sectors 4. Directory start = # of sectors to contain # Dir entries * 32
        // first data cluster is cluster 2
        // 0.Disk type, 1.Allocation block size (Cluster), 2.Directory start, 3.dir size, 4.interleave, 5.Sectors per Track, 6.Sector Size,
        // 7.# Tracks, 8. # heads, 9 FAT start, 10 # sectors in FAT, 11 Max FAT, 12 Total Sectors, 13 sectors per cluster, IMD Value
        public int[,] DiskType =
        {
            // 0    1       2     3       4   5  6      7   8   9    10   11    12   13 14
            {0xff, 0x400, 0x600,  0xE00,  1,  8, 0x200, 40, 2, 0x200, 1,  320, 0x280, 2, 4}, // MSDOS 40trk, 320k
            {0xfe, 0x400, 0x2000,  0xE00,  1,  8, 0x200, 40, 1, 0x200, 1,  320, 0x280, 2, 4}, // MSDOS 40trk, 160k
            {0xfe, 0x400, 0x1400, 0x1800, 1,  8, 0x400, 77, 2, 0x400, 2, 1232, 0x4d0, 1, 3},   // MSDOS 77 trk, 1232k, 8"            
            {0xfd, 0x400, 0xA00,  0xE00,  1,  9, 0x200, 40, 2, 0x200, 2,  360, 0x2D0, 2, 4}, // MSDOS 40trk, 360k
            {0xf9, 0x400, 0xA00,  0xE00,  1,  9, 0x200, 80, 2, 0x200, 2,  720, 0x5A0, 2, 5}, // MSDOS 80trk, 720k 3.5"
            {0xf0, 0x200, 0x2600, 0x1C00, 1, 18, 0x200, 80, 2, 0x200, 9, 2880, 0xb40, 1, 3},   // MSDOS 80 trk, 1474k, 3.5"   
  
        };


        private string fname;
        private byte[] fnameb;
        private bool readOnly; // Read only file
        private bool sys; // system file
        private bool chg; // disk changed - not used
        private uint fsize; // file size 
     

        public class DirList : IComparable<DirList>
        {
            public string fname; // filename plus extension in 8 + " " + 3 format
            public byte[] fnameB = new byte[11]; // byte array version of file name
            public int fsize; // file size in bytes
            public string flags; // flags for system and R/O
            public int firstFat;
            public int ftime;   // file time
            public int fdate;   // file date
            public bool isSubDir;       // sub directory flag
  

            public DirList()
            {
                fname = "";
                fsize = 0;
                flags = "";
                ftime = 0;
                fdate = 0;
                firstFat = 0;
                isSubDir = false;
            }

            public DirList(string tFname, int tFsize, string tFlags, int tftime, int tfdate, int tFirstFat, bool tsubDir)
            {
                fname = tFname;
                fsize = tFsize;
                flags = tFlags;
                ftime = tftime;
                fdate = tfdate;
                firstFat = tFirstFat;
                isSubDir = tsubDir;

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

/*
        public MsdosFile()
        {
            fname = "";
            fnameb = new byte[11] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            readOnly = false;
            sys = false;
            chg = false;
     
        }


        // destructor
        ~MsdosFile()
        {
        }
*/
     
        // ************** Insert File MS-DOS *********************
        /*
         * Directory entries are written sequentially
         * buf = destination file image buffer
         * fileBuff = buffer of file to add to image
         * len = input file length
         * filename = CP/M filename
         * diskType = offset to DiskType Array
         * ref byte[] fileBuff
         */
        public int InsertFileDos(string filename)
        {
            var result = 0;
            //int allocBlock = DiskType[diskType, 1],
            //    dirStart = DiskType[diskType, 2],
            //    albDirSize = DiskType[diskType, 3],
            //    dirSize = DiskType[diskType, 4],
            //    interleave = DiskType[diskType, 5],
            //    spt = DiskType[diskType, 6],
            //    sectorSize = DiskType[diskType, 7], // disk parameter values
            //    totalTrack = DiskType[diskType, 8]; 
            //int albNum = 2;       // default AB to store a file
            if (DiskImageActive.Length == 0)
            {
                MessageBox.Show("Programming Error. ReadMsdosDir() should have been called", "Program Error",
                    MessageBoxButtons.OK);
                return result;
            }

            long diskIndx = dirStart, // MSDOS disk index
                diskIndxTemp;
                //filei = 0; // file buffer index
            // string filename = addFilename;
            //if (filename.Length == 0||DiskImageImdActive.Length == 0)           // no filename to add
            //    return 0;

            // read entire file into buffer
            var file = File.OpenRead(filename);
            var len = file.Length; 
            var filebuf = new byte[len];
            var bin_file = new BinaryReader(file);
            bin_file.Read(filebuf, 0, (int)len);
            bin_file.Close();
            file.Dispose();

            // write the file to the disk image
            var filename8 = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrEmpty(filename8)) return 0;
           // var skewMap = BuildSkew(intLv, spt);
            // build 8.3 version of file name for directory
            var encoding = new ASCIIEncoding();
            filename8 = filename8.Substring(0, Math.Min(filename8.Length, 8));
            filename8 = filename8.PadRight(8, ' ');
            var ext3 = Path.GetExtension(filename);
            ext3 = string.IsNullOrEmpty(ext3) ? ext3 = "   " : ext3 = ext3.Substring(1, Math.Min(ext3.Length - 1, 3));
            ext3 = ext3.PadRight(3, ' ');
            var filenameb = string.Format(filename8 + ext3);
          
            var obj = fileNameList.FirstOrDefault(x => x.fname == filenameb);
            if (obj != null)
            {
                MessageBox.Show("File Exists. You must delete first", "File Exists Error",
                    MessageBoxButtons.OK);
                return result;
            }

            // Build FAT map and directory map
            var fatCnt = 0;
            var fatMap = new int[maxFat];
            var firstFat = 0;
            for (var i = 0; i < maxFat; i++) fatMap[i] = 0;
            var fileFatPtr = 0;
            while (fileFatPtr < maxFat)
            {
                var fileFatStart = fatPtr + (fileFatPtr * 3) / 2;
                var fatVal = buf[fileFatStart++] + buf[fileFatStart++] * 256;
                if (fileFatPtr % 2 == 0)  //even
                    fatVal = fatVal & 0xfff;
                else fatVal = fatVal >> 4;          // odd
                // fatVal contains cluster number to get
                if (fatVal > 0)
                {
                    fatMap[fileFatPtr] = fatVal; // disk area in use
                    fatCnt++;
                }
                else if (firstFat == 0)
                    firstFat = fileFatPtr;
                fileFatPtr++;
            }
            if((maxFat -fatCnt) * albSize < len)
            {
                MessageBox.Show("File too large for available disk space", "File Size Error",
                    MessageBoxButtons.OK);
                return result;
            }
            // Find available directory entry

            /*
            var dirMap = new int[dirSize / 16];         // make the directory table twice the number of entries to allow for subfolder
            for (var i = 0; i < dirMap.Length; i++) dirMap[i] = 0;
            var dirCnt = 0;
            */
            for (var numSect = 0; numSect < dirSize / sectorSize && result == 0; numSect++) // number of sectors in directory
            {
                diskIndx = diskIndxTemp = dirStart + numSect *sectorSize; // buffer offset for sector start
                while (diskIndx < diskIndxTemp + sectorSize) // process one sector
                {
                    if (buf[bufPtr + diskIndx] == 0xe5 || buf[bufPtr + diskIndx] == 0x00) // erased or available
                    {
                        result = 1;
                        break;
                    }
                    else diskIndx += 32;
 
                }
            }

            if (result > 0)
            {
                // add directory entry
                var fn = filenameb.ToUpper().ToCharArray();
                for (var i = 0; i < filenameb.Length; i++) buf[diskIndx + i] = (byte)fn[i];
                buf[diskIndx + 0xb] = 0;        // File Attributes
                buf[diskIndx + 0xc] = 0;        // not used in DOS 2.0
                buf[diskIndx + 0xd] = 0;        // not used in DOS 2.0
                var fTime = File.GetLastWriteTime(filename); 
                int tempTime = fTime.Hour<< 11 | fTime.Minute << 5 | fTime.Second;
                int tempDate = (fTime.Year-1980) << 9 | fTime.Month << 5 | fTime.Day;
                buf[diskIndx + 0xe] = (byte)(tempTime & 0xff);
                buf[diskIndx + 0xf] = (byte)(tempTime>>8 & 0xff);
                buf[diskIndx + 0x10] = (byte)(tempDate & 0xff);
                buf[diskIndx + 0x11] = (byte)(tempDate >> 8 & 0xff);
                for (var i = 0; i < 4; i++)
                    buf[diskIndx + 0x12 + i] = 0;
                for (var i = 0; i < 4; i++)                     // set last modified time/date to creation time/date
                    buf[diskIndx + 0x16 + i] = buf[diskIndx+0xe + i];
                buf[diskIndx + 0x1a ] = (byte)(firstFat&0xff);
                buf[diskIndx + 0x1b] = (byte)(firstFat >> 8 & 0xff);
                var tempLen = len;
                for (var i = 0; i < 4; i++)
                {
                    buf[diskIndx + 0x1c + i] = (byte)(tempLen & 0xff);
                    tempLen = (tempLen >> 8) & 0xffffff;
                }
                // write file data
                // fatMap[] lists open allocation blocks
                // firstFat = first available block
                // fatPtr = start of FAT
                // filebuf contains file to add, len is file length
 
                var rBptr = 0;
                var wBptr = 0;
                var currFat = firstFat; 
                var nextFat = firstFat+1;
                while (rBptr < len)
                {
                    wBptr = dirStart + dirSize +  (currFat-2) * albSize;
                    for (var k = 0; k < albSize && rBptr < len; k++)
                    {
                            buf[wBptr++] = filebuf[rBptr++];
                    }
                    if (rBptr < len)
                    {
                        while (fatMap[nextFat] != 0) 
                            nextFat++; // find next open FAT
                        fatMap[currFat] = nextFat;
                        currFat = nextFat++;
                    }
                }
                //while (fatMap[nextFat] != 0) nextFat++;         // find next open FAT
                fatMap[currFat] = 0xfff;                     // end of FAT chain marker
                // write FAT Table
                var clusterNum = 0;
                var wFatPtr = fatPtr;
                var fatSize = maxFat * 3 / 2;
                while( clusterNum < maxFat)            // 
                {
                    var temp = fatMap[clusterNum++] & 0xfff | fatMap[clusterNum++] << 12;
                    for (var i = 0; i < 3; i++)
                    {
                    buf[wFatPtr+i ] = (byte) (temp & 0xff);
                    buf[wFatPtr + i + fatSize] = buf[wFatPtr + i];
                    temp = temp >> 8;
                    }
                    wFatPtr += 3;
                    
                }

                for (var i = 0; i < maxFat * 3 / 2; i++)
                    buf[fatPtr + fatSectors * sectorSize+i] = buf[fatPtr + i];

            }
            return result;
        }
        //*********************Read MSDOS Dir ************************************/
        // Inut: Disk File name, Disk size
       //

        public void ReadMsdosDir(string diskFileName, ref int diskTotal)
        {
            // Check if file already in memory. If not, then process
            // open file: fileName
            // get disk parameters
            // Read directory gathering file names and sizes
            // update filename list with file information and first FAT
            // update file count and total file size

            int result = 0, fileLen = 0;
            var encoding = new UTF8Encoding();
            var subDir = false;

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
                diskTotal = fileLen;
                fileNameList.Clear();
                file.Close();
                file.Dispose();
            }
            else
            {
                return; // list is current do nothing
            }

            if(diskTotal == 327680 || diskTotal == 327712 ) diskType = 0xff;
            else if(diskTotal == 368640) diskType = (int)buf[0x15];
                    else diskType = (int)buf[0x15];
            int ctr,
                bufPtr = 0;

            for (ctr = 0; ctr < DiskType.GetLength(0); ctr++) // search DiskType array for values
                if (diskType == DiskType[ctr, 0] && (diskTotal == DiskType[ctr,12]* DiskType[ctr, 6] ||
                                                     diskTotal == (DiskType[ctr, 12] * DiskType[ctr, 6])+32))
                    //|| ctr == DiskType.GetLength(0) - 1)
                {
                    // if ctr equals last value, use as default
                    albSize = DiskType[ctr, 1]; // ALB Size
                    dirStart = DiskType[ctr, 2]; // physical start of directory
                    dirSize = DiskType[ctr, 3]; // size of the directory
                    intLv = DiskType[ctr, 4]; // interleave
                    spt = DiskType[ctr, 5]; // sectors per track  
                    sectorSize = DiskType[ctr, 6]; // sector size
                    numTrack = DiskType[ctr, 7]; // number of tracks on the disk
                    numHeads = DiskType[ctr, 8];  // number of heads
                    maxFat = DiskType[ctr,11];      // max number of FAT entries
                    maxSect = DiskType[ctr, 12];     // max number of sectors
                    diskSize = numTrack * numHeads  * spt * sectorSize / 1024;
                    dirSectStart = dirStart / sectorSize;
                    fatPtr = DiskType[ctr, 9]; // First FAT location
                    fatSectors = DiskType[ctr, 10];
                    dataStart = dirStart + dirSize;
                    break;
                }

            // error if no match found
            if (ctr == DiskType.GetLength(0))
                MessageBox.Show("Error - MS-DOS Disk Type not found in File", diskFileName, MessageBoxButtons.OK);
            else
                result = 1;
            if (result == 1) // done error checking, read directory
            {
                // Read Dir
                var diskUsed = 0;
                var skewMap = BuildSkew(intLv, spt);
                for (var i = 0; i < dirSize / sectorSize; i++) // loop through # sectors in directory
                {
                    bufPtr = dirStart + i / spt * sectorSize * spt + skewMap[i % spt] * sectorSize;
                    bufPtr = dirStart + i * sectorSize;
                    for (var dirPtr = 0; dirPtr < sectorSize; dirPtr += 32) // loop through sector checking DIR entries
                        if (buf[bufPtr + dirPtr] != 0xe5 && buf[bufPtr + dirPtr] != 0x00 && buf[bufPtr + dirPtr] != 0x2E)   // erased, available, or . or .. entry
                        {
                            // process flag data
                            var flagStr = "";
                            if ((buf[bufPtr + dirPtr + 0x0b] & FRead) > 0) flagStr += "R/O";
                            if ((buf[bufPtr + dirPtr + 0x0b] & FSys) > 0) flagStr += " S";
                            if ((buf[bufPtr + dirPtr + 0x0b] & FHidden) > 0) flagStr += " H";
                            if ((buf[bufPtr + dirPtr + 0x0b] & FLabel) > 0) flagStr += "Vol Label";
                            if ((buf[bufPtr + dirPtr + 0x0b] & FSub) > 0)
                            {
                                flagStr += "Directory";
                                subDir = true;
                            }

                            // get file name in both string and byte format
                            var fnameStr = encoding.GetString(buf, bufPtr + dirPtr , 11);
                            var t = bufPtr + dirPtr + 0x1c;
                            var fileDirSize = buf[t] + buf[t + 1] * 256 + buf[t + 2] * 256 * 256 + buf[t + 3] * 256 * 256 * 256;
                            t = bufPtr + dirPtr + 0x16;
                            var fTime = buf[t++] + buf[t++]*256; 
                            var fDate = buf[t++] + ((int) buf[t++]) *256;
                            //var datestr = DosDateStr(fDate);
                            //var timestr = DosTimeStr(fTime);

                            var fFat = buf[t++] + buf[t++] * 256;

                             var temp = new DirList(fnameStr, fileDirSize, flagStr, fTime, fDate, fFat, subDir); // temp storage
                            Array.Copy(buf, bufPtr + dirPtr + 1, temp.fnameB, 0, 11); // copy byte filename
                            //diskUsed += fileDirSize;
                            fileNameList.Add(temp);
                            if (subDir)
                            {
                                ReadMsdosSubDir( fnameStr, fFat);
                            }
                        }

                    fileNameList.Sort();
                }
            }
        }

        /********************* Read MSDOS Sub Dir ************************************
         *
         */
        public void ReadMsdosSubDir(string dirFname, int fatPtr)
        {
            var bufPtr = dataStart + (fatPtr - 2) * albNumSize  * sectorSize;
            var encoding = new UTF8Encoding();
            for (var dirPtr = 0; dirPtr < sectorSize; dirPtr += 32) // loop through sector checking DIR entries
                if (buf[bufPtr + dirPtr] != 0xe5 && buf[bufPtr + dirPtr] != 0x00 && buf[bufPtr + dirPtr] != 0x2E)   // erased, available, or . or .. entry
                {
                    // process flag data
                    var flagStr = "";
                    if ((buf[bufPtr + dirPtr + 0x0b] & FRead) > 0) flagStr += "R/O";
                    if ((buf[bufPtr + dirPtr + 0x0b] & FSys) > 0) flagStr += " S";
                    if ((buf[bufPtr + dirPtr + 0x0b] & FHidden) > 0) flagStr += " H";
                    if ((buf[bufPtr + dirPtr + 0x0b] & FLabel) > 0) flagStr += "Vol Label";
                    if ((buf[bufPtr + dirPtr + 0x0b] & FSub) > 0)
                    {
                        flagStr += "Directory";
                    }

                    // get file name in both string and byte format
                    var fnameStr = dirFname+ "\0"+ encoding.GetString(buf, bufPtr + dirPtr, 11);
                    var t = bufPtr + dirPtr + 0x1c;
                    var fileDirSize = buf[t] + buf[t + 1] * 256 + buf[t + 2] * 256 * 256 + buf[t + 3] * 256 * 256 * 256;
                    t = bufPtr + dirPtr + 0x16;
                    var fTime = buf[t++] + buf[t++] * 256;
                    var fDate = buf[t++] + ((int) buf[t++]) * 256;
                    var fFat = buf[t++] + buf[t++] * 256;

                    var temp = new DirList(fnameStr, fileDirSize, flagStr, fTime, fDate, fFat, false); // temp storage
                    Array.Copy(buf, bufPtr + dirPtr + 1, temp.fnameB, 0, 11); // copy byte filename
                    //diskUsed += fileDirSize;
                    fileNameList.Add(temp);
                }
        }

        // ************** DOS Time String *********************
        // inputs: DOS Time integer from Directory entry
        // output: stirng in readable format
        // Bits 15-11	Hours(0-23)
        // Bits 10-5	Minutes(0-59)
        // Bits 4-0	Seconds/2 (0-29)

        public string DosTimeStr(int time)
        {
            string result = "";
            int minutes, hours, seconds;

            hours = (time & 0xfC00) >> 11;
            minutes = (time & 0x07E0) >> 5;
            seconds = (time & 0x001f);
            result = string.Format("{0,2:D2}:{1,2:D2}", hours, minutes);
            return result;
        }
        // ************** DOS Day String *********************
        // inputs: DOS Time integer from Directory entry
        // output: stirng in readable format
        // Bits 15-9	years from 1980
        // Bits 8-5	Month 1-12
        // Bits 4-0	Day 1-31
        public string DosDateStr(int date)
        {
            string result = "";
            int year, month, day;
            year = (date & 0xfE00);
            year = 1980+ ((date & 0xfE00) >> 9);
            month = (date & 0x01E0) >> 5;
            day = (date & 0x001f);
            result = string.Format("{0,2:D2}/{1,2:D2}/{2,4:D2}", month, day, year);
            return result;
        }


        // ************** Extract File MS-DOS  *********************
        // inputs: path and filename, disk entry structure
        // output: requested file
        public int ExtractFileMsdos(Form1.DiskFileEntry disk_file_entry)
        {
            var result = 1; // assume success
            var maxBuffSize = 0x2000; // largest allocation block size
            var diskTotal = 0;
            var disk_image_file = disk_file_entry.DiskImageName;

            if (disk_file_entry.fFlags.Contains("Directory"))           // Directory, nothing to extract
                return 0;
            if (disk_image_file != DiskImageActive) ReadMsdosDir(disk_image_file, ref diskTotal);

            var encoding = new UTF8Encoding();
            var dir = string.Format("{0}_Files",disk_image_file); // create directory name and check if directory exists
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            fnameb = encoding.GetBytes(disk_file_entry.FileName);

            // Create output File
            var pos = disk_file_entry.FileName.IndexOf("\0");
            if (pos < 0) 
                pos = 0;
            else
                pos++;
            var name = disk_file_entry.FileName.Substring(pos, 8).Trim(' ');
            var ext = disk_file_entry.FileName.Substring(pos+8, 3).Trim(' ');
            var file_name = string.Format("{0}\\{1}.{2}", dir, name, ext);

            if (File.Exists(file_name))
                if (MessageBox.Show("File exists, Overwrite it?", "File Exists", MessageBoxButtons.YesNo) ==
                    DialogResult.No)
                {
                    result = 0;
                    return result;
                }

            var file_out = File.Create(file_name);
            var bin_out = new BinaryWriter(file_out);

     
            // Find filename in DIRList
            var obj = fileNameList.FirstOrDefault(x => x.fname == disk_file_entry.FileName);
            if (obj != null)
            {
                var rBptr = 0; // read buffer ptr
                var wBptr = 0; // write buffer ptr
                var buffSize = obj.fsize;
                if (buffSize == 0) buffSize = 4096;
                var wBuff = new byte[buffSize + albSize]; //   write buffer

 
                var fileFatPtr = obj.firstFat;
                int fatVal = fileFatPtr;                
                // get first sector from directory entry
                var sectOffset = dataStart;         // place holder

                while (fileFatPtr < maxFat) {
                    // Sectors count from first, 1
                     rBptr = dataStart + (fatVal-2) * albSize;
                     var t = rBptr;
                     for (var l = 0; l < albSize; l++)
                         wBuff[wBptr++] = buf[rBptr++];
                        var fileFatStart = fatPtr + (fileFatPtr * 3) / 2;
                        fatVal = buf[fileFatStart++]+ buf[fileFatStart++]*256;
                        if (fileFatPtr % 2 == 0)  //even
                            fatVal = fatVal & 0xfff;
                        else fatVal= fatVal >> 4;          // odd
                        // fatVal contains cluster number to get
                        if (fatVal == 0xfff) 
                            break;// end of chain
                        else 
                            fileFatPtr = fatVal;
                }

                if (wBptr > buffSize) 
                    wBptr = buffSize;
               bin_out.Write(wBuff, 0, wBptr);
            }

            bin_out.Close();
            file_out.Close();

            return result;
        }

        //********************* View File MS-DOSFeature *********************************
        // View files from MS-DOS images

        public byte[] ViewFileMsdos(string fileName)
        {

            if (DiskImageActive.Length > 0)
            {
                var skewMap = BuildSkew(intLv, spt);

                var obj = fileNameList.FirstOrDefault(x => x.fname == fileName);
                if (obj != null)
                {
                    var rBptr = 0; // read buffer ptr
                    var wBptr = 0; // write buffer ptr
                    var wBuff = new byte[obj.fsize + 256]; //   write buffer
                    /*
                    foreach (var f in obj.fname)
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
                                    var t1 = sectOffset % spt; // sector to get on the track
                                    var t2 = sectOffset / spt; // # of tracks
                                    rBptr = dirStart + sectOffset / spt * sectorSize * spt +
                                            skewMap[sectOffset % spt] * sectorSize;
                                    var j = 0;
                                    for (; j < sectorSize / 128 && j < fcbNum; j++)
                                        for (var l = 0; l < 128; l++)
                                            wBuff[wBptr++] = buf[rBptr++];
                                    fcbNum -= j;
                                }
                    }
                    */
                    return wBuff;
                }

            }
            var fileImage = new byte[1];
            fileImage[0] = 0;
            return fileImage;

        }


        //******************** Build Skew *************************************
        // returns an integer array of size spt with the requested interleave intLv
        // array is in logical to physical format
        // logical sector is array index, value is physical order
        public int[] BuildSkew(int intLv, int spt)
        {
            var physicalS = 0;
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

        //*************************** Not needed ********************************************

        //*************** Read IMD Directory
        public void ReadDosImdDir(string diskFileName, ref long diskTotal)
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
            var readSize = 0;

            if (diskFileName != DiskImageActive) // check if data already in memory
            {
                var file = File.OpenRead(diskFileName); // read entire file into an array of byte
                var fileByte = new BinaryReader(file);
                var fileLen = (int)file.Length;
                readSize = fileLen;
                //byte[] buf = new byte[bufferSize];
                try
                {
                    if (fileByte.Read(buf, 0, readSize) != fileLen)
                    {
                        MessageBox.Show("IMD file read error", "Error", MessageBoxButtons.OK);
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("File buffer too small", "Error", MessageBoxButtons.OK);
                    return;
                }

                DiskImageActive = diskFileName;
                diskSize = fileLen;
                fileNameList.Clear();
            }
            else
            {
                return; // list is current do nothing
            }

            var bufPtr = 0;
            while (buf[bufPtr] != 0x1a && bufPtr < readSize) bufPtr++; // look for end of text comment in IMD file
            if (bufPtr < bufferSize && buf[bufPtr + 1] < 6) // process as IMD file
            {
                bufPtr += 4;
                spt = buf[bufPtr++]; // sectors per track
                if (spt == 0)
                {
                    DiskImageActive = "";  // data read fail
                    MessageBox.Show("Sector Per Track read as 0", "Error",
                        MessageBoxButtons.OK);
                    return;
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
                while (sectorCnt < sectorMax)
                {
                    // debug
                    // int t1 = sectorCnt % spt;
                    //int t2 = skewMap[sectorCnt % spt];
                    //int t3 = (sectorCnt / spt) * spt;
                    diskMap[sectorCnt / spt * spt + skewMap[sectorCnt % spt] - 1] =
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
                            MessageBox.Show("IMD sector marker out of scope", "Error",
                                MessageBoxButtons.OK);
                            DiskImageActive = "";        // disk read failed, mark disk image inactive
                            return;
                    }

                    if ((sectorCnt + 1) % spt == 0 && sectorCnt > 0)
                        bufPtr += 5 + spt; // skip track header and interleave info
                    sectorCnt++;
                }
                //

                diskType = (int)buf[diskMap[0] + 6];


                for (ctr = 0; ctr < DiskType.GetLength(0); ctr++) // search DiskType array for values
                    if (diskType == DiskType[ctr, 0])
                    {
                        albSize = DiskType[ctr, 1]; // ALB Size
                        albNumSize = DiskType[ctr, 3]; // size of ALB size in directory
                        dirStart = DiskType[ctr, 2]; // physical start of directory
                        dirSizeD = DiskType[ctr, 4]; // size of the directory
                        sptD = DiskType[ctr, 6]; // sectors per track  
                        sectorSizeD = DiskType[ctr, 7]; // sector size
                        numTrack = DiskType[ctr, 7];
                        numHeads = DiskType[ctr, 8];
                        diskSize = diskTotal = numTrack * numHeads* spt * sectorSize / 1024;
                        dirSectStart = dirStart / sectorSize;
                        break;
                    }

                // error if no match found
                if (ctr == DiskType.GetLength(0))
                    MessageBox.Show("CP/M Disk Type not found in IMD File", "Error", MessageBoxButtons.OK);
                else
                    result = 1;


                if ((spt != sptD || sectorSize != sectorSizeD) && result == 1)
                {
                    MessageBox.Show("Sector/track or sector size mismatch", "Error", MessageBoxButtons.OK);
                    result = 0;
                }

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

                                    var fnameStr = encoding.GetString(buf, bufPtr + dirPtr + 1, 11);
                                    //fnameStr = fnameStr.Insert(8, " ");
                                    var fileDirSize = buf[bufPtr + dirPtr + 15] * 128;
                                    var t = bufPtr + dirPtr + 0x16;
                                    var fTime = buf[t++] + buf[t++] * 256;
                                    var fDate = buf[t++] + buf[t++] * 256;
                                    var fFat = buf[t++] + buf[t++] * 256 + (buf[t++] + buf[t++] * 256) * 256 * 256;
                                    var temp = new DirList(fnameStr, fileDirSize, flagStr, fTime, fDate, fFat, false); // temp storage
                                    Array.Copy(buf, bufPtr + dirPtr + 1, temp.fnameB, 0, 11); // copy byte filename
                                    diskUsed += fileDirSize;

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
                DiskImageActive = "";
                fileNameList.Clear();
            }

            return;
        }

        //****************************** NOT USED **************
        //***************** Extract File MS-DOS IMD
        // Call ReadImdDisk to make sure image is in memory
        // Check to make sure file is in DirList
        public int ExtractFileMsdosImd(Form1.DiskFileEntry diskFileEntry)
        {
            var diskImage = diskFileEntry.DiskImageName;
            //var fileNameListtemp = new List<CPMFile.DirList>();
            long diskUsed = 0, diskTotal = 0;
            var result = 0;


            ReadDosImdDir(diskImage, ref diskTotal);
            var obj = fileNameList.FirstOrDefault(x => x.fname == diskFileEntry.FileName);
            if (obj != null)
            {
                var encoding = new UTF8Encoding();
                var dir = string.Format("{0}_Files", diskImage); // create directory name and check if directory exists
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                fnameb = encoding.GetBytes(diskFileEntry.FileName);

                // Create output File
                var name = diskFileEntry.FileName.Substring(0, 8).Trim(' ');
                var ext = diskFileEntry.FileName.Substring(8, 3).Trim(' ');
                var file_name = string.Format("{0}\\{1}.{2}", dir, name, ext);

                if (File.Exists(file_name))
                    if (MessageBox.Show("File exists, Overwrite it?", "File Exists", MessageBoxButtons.YesNo) ==
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
                foreach (var f in obj.fname)
                {
                    t0++;
                    // CP/M version of code
                    /*
                    var fcbNum = f.fcbnum; // number of 128 byte CP/M records in the FCB
                    for (var i = 0;
                        i < 16 / albNumSize && fcbNum > 0;
                        i++) // read each fcb block in record. may be 8 or 16 values
                        if (f.fcb[i] > 0) // only process valid allocation blocks
                        {
                            var sectPerAlb = albSize / sectorSize;
                            //var t2 = f.fcb[i] * sectPerAlb + dirSectStart; // debug
                            for (var albCnt = 0;
                                albCnt < sectPerAlb;
                                albCnt++) // number of sectors to read in this allocation block
                            {
                                //var t3 = f.fcb[i] * sectPerAlb + dirSectStart + albCnt;
                                var bufPtr =
                                    diskMap[
                                        f.fcb[i] * sectPerAlb + dirSectStart + albCnt]; // location of sector in buf[]
                                var bufData =
                                    buf[bufPtr]; // get IMD sector marker. If odd, a sector worth of data follows

                                if (bufData % 2 > 0) // IMD sector marker. odd number equals sector worth of data
                                {
                                    bufPtr++; // point to first data byte
                                    var k = 0; // declared outside for loop to preserve value
                                    for (;
                                        k < sectorSize / 128 && k < fcbNum;
                                        k++) // read only one sector or the number of fcb records left
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
                    */
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


    }

}
