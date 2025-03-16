using DiskUtility;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using static DiskUtility.Form1;
using IDLDESC = System.Runtime.InteropServices.IDLDESC;


namespace HDOS
{
    public class HDOSFile
    {
        // test bit 7 for the following filename chars
        //private const byte FRead = 9; //  Read only
        //private const byte FSys = 10; // 1 = true
        //private const byte FChg = 11; // bit 7 means file changed

        //private const byte FMask = 0x80; // bit mask
        //private const int BuffLen = 0x2000; // buffer size


        ///********** data values for reading disks       */
        private const int bufferSize = 80 * 2 * 16 * 256 + 1024; // Tracks * Head * SPT * Sector Size + extra

        private const int diskLabelLen = 60;

        public byte[] buf = new byte[bufferSize];

        public string addFilename = "";

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
        public int fileLen = 0;

        public List<DirList> fileNameList = new List<DirList>();


        // 0.Disk type, 1.# Track, 2. Sectors per Track, 3. Groups, 4. SPG 5. Available Sectors, 6. Size, 7 Heads
        public int[,] DiskType =
        {
            // 0    1  2    3    4     5    6      7
            { 0x6b, 80, 16, 255, 10, 2550, 655360, 2 }, // 1 640k H37 96tpi DD DS
            { 0x00, 40, 16, 213,  6, 1278, 327680, 2 }, //  6 320k H37 48tpi DD DS
            { 0x60, 40, 10, 200,  2,  400, 102400, 1 }, //   7 100k H37 48tpi DD SS

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
            public List<int> grtList ;    

            public DirList()
            {
                fname = "";
                fsize = 0;
                userArea = 0;
                //grtList = new List<int> { 0 };    // initialize to 0
                flags = createDate = modDate = "";
                fGroup = lGroup = lastGroupSector = 0;

            }
            public DirList(string tFname,  string tFlags, int tuser, int fgroup, int lgroup,
                int lastgroupsector)
            {
                fname = tFname;
                fsize = 0;
                userArea = tuser;
                flags = tFlags;
                //grtList = new List<int> { 0 };
                createDate = modDate = "";
                fGroup = fgroup;
                lGroup = lgroup;
                lastGroupSector = lastgroupsector;

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
            var result = Globals.Results.Success;
            long
                //diskItemp,
                filei = 0; // file buffer index


            // Read file to add to image
            var file = File.OpenRead(filename);
            var cDate = File.GetLastWriteTime(filename);
            var len = file.Length; // read entire file into buffer
            var padLen = (len / 256)*256;
            if (len - padLen != 0)              // make buffer a multiple of 256
                padLen += 256;
            var filebuf = new byte[padLen];
            var bin_file = new BinaryReader(file);
            bin_file.Read(filebuf, 0, (int)len);
            bin_file.Close();
            file.Dispose();
            
            for (var i = len; i < padLen; i++)      // pad buffer with 0
                filebuf[i] = 0;
            // write the file to the disk image
            var filename8 = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrEmpty(filename8))
                return Globals.Results.Fail;

            var encoding = new ASCIIEncoding();
            filename8 = filename8.Substring(0, Math.Min(filename8.Length, 8));
            filename8 = filename8.PadRight(8, '\0');
            var ext3 = Path.GetExtension(filename);
            ext3 = string.IsNullOrEmpty(ext3) ? ext3 = "\0\0\0" : ext3 = ext3.Substring(1, Math.Min(ext3.Length - 1, 3));
            ext3 = ext3.PadRight(3, '\0');
            var filenameb = string.Format(filename8 + ext3).ToUpper();
            var fileNameStr = filenameb.Replace('\0',' ');

            var obj = fileNameList.FirstOrDefault(x => x.fname == fileNameStr);
            if (obj != null)
            {
                MessageBox.Show("File Exists in image. Skipping", fileNameStr, MessageBoxButtons.OK);
                return Globals.Results.Fail;
            }

            // Find a Directory Entry
            var startDir = diskDirectStart;
            var cntDirs = 0;
            bool found = false;

            while (!found && startDir != 0)
            {
                while (cntDirs < dirCnt && !found)
                    if (buf[startDir + cntDirs * dirLen] > 0xfd)
                    {
                        found = true;
                        startDir += cntDirs * dirLen;
                    }
                    else
                        cntDirs++;
                if (!found)
                {
                    startDir = (buf[startDir+510]+ buf[startDir + 511]*256)*256;
                    // check for shorter than normal directories
                    if (startDir % diskSectorPerGroup == 0)         // end of Group
                    {
                        if (buf[diskGrtStart + startDir / diskSectorPerGroup/256] == 0) // out of directory entries
                        {
                            MessageBox.Show("Not enough Directory entries", "Directory Full", MessageBoxButtons.OK);
                            result = Globals.Results.Fail;
                            break;
                        }
                    }
                    cntDirs = 0;
                }
            }

            // Find available disk area
            var grtStart = diskGrtStart ;
            var startFree = buf[grtStart];          // start of free groups, group #
            var nextFree = startFree;                   // next free group, group #
            var cntFree = padLen/diskSectorPerGroup/256;   // number of groups to get
            if (cntFree * diskSectorPerGroup * 256 != padLen)
                cntFree++;
            var numFree = 0;                                // Number of free groups
            if (result == 1)
            {

                while (nextFree != 0 && numFree < cntFree-1)
                {
                    nextFree = buf[grtStart+nextFree];
                    numFree++;
                }

                if (nextFree == 0)
                {
                    MessageBox.Show("Not enough space on the disk. Skipping", "Disk Full", MessageBoxButtons.OK);
                    result = Globals.Results.Full;
                }
            }


            if (result == Globals.Results.Success)              // valid directory entry and space on disk
            {
                var tt3 = (len % (diskSectorPerGroup * 256));
                int lastGroupSector = (int)(padLen %(diskSectorPerGroup*256))/256;
                if ((padLen %( diskSectorPerGroup * 256 ))%256 !=0)             // check if remainder is a multiple of 256
                    lastGroupSector++;
                if(lastGroupSector == 0)
                    lastGroupSector = diskSectorPerGroup;
                var iDate = HdosDate(cDate.Day, cDate.Month, cDate.Year);
                var t2 = HdosDateStr(iDate);
                LoadDirHdos(filenameb,startDir,0,startFree,nextFree,lastGroupSector,iDate);
                // load data 
                var grpPtr = buf[grtStart];
                var grpPtrLast = grpPtr;
                var imgPtr = grpPtr*diskSectorPerGroup*256;   // offset into image buffer
                var fPtr = 0;                       // offset into insert file buffer
                numFree = 0;                    // groups to load counter
                int getBytes = 0;
                while (numFree < cntFree)
                {
                    getBytes = diskSectorPerGroup;
                    if (numFree == cntFree - 1)
                    {
                        getBytes = lastGroupSector;
                    }
                    for (var i = 0; i < getBytes * 256; i++)
                        buf[imgPtr + i] = filebuf[fPtr+i];
                    numFree++;
                    grpPtrLast = grpPtr;
                    grpPtr = buf[grtStart+grpPtr];
                    imgPtr = grpPtr*diskSectorPerGroup*256;
                    fPtr += getBytes * 256;
                }
                // Update GRT.SYS
                buf[grtStart] = buf[grtStart+grpPtrLast];
                buf[grtStart+grpPtrLast] = 0;
            }

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
                        MessageBox.Show("File read error", diskFileName, MessageBoxButtons.OK);
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("File buffer too small", diskFileName, MessageBoxButtons.OK);
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
            diskLabel = diskLabel.Replace('\0', ' ');

            startGroupBufPtr = startDirBufPtr = dirBufPtr = diskDirectStart; // start of cluster
            dirGroup = startGroupBufPtr / diskSectorPerGroup / 256; // start Group for DIRECT.SYS
            var fnameStr = "";
            while (dirBufPtr != 0) // Read each sector
            {
                if (dirBufPtr > fileLen)    // ERROR CHECK
                    break;
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
                    
                    // File Creation Date
                    var fDate = buf[dirBufPtr + 0x13] * 256 + buf[dirBufPtr + 0x14];

                    // Create temp storage
                    firstGroup = buf[dirBufPtr + 0x10];
                    nextGroup = firstGroup;
                    lastGroup = buf[dirBufPtr + 0x11];
                    lastGroupSector = buf[dirBufPtr + 0x12];

                    var temp = new DirList(fnameStr,  flagStr, userArea, firstGroup,
                        lastGroup, lastGroupSector); // temp storage
                    
                    // Calculate file size
                    var fileDirSize = diskSectorPerGroup;
                    temp.grtList = new List<int> {firstGroup};
                    var fileNext = firstGroup;
                    var cnt = 0; // safety counter
                    
                    while ((fileNext != 0) && (cnt++ < 255))
                    {
                        fileNext = buf[diskGrtStart + fileNext];
 
                        if (fileNext != 0)
                        {
                            temp.grtList.Add(fileNext);
                            fileDirSize += diskSectorPerGroup;
                        }
                        else
                        {
                            fileDirSize += buf[dirBufPtr + 0x12] - diskSectorPerGroup;
                        }
                    }

                    var tt2 = temp.grtList.Count;
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
                                    // check if next group = 0 or next group != current dirBufPtr
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

        // ************** HdosDate  *********************
        // inputs: int day, month, year
        // output: Date int, Bit format "MMMDDDDDYYYYYYYM"

        private int HdosDate(int d, int m, int y)
        {
            d = d << 8;
            m = (m & 0x07) << 16 + (m & 0x08) >> 3;
            y = (y-1970) << 1;
            return d + m + y;
        }

        // ************** HdosDateStr  *********************
        // inputs: Date int, Bit format "MMMDDDDDYYYYYYYM"
        //This is the new y2k date encoding:
        //15-----------9 8------5 4--------0
        //| 7-bits | 4-bits | 5-bits |
        //-------------- -------- ----------
        //Year 00-99 Mon 1-12 Day 1-31
        //
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
            if(year > 100)
                year -= 100;
            
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
        // inputs: Form1 DiskFileEntry with disk image name
        // output: File extracted count
        public int ExtractFileHDOS(Form1.DiskFileEntry disk_file_entry)
        {
            var result = 1; // assume success
            var maxBuffSize = 0x2000; // largest allocation block size
            var diskTotal = 0;

            var disk_image_file = disk_file_entry.DiskImageName;
            if (disk_image_file != DiskImageActive) 
                ReadHdosDir(disk_image_file, ref diskTotal);

            var encoding = new UTF8Encoding();
            var dir = string.Format("{0}_Files", disk_image_file); // create directory name and check if directory exists
            if (!Directory.Exists(dir)) 
                Directory.CreateDirectory(dir);
            //fnameb = encoding.GetBytes(disk_file_entry.FileName);

            // Create output File
            var name = disk_file_entry.FileName.Substring(0, 8).Trim(' ');
            var ext = disk_file_entry.FileName.Substring(8, 3).Trim(' ');
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

 
            // Find filename in DIRList
            var obj = fileNameList.FirstOrDefault(x => x.fname == disk_file_entry.FileName);
            if (obj != null)
            {
                var rBptr = 0; // read buffer ptr
                var wBptr = 0; // write buffer ptr
                var wBuff = new byte[obj.fsize *diskSectorPerGroup*256+ 256]; //   create write buffer
                //var first = obj.fGroup;
                var last = obj.lGroup;
                var numSects = diskSectorPerGroup;
                var tt = obj.grtList.First() * diskSectorPerGroup * 256;
                var binaryFile = buf[obj.grtList.First() * diskSectorPerGroup * 256];

                foreach (var f in obj.grtList)
                {
                    rBptr = f * diskSectorPerGroup * 256;
                    if (f == last)
                        numSects = obj.lastGroupSector;
                    for (var i = 0; i < 256 * numSects; i++)
                        wBuff[wBptr++] = buf[rBptr + i];
                }
                if(binaryFile != 0xff)
                    while (wBuff[--wBptr] == 0) ;
                bin_out.Write(wBuff, 0, wBptr);
            }

            bin_out.Close();
            file_out.Close();

            return result;
        }
        // **************  Delete File HDOS *********************
        // inputs: Form1 DiskFileEntry with disk image name
        /*
             * filename = HDOS filename
            * ref byte[] fileBuff
            * Assumes ReadHdosDir already populated image parameters
            */
        public int DeleteFileHDOS(Form1.DiskFileEntry disk_file_entry)
        {

            var result = 0;
            long
                //diskItemp,
                filei = 0; // file buffer index
            var diskImage = disk_file_entry.DiskImageName;
            var fileNameStr = disk_file_entry.FileName;
            char[] fileNameB= new char[fileNameStr.Length];     // make a char copy of filename with ' ' = 0
            for(var i = 0; i < fileNameStr.Length;i++)
                if (fileNameStr[i] != ' ')
                    fileNameB[i] = fileNameStr[i];
                else
                    fileNameB[i] = '\0';
            
            if (DiskImageActive != diskImage) // image mismatch
                return result;
            var obj = fileNameList.FirstOrDefault(x => x.fname == fileNameStr);
            if (obj == null)
            {
                MessageBox.Show("File Not Found", fileNameStr, MessageBoxButtons.OK);
                return result;
            }
            // Find DIRECT.SYS entry
            var start = diskDirectStart;
            var dirFind = 0;
            byte grtStart = 0;
            var cnt = 0;
            while (dirFind == 0)
            {
                for (cnt = 0; cnt< 11; cnt++)
                    if (buf[start + cnt] != fileNameB[cnt])
                        break;
                if (cnt == 11)
                {
                    dirFind = 1;
                    buf[start] = 0xff;      // mark available
                    grtStart = buf[start + 0x10];
                }
                else
                {
                    start += dirLen;
                    if (buf[start+1] == dirLen)
                    {
                        var tt = buf[start + 4];
                        var tt1 = buf[start + 5];
                        start = (buf[start + 4] + buf[start + 5]*256)*256;
                        if (start == 0)
                            break;
                    }
                }
            }

            if (dirFind == 1)
            {
                var temp = buf[diskGrtStart];
                buf[diskGrtStart] = grtStart ;
                while (buf[ diskGrtStart+ grtStart]!=0)
                    grtStart = buf[diskGrtStart + grtStart];
                buf[diskGrtStart + grtStart] = temp;
                result = 1;
            }

            


            return result;
        }

        //********************* View File HDOS *********************************
        // View files from IMG or H8D images

        public byte[] ViewFileHDOS(string fileName)
        {

            if (DiskImageActive.Length > 0)
            {
                var obj = fileNameList.FirstOrDefault(x => x.fname == fileName);
                if (obj != null)
                {
                    var rBptr = 0; // read buffer ptr
                    var wBptr = 0; // write buffer ptr
                    var buffSize = obj.fsize*256;
                    var last = obj.lGroup;
                    var numSects = diskSectorPerGroup;
                    var wBuff = new byte[buffSize]; //   write buffer + 256 removed

                    foreach (var f in obj.grtList)
                    {
                        rBptr = f * diskSectorPerGroup * 256;
                        if (f == last)
                            numSects = obj.lastGroupSector;
                        for (var i = 0; i < 256 * numSects; i++)
                            wBuff[wBptr++] = buf[rBptr + i];
                    }
                    // look for 0x1A to indicate end of text file
                    var chk = 0;
                    for (; chk < buffSize; chk++)
                    {
                        if (wBuff[chk] == 0x1a)
                            break;
                    }
                    if (chk == buffSize)
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
        //***************** Init HDOS Disk ******************
        // input: disk size total, disk file name
        // output: create disk image in buf[] and write file to disk
        //
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
            //MessageBox(diskLabel, "Change Disk Label?");
            var prompt = "Enter Desired Disk Label";
            var title = "";
            var label = diskLabel;
            if (InputBox(prompt, title, ref label) == DialogResult.OK)
                if (label.Length > 60)
                    diskLabel = label.Substring(0, diskLabelLen);
                 else
                    diskLabel = label; 

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
            var flag = 0b11100000; // SLW___D_  Ver2 SLWC1234; Ver 3 SLWCABDU  1234 & U are user flags
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


            //RGT.SYS
            bufPtr = rgtSector*256;
            buf[bufPtr] = 0;
            buf[bufPtr + 1] = 0;
            for (j = 2; j < diskTotal/sectorsCluster/256; j++)
                buf[bufPtr + j] = 0x01;
            buf[bufPtr + 0xff] = 0xff;
            for (; j < 256; j++)
                buf[bufPtr + j] = 0xff;

            // DIRECT.SYS

            var dirSize = sectorsCluster;
            if (diskTotal < 104000)
                dirSize = 4;
            else
                dirSize = sectorsCluster / 2;
          
            bufPtr = directSector*256;
            for (var k = 0; k < dirSize; k++)       // each loop writes 512 bytes
            {
                for( j = 0;j < dirCnt; j++)         // Write blank directory entry 
                {
                    if(k < 2)
                        buf[bufPtr + j * dirLen] = 0xff;        // empty DIR marker
                    else 
                        buf[bufPtr + j * dirLen] = 0xfe;
                    for (var i = 1; i < dirLen; i++)            // zero fill entry
                        buf[bufPtr+ j*dirLen + i] = 0;
                }

                var l = 0x1fa;
                buf[bufPtr + l++ ] = 0;
                buf[bufPtr + l++] = 0x17;
                LoadBufLsMs(bufPtr+l, bufPtr/256);
                LoadBufLsMs(bufPtr+l+2, (bufPtr+512)/256);
                bufPtr += 512;
            }
            LoadBufLsMs(bufPtr -2, 0);
            //buf[bufPtr] = 0xff;
            byte grtGroup = (byte)(grtSector / sectorsCluster); // HDOS 3, 640k disk values: 57
            byte directGroup = (byte)(directSector / sectorsCluster); // 54
            byte rgtGroup = (byte)(rgtSector / sectorsCluster); // 2           LoadBufLsMs(bufPtr -2, 0);
            bufPtr = directSector * 256;
            bufPtr = bufPtr + 512+dirLen * 18;    // put the three files where HDOS 2 is happy
            LoadDirHdos("RGT     SYS", bufPtr , flag, rgtGroup, rgtGroup, 1, diskDate); 
            LoadDirHdos("GRT     SYS", bufPtr += dirLen, flag, grtGroup, grtGroup, 1, diskDate);
            LoadDirHdos("DIRECT  SYS", bufPtr += dirLen, flag, directGroup, 
                        directGroup+dirSize/sectorsCluster, sectorsCluster, diskDate);
            buf[bufPtr+dirLen] = 0xfe;         // HDOS 2 Happy marker


            // Set up GRT.SYS
            //   disk is formatted to 'GL'
            bufPtr = grtSector * 256;
            for( j = 0; j < 256; j++)
                buf[bufPtr +j] = 1;     // fill GRT with 1's
                // setup three required file groups
            buf[bufPtr + grtGroup] = 0; // GRT is one sector, list of used/available clusters
            buf[bufPtr + rgtGroup] = 0; // RGT is one sector, list of good/bad clusters
            for(j = directGroup; j < directGroup + dirSize / sectorsCluster; j++) // DIRECT.SYS Groups
                buf[bufPtr + j] = (byte) (j+1);
            buf[bufPtr + j] = 0;


                // create free list
             var nextGroup = rgtGroup+1; 
             buf[bufPtr] = (byte) nextGroup;

            for (j = 5; j < diskSize / sectorsCluster-1; j++)
                if (buf[bufPtr + j + 1] == 1) // available group
                {
                    buf[bufPtr+nextGroup] = (byte)(j + 1);
                    nextGroup = j + 1;
                }

            buf[bufPtr + j++] = 0;         // mark end of GRT.SYS
            buf[bufPtr + j++] = 0xff;
            for(;j < 256;j++)
                buf[bufPtr + j] = 0xff;



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
