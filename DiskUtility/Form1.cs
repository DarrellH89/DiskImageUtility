using CPM;
using MSDOS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using HDOS;
using static DiskUtility.Form1;

namespace DiskUtility
{
    public partial class Form1 : Form
    {
        public static string FolderStr = "";

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        
        /*
        public struct CPMDirEntry
        {
            public byte flag; 
            public byte[] filename;
            public byte[] fileext;
            public byte extent;
            public byte[] unused;
            public byte sector_count;
            public byte[] alloc_map;
        }
        */
        public struct DiskFileEntry
        {
            public int ListBox2Entry;
            public string DiskImageName;
            public string FileName;
            public int UserArea;
            public string fDate;
            public string fTime;
            public string fFlags;
            public string fileType;
        }

        public ArrayList DiskFileList;

        /*
         public struct DiskLabelEntry
         {
             public int ListBox2Entry;
             public string DiskImageName;
             public string DiskLabelName;
         }

         public ArrayList DiskLabelList;
         public DiskLabelEntry RelabelEntry;
        */

        public bool bImageList = false;

        public int FileCount = 0;
        public int TotalSize = 0;

        public GroupBox FileViewerBorder;
        public RichTextBox FileViewerBox;
  
        public Form1()
        {
            InitializeComponent();
            CenterToScreen();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            labelVersion.Text =
                "Version 1.2c.3 Disk Image Utility based on H8DUtilty"; // version number update Darrell Pelan

            FileViewerBorder = new GroupBox();
            FileViewerBorder.Size = new Size(720, 580);
            FileViewerBorder.Location = new Point(90, 30);
            FileViewerBorder.Text = "File Viewer";
            FileViewerBorder.ForeColor = Color.Black;
            FileViewerBorder.BackColor = Color.DarkGray;
            FileViewerBorder.Visible = false;

            Controls.Add(FileViewerBorder);

            FileViewerBox = new RichTextBox();
            FileViewerBox.Size = new Size(700, 520);
            FileViewerBox.Location = new Point(10, 20);
            FileViewerBox.Font = new Font(FontFamily.GenericMonospace, 10);
            FileViewerBox.BorderStyle = BorderStyle.FixedSingle;
            FileViewerBox.BackColor = Color.LightGray;
            FileViewerBox.ReadOnly = true;

            FileViewerBorder.Controls.Add(FileViewerBox);

            var FileViewerButton = new Button();
            FileViewerButton.Name = "filebutton1";
            FileViewerButton.Text = "CLOSE";
            FileViewerButton.Location =
            new Point(FileViewerBorder.Size.Width / 2 - FileViewerButton.Size.Width / 2, 550);
            FileViewerButton.Click += new EventHandler(filebutton1_Click);
            FileViewerButton.BackColor = Color.LightGray;

            FileViewerBorder.Controls.Add(FileViewerButton);

            BtnExtract.Enabled = false;
            DisableButtons();

            folderBrowserDialog2.ShowNewFolderButton = false;
            DiskFileList = new ArrayList();
            //DiskLabelList = new ArrayList();

            ReadData();
            FileCount = 0;
            // DCP
            if (folderBrowserDialog2.SelectedPath.Length > 0) BtnFolder_init();
        }

        // DCP
        private void BtnFolder_init()
        {
            BtnExtract.Enabled = false; // Catalog button disabled in case no files are found
            BtnFolder_initA();

    
        }

        private void
            BtnFolder_initA() // dcp modified code to read files store in last used directory. initA is used both on startup and when Folder Button is clicked.
        {
            listBoxImages.Items.Clear(); // clear file list
            labelFolder.Text = folderBrowserDialog2.SelectedPath; // display current working directory
            // set file extension types to scan directory
            var file_list = new string[1];
            try
            {
                var h8d_list = Directory.GetFiles(labelFolder.Text, "*.h8d");
                var img_list = Directory.GetFiles(labelFolder.Text, "*.img");
                var imd_list = Directory.GetFiles(labelFolder.Text, "*.imd");
                var h37_list = Directory.GetFiles(labelFolder.Text, "*.h37");
                file_list = new string[h8d_list.Length + img_list.Length + imd_list.Length +
                                       h37_list.Length]; // combine filename lists
                Array.Copy(h8d_list, file_list, h8d_list.Length);
                Array.Copy(img_list, 0, file_list, h8d_list.Length, img_list.Length);
                Array.Copy(imd_list, 0, file_list, h8d_list.Length + img_list.Length, imd_list.Length);
                Array.Copy(h37_list, 0, file_list, h8d_list.Length + img_list.Length + imd_list.Length,
                    h37_list.Length);
            }
            catch
            {
                // Directory not found, clear string
                file_list = null;
                labelFolder.Text = "";
            }
            FolderStr = labelFolder.Text; // used in MSDOS File

            if (file_list == null)
            {
                listBoxImages.Items.Add("No image files found");
                label3.Text = "0 Files";
                bImageList = false;
            }
            else
            {
                foreach (var files in file_list) // add file names to listbox1
                {
                    string fileName = files.Substring(files.LastIndexOf("\\") + 1).ToUpper();
                    listBoxImages.Items.Add(fileName);
                    var file_count = string.Format("{0} disk images", listBoxImages.Items.Count.ToString());
                    label3.Text = file_count;
                }

                BtnExtract.Enabled = true; // enable Catalog button
                bImageList = true;
            }
        }

        private void DisableButtons()
        {
            //BtnAddCpm.Enabled = false;
            //BtnAddMsdos.Enabled = false;
            //BtnImdConvert.Enabled = false;
            //BtnDelete.Enabled = false;
            BtnView.Enabled = false;
        }

        private void ReadData()
        {
            if (File.Exists("DiskUtility.dat"))
            {
                var stream = File.OpenText("DiskUtility.dat");
                if (stream != null)
                {
                    folderBrowserDialog2.SelectedPath = stream.ReadLine();
                    stream.Close();
                }
            }
        }

        private void SaveData()
        {
            var stream = File.CreateText("DiskUtility.dat");
            if (stream != null)
            {
                stream.WriteLine(folderBrowserDialog2.SelectedPath);
                stream.Close();
            }
        }

        private void BtnFolder_Click(object sender, EventArgs e)
        {
            BtnExtract.Enabled = false;
            if (folderBrowserDialog2.ShowDialog() == DialogResult.OK)
            {
                SaveData();
                BtnFolder_initA();
            }
        }

        private void BtnListboxfilesCopy_Click(object sender, EventArgs e)
        {
            string s1 = "";
            foreach (object item in listBoxFiles.Items) 
                s1 += item.ToString() + "\r\n";
            Clipboard.SetText(s1);
        }

        //********************* Add HDOS Blank Disk image ***********************
        private void buttonCreateHdos_click(object sender, EventArgs e)
        {
            var makeDisk = new Form5('H');
            makeDisk.ShowDialog();
            makeDisk.unload();
            Refresh();
            BtnFolder_initA();
        }

        //********************* Add CP/M Blank Disk image ***********************
            private void buttonCreateCPM_click(object sender, EventArgs e)
        {
            var makeDisk = new Form5('C');
            makeDisk.ShowDialog();
            makeDisk.unload();
            Refresh();
            BtnFolder_initA();
        }
        //********************* Add MS-DOS Blank Disk image ***********************
        private void buttonCreateMSDOS_click(object sender, EventArgs e)
        {
            var makeDisk = new Form5('D');
            makeDisk.ShowDialog();
            makeDisk.unload( );
            Refresh();
            BtnFolder_initA();
        }
        //********************* Delete File ***********************
        private void ButtonDelete(object sender, EventArgs e)
        {
            var files_deleted = 0;
            var fileImage_deleted = 0;
            var files_skipped = 0;
            var diskTotal = 0;
            string diskImage = "";

            var getHdosFile = new HDOSFile(); // create instance of HDOSFile, then call function
 
            var idx = listBoxFiles.SelectedIndex;
            if (idx != -1)
            {
                for (var i = 0; i < listBoxFiles.SelectedItems.Count; i++)
                {
                    idx = listBoxFiles.SelectedIndices[i];
                    foreach (DiskFileEntry entry in DiskFileList)
                        if (entry.ListBox2Entry == idx)
                        {
                            if (entry.fileType == "HDOS")
                            {
                                if (diskImage != entry.DiskImageName)
                                {
                                    if (diskImage.Length > 0 && fileImage_deleted>0)     // image change, write last
                                        WriteHdosImage(diskImage, getHdosFile);
                                    diskImage = entry.DiskImageName;
                                    fileImage_deleted = 0;
                                    getHdosFile.ReadHdosDir(diskImage, ref diskTotal);
                                }
                                if (getHdosFile.DeleteFileHDOS(entry) > 0)
                                {
                                    files_deleted++;
                                    fileImage_deleted++;
                                }
                            }
                            else
                                files_skipped++;
                        }
                }
                if (diskImage.Length > 0 && fileImage_deleted > 0)     // image change, write last
                    WriteHdosImage(diskImage, getHdosFile);
                listBoxFiles.ClearSelected();
            }

            var message = string.Format("{0} file(s) deleted, {1} file(s) skipped", files_deleted, files_skipped);
            MessageBox.Show(this, message, "Disk Image Utility HDOS Files Only");
            BtnFileList_Click(sender, e);

        }

        //**************************** Write Hdos Image **********************
        private void WriteHdosImage(string path, HDOSFile hFile)
        {
            FileStream fsOut = new FileStream(path, FileMode.Open, FileAccess.Write);
            BinaryWriter fileOutBytes = new BinaryWriter(fsOut);

            fileOutBytes.Seek(0, SeekOrigin.Current);
            fileOutBytes.Write(hFile.buf, 0, hFile.fileLen);
            fileOutBytes.Close();
            fsOut.Dispose();
        }

        //*************** File List ********************
        private void BtnFileList_Click(object sender, EventArgs e)
        {
            //  catalog selected image(s)
            FileCount = 0;
            TotalSize = 0;
            listBoxFiles.Items.Clear();
            DiskFileList.Clear();

            if (listBoxImages.SelectedIndex != -1)
                // one or more files selected in listbox1
                foreach (var lb in listBoxImages.SelectedItems)
                {
             
                    var disk_name = labelFolder.Text + "\\" + lb; // path + file name
                    // HDOS Check
                    byte[] tempBuf = new byte[512];
                    var file = File.OpenRead(disk_name); // read entire file into an array of byte
                    var fileByte = new BinaryReader(file);
                    fileByte.Read(tempBuf, 0, 256);
                    file.Close();
                    file.Dispose();
                    // HDOS file Check end

                    listBoxFiles.Items.Add(lb.ToString());
                    /*if (lb.ToString().Contains(".H8D"))
                        ProcessFile(disk_name);
                    else 
                    */
                    if (IsHDOSDisk(ref tempBuf))
                        ProcessFileHdos(disk_name);    // process HDOS files
                    else if (lb.ToString().Contains(".DOS")) 
                        ProcessFileDOS(disk_name);  // check for Z100 MS-DOS first
                    else if (lb.ToString().Contains(".IMD"))
                        ProcessFileImd(disk_name);
                    else
                        ProcessFileH37(disk_name);    // process CP/M files
 
                }
            else // no files selected, so process all of them in listbox1
                foreach (var lb in listBoxImages.Items)
                {
                    var disk_name = labelFolder.Text + "\\" + lb;
                    // HDOS Check
                    byte[] tempBuf = new byte[512];
                    var file = File.OpenRead(disk_name); // read entire file into an array of byte
                    var fileByte = new BinaryReader(file);
                    fileByte.Read(tempBuf, 0, 256);
                    file.Close();
                    file.Dispose();
                    // HDOS file Check end

                    listBoxFiles.Items.Add(lb.ToString());
                    if (IsHDOSDisk(ref tempBuf))
                        ProcessFileHdos(disk_name);    // process HDOS files
                    else if (lb.ToString().Contains(".DOS."))
                        ProcessFileDOS(disk_name); // check for Z100 MS-DOS first
                    else if (lb.ToString().Contains(".IMD"))
                        ProcessFileImd(disk_name);
                    else
                        ProcessFileH37(disk_name); // process CP/M files

                }
    
            if (FileCount == 0)
            {
                DisableButtons();
            }
            else
            {
                BtnAddCpm.Enabled = true;
                BtnAddMsdos.Enabled = true;
                BtnImdConvert.Enabled = true;
                BtnView.Enabled = true;
            }

            // dcp changed KB to bytes
            listBoxFiles.Items.Add("");
           listBoxFiles.Items.Add(string.Format("Total Files {0,5:N0}, Total File Size {1,5:N0} K", FileCount,
                TotalSize / 1024));
            listBoxFiles.Items.Add("");
        }
        //************************** TrackData *************************
        // stores IMD track data
        private byte[] TrackData(byte[] b, int ptr)
        {
            var tb = new byte[50];
            int p = ptr, tbptr, temp, power=0;

            tb[1] = b[p++];           // mode
            tb[2] = b[p++];           // cylinder
            tb[3] = b[p++] ;   // head flags, bits 6 & 7
            tb[4] = b[p++];           // SPT
            tb[5] = b[p++];
            //var t1 = b[p];
            //var t2 = Math.Pow(2, t1);
            //var t3 = (byte)(Math.Pow(2, b[p++]) * 128);
            //tb[5] = (byte) (Math.Pow(2, b[p++])*128);   // Sector size flag
            tbptr = 6;
            temp = p + tb[4];
            for (; p < temp; p++)  // sector map
                tb[tbptr++] = b[p];
            temp = p + tb[4];
            if ((tb[3]& 0x80) > 0)                  // Cylinder map
                for (; p < temp; p++)  // sector map
                    tb[tbptr++] = b[p];
            tb[0] = (byte) tbptr;      // number of valid bytes
            return tb;
        }

        //************************** Convert IMD  to IMG or IMG/H37 to IMD ******************************************
        private void BtnImdConvert_click(object sender, EventArgs e)
        {
            var sectorSizeList = new int[] { 128, 256, 512, 1024, 2048, 4096, 8192 }; // IMD values
  
            List<byte[]> trackList = new List<byte[]>() ;
            int trackChange = 0;
            int dataReadError = 0;
            int cylHeadCnt = 0;
            byte[] tb0 = new byte[50] ;
            byte[] tb1 = new byte[50];
            bool abort = false;
            int trackStart;
            int[] diskSkew = new int[25];           // larger than any known SPT
            int result = 0;
            int H8DCount = 0;
            // int diskType = 0;
            var encoding = new UTF8Encoding();
            if (listBoxImages.SelectedIndex != -1)
                foreach (var lb in listBoxImages.SelectedItems)
                {
                    if (lb.ToString().ToUpper().EndsWith(".H8D")) 
                        H8DCount++;
                    if (lb.ToString().ToUpper().EndsWith(".IMD") || lb.ToString().ToUpper().EndsWith(".H37")
                                                                 || lb.ToString().ToUpper().EndsWith(".IMG"))
                                                          //       || lb.ToString().ToUpper().EndsWith(".H8D")) // no need, HxCFloppy works with H8D
                    {
                        var IMDfileType = lb.ToString().ToUpper().EndsWith(".IMD"); // test for IMD file
                        // read entire file into memory
                        var shortFileName = lb.ToString();
                        var diskFileName = labelFolder.Text + "\\" + lb.ToString();
                        var file = File.OpenRead(diskFileName); // read entire file into an array of byte
                        var fileByte = new BinaryReader(file);
                        var fileLen = (int) file.Length;
                        var buf = new byte[fileLen + 5 * 1024];
                        var wbuf = new byte[fileLen + 5 * 1024]; // add extra track as buffer

                        try
                        {
                            if (fileByte.Read(buf, 0, fileLen) != fileLen)
                                MessageBox.Show(shortFileName+" File Read Error", "Error", MessageBoxButtons.OK);
                        }
                        catch
                        {
                            MessageBox.Show(shortFileName + " File Access Error", "Error", MessageBoxButtons.OK);
                            return;
                        }

                        fileByte.Close();
                        file.Close();

                        // create output file
                        int wBufPtr = 0, bufPtr = 0, firstSector = 0;
                        if (IMDfileType) // convert IMD to IMG for H89, Z100, or Small Z80
                        {
                            diskFileName = diskFileName.Replace(".IMD", ".IMG");
                            if (diskFileName.Contains(".H37"))
                                diskFileName = diskFileName.Replace(".H37", "");  // dropping old H37 designation
                        }
                        else
                        {

                            if (diskFileName.EndsWith(".IMG"))
                                diskFileName = diskFileName.Replace(".IMG", ".IMD");
                            else
                                diskFileName = diskFileName + ".IMD";  // H37 file

                        }



                        if (File.Exists(diskFileName))
                            if (MessageBox.Show(diskFileName+" exists, Overwrite it?", "File Exists",
                                    MessageBoxButtons.YesNo) ==
                                DialogResult.No)
                                return;
                        FileStream file_out;
                        try
                        {
                            file_out = File.Create(diskFileName);
                        }
                        catch
                        {
                            MessageBox.Show(shortFileName+" File Access Error", "Error", MessageBoxButtons.OK);
                            return;
                        }

                        var bin_out = new BinaryWriter(file_out);

                        if (IMDfileType) // convert to H37, Z80, Z100
                        {
                            while (buf[bufPtr] != 0x1a && bufPtr < fileLen)
                                bufPtr++; // look for end of text comment in IMD file
                            if (bufPtr < fileLen && buf[bufPtr + 1] < 6
                            ) // process as IMD file - found end of comment and next byte is valid
                            {
                                tb0 = TrackData( buf,++bufPtr );
                                trackList.Add(tb0);         // Save initial track information
                                trackStart = bufPtr;
                                bufPtr += 3; // skip cylinder count, head value used for extra data flag
                                var spt = buf[bufPtr++]; // sectors per track
                                var sectorSize = sectorSizeList[buf[bufPtr++]];
                                //diskSkew = new int[spt];
                                for (var i = 0; i < spt; i++) diskSkew[i] = buf[bufPtr++]; // load skew table
                                //int shift = 0, temp = 0;
                                if((trackStart + tb0[0]-1)!= bufPtr)
                                    Debug.WriteLine("Initial Track Header Miscount. Calc {0:X} Actual {1:X}",
                                            trackStart + tb0[0], bufPtr);
                                if (tb0[3] > 1) // Found cylinder map, advance past it
                                {
                                    bufPtr += spt;
                                    cylHeadCnt++;
                                }

                                firstSector = bufPtr;
                                //var numTrack = 0;

                                int sectorCnt = 0, totalSect = 0;

                                while (bufPtr < fileLen && !abort)
                                {

                                    totalSect++;
                                    var t0 = (sectorCnt) % spt;
                                    var modeVal = buf[bufPtr];
                                    Debug.WriteLine("Sector offset {0:X} Value {1:X}", bufPtr, buf[bufPtr]);
                                    switch (buf[bufPtr])
                                    {
                                        case 1:
                                        case 3:
                                        case 5:
                                        case 7:
                                            bufPtr++; // order sectors in starting with sector 1
                                            var t3 = (diskSkew[sectorCnt] - 1) * sectorSize;
                                            for (var i = 0; i < sectorSize; i++)
                                            {
                                                wbuf[(diskSkew[sectorCnt] - 1) * sectorSize + i] = buf[bufPtr++];
                                            }

                                            if (modeVal != 1)
                                                dataReadError++;
                                            break;
                                        case 2:
                                        case 4:
                                        case 6:
                                        case 8: // compressed sector
                                            bufPtr++;
                                            for (var i = 0; i < sectorSize; i++)
                                                wbuf[(diskSkew[sectorCnt] - 1) * sectorSize + i] = buf[bufPtr];
                                            bufPtr++;
                                            if (modeVal != 2)
                                                dataReadError++;
                                            break;
                                        case 0:
                                            bufPtr++;
                                            break;
                                        default:
                                            MessageBox.Show(shortFileName+" IMD sector marker out of scope " + buf[bufPtr].ToString("X2") 
                                                            + " at location " + bufPtr.ToString("X8")+". Values in hex", "Error",
                                                            MessageBoxButtons.OK);
                                            Debugger.Break();
                                            abort = true; // stop processing due to file error
                                            break;
                                    }

                                    sectorCnt++;
                                    if (sectorCnt == spt)
                                    {
                                        bin_out.Write(wbuf, 0, spt * sectorSize); // write a track at a time
                                        sectorCnt = 0;
                                        /**************
                                        *     NEED TO ADD CODE TO CHECK SPT AND SKEW FOR EACH TRACK
                                        */
                                        tb1 = TrackData(buf, bufPtr);
                                        var ok = true;
                                        for (var i = 0; i < tb1[0]; i++)            // check if track info changed
                                        {
                                            if (tb0[i] != tb1[i])
                                            {
                                                ok = false;
                                                break;
                                            }
                                        }

                                        if ((tb0[4] != tb1[4])&&(bufPtr < fileLen)) // Fatal Error, SPT changed
                                        {
                                            MessageBox.Show( string.Format(shortFileName+" Sector per Track changed from {0} to {1}", 
                                                             tb0[4], tb1[4]), "Fatal Error", MessageBoxButtons.OK );
                                            abort = true;   // flag exit
                                        }

                                        if (!ok)
                                        {
                                            trackList.Add(tb1);         // Save new track information
                                            for (var i = 0; i < tb1[0]; i++)
                                            {
                                                tb0[i] = tb1[i];
                                            }

                                            trackChange++;
                                        }
                                        trackStart = bufPtr;
                                        bufPtr += 3; // skip cylinder count, head value used for extra data flag
                                        spt = buf[bufPtr++]; // sectors per track
                                        if (buf[bufPtr] > sectorSizeList.Length)
                                            Debugger.Break();
                                        sectorSize = sectorSizeList[buf[bufPtr++]];
                                        //diskSkew = new int[spt];
                                        for (var i = 0; i < spt; i++) 
                                            diskSkew[i] = buf[bufPtr++]; // load skew table
                                        //int shift = 0, temp = 0;
                                        var t5 = trackStart + tb1[0];
                                        if ((trackStart + tb1[0]-1)!= bufPtr)       // Found cylinder map, advance past it
                                        {
                                            Debug.WriteLine("Sector Cylinder Map");
                                            bufPtr = trackStart + tb1[0]-1;
                                            cylHeadCnt++;

                                        }

                                        //bufPtr += 5 + spt; // skip track header and interleave info


                                    }
                                }

                                if (!abort)
                                {
                                    if (sectorCnt > 0) 
                                        bin_out.Write(wbuf, 0, (sectorCnt - 1) * sectorSize);
                                    result++;
                                }

                                bin_out.Close();
                                file_out.Close();
                                if(abort)
                                    File.Delete(diskFileName);
                                // Report Results
                                Debug.WriteLine(" Track Data Changes {0}", trackChange);
                                Debug.WriteLine("Data Read Errors {0}", dataReadError);
                                Debug.WriteLine("Cylinder Head Fields {0}", cylHeadCnt);

                            }
                        }
                        else //Convert IMG File H37, Z80, or Z100 to IMD
                        {
                            byte imdSectorIndex = 0;
                            byte imdMode = 5;
                            var ctr = 0;
                            int intLv = 1, spt = 0, sectorSize = 0, diskHeads = 1, sptStart = 1;
                            var skewMap = new int[20];

                            if (lb.ToString().ToUpper().Contains(".DOS"))
                            {
                                // DOS File format
                                var putMsdos = new MsdosFile();
                                for (ctr = 0; ctr < putMsdos.DiskType.GetLength(0); ctr++)
                                {
                                    //var t0 = putMsdos.DiskType[ctr, 5] * putMsdos.DiskType[ctr, 6] * putMsdos.DiskType[ctr, 7] * putMsdos.DiskType[ctr, 8];
                                    if (putMsdos.DiskType[ctr, 5]*putMsdos.DiskType[ctr, 6] * putMsdos.DiskType[ctr, 7] * putMsdos.DiskType[ctr, 8] == fileLen)
                                        break;
                                }

                                if (ctr == putMsdos.DiskType.GetLength(0))
                                {
                                    MessageBox.Show("Could not determine MS-DOS Disk type", "Error",
                                        MessageBoxButtons.OK);
                                    bin_out.Close();
                                    file_out.Dispose();
                                    return;
                                }

                                intLv = 1;
                                spt = putMsdos.DiskType[ctr, 5];
                                sectorSize = putMsdos.DiskType[ctr, 6];
                                diskHeads = putMsdos.DiskType[ctr, 8];
                                sptStart = 1;
                                imdMode = (byte) putMsdos.DiskType[ctr,14];
                                skewMap = putMsdos.BuildSkew(intLv, spt);
                                //result++;
                            }
                            else           //************* CP/M File format
                            {
                               
                                var putCPM = new CPMFile();
                                ctr = putCPM.DiskTypeCheck(ref buf, fileLen, diskFileName.ToUpper().EndsWith("IMG"));
                                if (ctr != putCPM.DiskType.GetLength(0))
                                {
                                    if(lb.ToString().ToUpper().Contains(".IMG"))
                                        intLv = 1; // interleave
                                    else
                                        intLv = putCPM.DiskType[ctr, 5];
                                    spt = putCPM.DiskType[ctr, 6]; // sectors per track  
                                    sectorSize = putCPM.DiskType[ctr, 7]; // sector size
                                    diskHeads = putCPM.DiskType[ctr, 9];
                                    sptStart = putCPM.GetSptStart();
                                    if (putCPM.DiskType[ctr, 0] == 0xff) // check for high density Small Z80 disk
                                        imdMode = 3;
                                    if (diskFileName.ToUpper().Contains(".H37"))
                                    {
                                        sptStart = 0;
                                        intLv = putCPM.DiskType[ctr, 5]; // interleave
                                    }
                                    skewMap = putCPM.BuildSkew(intLv, spt, sptStart);

                                }
                                else
                                {
                                    MessageBox.Show("Could not determine CP/M Disk type", "Error",
                                        MessageBoxButtons.OK);
                                    bin_out.Close();
                                    file_out.Dispose();
                                    return;
                                }
                            }

                            for (ctr = 0; ctr < sectorSizeList.Length; ctr++) // look up IMD sector size parameter
                                    if (sectorSizeList[ctr] == sectorSize)
                                    {
                                        imdSectorIndex = (byte) ctr;
                                        break;
                                    }

                            wBufPtr = bufPtr = 0;
                            byte cylinder = 0, head = 0;
                            var initString = "IMD 1.18 format conversion to IMD by Darrell Pelan Disk Utility";
                            var tempbuf = initString.ToCharArray();
                            for (ctr = 0; ctr < tempbuf.Length; ctr++)
                                wbuf[wBufPtr++] = (byte) tempbuf[ctr];
                            wbuf[wBufPtr++] = 0x1A;
                            while (bufPtr < fileLen)
                            {
                                wbuf[wBufPtr++] = imdMode; // write IMD track data
                                wbuf[wBufPtr++] = cylinder;
                                if (diskHeads > 1)
                                    wbuf[wBufPtr++] = head++;
                                else
                                {
                                    wbuf[wBufPtr++] = head;
                                    cylinder++;                 // increment cylinder every pass since there is only one head
                                }
                                wbuf[wBufPtr++] = (byte) spt;
                                wbuf[wBufPtr++] = imdSectorIndex;
                                for (ctr = 0; ctr < skewMap.Length; ctr++) // write sector order for track
                                    wbuf[wBufPtr + skewMap[ctr]] = (byte) (ctr + 1);
                                wBufPtr += ctr;
                                for (var i = 0; i < spt; i++) // Write sectors for each track
                                {
                                    wbuf[wBufPtr++] = 0x01;
                                    for (ctr = 0; ctr < sectorSize && bufPtr < fileLen; ctr++)
                                        wbuf[wBufPtr++] = buf[bufPtr++];
                                }

                                bin_out.Write(wbuf, 0, wBufPtr);
                                wBufPtr = 0;
                                if (head > 1)           // reset head value and increment cylinder
                                {
                                    cylinder++;
                                    head = 0;
                                }

                            }

                            bin_out.Close();
                            file_out.Dispose();
                            result++;
                        }
                    }
                    abort = false;  // reset for multiple files
                } 
                var resultStr = string.Format("{0} Disks converted.", result);
                if (H8DCount > 0)
                    resultStr += string.Format(" No need to convert {0} H8D files. They are supported by HxCFloppy", H8DCount);
      
                

                if (result == 1) 
                    resultStr = resultStr.Replace("ks", "k");
                MessageBox.Show(resultStr, "Disk Conversion", MessageBoxButtons.OK);
                BtnFolder_initA();
        }

        //******************************* Process File IMD ********************************
        private void ProcessFileImd(string DiskfileName) // for .IMD disks
        {
            var getCpmFile = new CPMFile(); // create instance of CPMFile, then call function
            //var fileNameList = new List<CPMFile.DirList>();

            int diskUsed = 0, diskTotal = 0;
            var result = getCpmFile.ReadImdDir(DiskfileName, ref diskTotal);
            var diskFileCnt = 0;

            if (getCpmFile.fileNameList.Count > 0)
            {
                diskFileCnt = 0;
                diskUsed = 0;
                listBoxFiles.Items.Add("CP/M DISK IMAGE");
                listBoxFiles.Items.Add("=== ======== === ==== =========");
                listBoxFiles.Items.Add("Usr   FILE   EXT SIZE   FLAGS  ");
                listBoxFiles.Items.Add("=== ======== === ==== =========");
                foreach (var f in getCpmFile.fileNameList)
                {
                    diskFileCnt++;
                    diskUsed += f.fsize;
                    var disk_file_entry = new DiskFileEntry();
                    disk_file_entry.DiskImageName = DiskfileName;
                    disk_file_entry.FileName = f.fname;
                    disk_file_entry.UserArea = f.userArea;
                    disk_file_entry.ListBox2Entry = listBoxFiles.Items.Count;
                    disk_file_entry.fFlags = f.flags;
                    disk_file_entry.fileType = "CPM";
                    DiskFileList.Add(disk_file_entry);
                    double tempSize = f.fsize;
                    var tempfname = f.fname.Substring(2, 11);
                    listBoxFiles.Items.Add(string.Format("{0,4:G} {1} {2,4:F1} {3}", f.userArea,tempfname.Insert(8," "), tempSize / 1024, f.flags));
                }
                listBoxFiles.Items.Add("=== ======== === ==== =========");
                listBoxFiles.Items.Add(string.Format("Files {0}, Total {1,3:N0} K, Free {2,5:N0} K, Disk Size {3,5:N0} k", diskFileCnt,
                    diskUsed / 1024, diskTotal - (diskUsed / 1024), diskTotal));
                listBoxFiles.Items.Add("");
            }

            if (result == 0)
            {
                listBoxFiles.Items.Add("  Read failed, try converting to IMG file");
            }

            TotalSize += (int)diskUsed;
            FileCount += diskFileCnt;
        }
        //******************************* Process File H37 ********************************

        private void ProcessFileH37(string diskName) // for .H37 & H8D disks
        {
            var getCpmFile = new CPMFile(); // create instance of CPMFile, then call function
            var encoding = new UTF8Encoding();
            int diskUsed = 0, diskTotal = 0;
            if(diskName.EndsWith(".IMD"))
                getCpmFile.ReadImdDir(diskName, ref diskTotal);
            else
                getCpmFile.ReadCpmDir(diskName, ref diskTotal);
            var diskFileCnt = 0;

            if (getCpmFile.fileNameList.Count > 0)
            {
                diskFileCnt = 0;
                diskUsed = 0;
                listBoxFiles.Items.Add("CP/M DISK IMAGE");
                listBoxFiles.Items.Add("=== ======== === ==== =========");
                listBoxFiles.Items.Add("Usr   FILE   EXT SIZE   FLAGS  ");
                listBoxFiles.Items.Add("=== ======== === ==== =========");
                foreach (var f in getCpmFile.fileNameList)
                {
                    diskFileCnt++;
                    diskUsed += f.fsize;
                    var disk_file_entry = new DiskFileEntry();
                    disk_file_entry.DiskImageName = diskName;
                    disk_file_entry.FileName = f.fname;
                    disk_file_entry.UserArea = f.userArea;
                    disk_file_entry.ListBox2Entry = listBoxFiles.Items.Count;
                    disk_file_entry.fFlags = f.flags;
                    disk_file_entry.fileType = "CPM";
                    DiskFileList.Add(disk_file_entry);
                    var tempStr = disk_file_entry.FileName.Substring(2, 11);
                    tempStr = tempStr.Insert(8, " ");
                    //tempStr = tempStr.Substring(1);
                    // mask high bit in second character
                    /* temp try code
                     * byte[] ttstr = encoding.GetBytes(tempStr) ;
                  
                    ttstr[1] = (byte) (ttstr[1] & 0x7f);
                    tempStr = encoding.GetString(ttstr);
                    tempStr = disk_file_entry.FileName.Insert(8, " ");
                    */
                    double tempSize = (double) f.fsize/1024;
                    listBoxFiles.Items.Add(string.Format("{0,4:G} {1} {2,4:F1} {3}", f.userArea, tempStr, tempSize , f.flags));
                }
            }

            listBoxFiles.Items.Add("=== ======== === ==== =========");
            listBoxFiles.Items.Add(string.Format("Files {0}, Total {1,3:N0} K, Free {2,5:N0} K, Disk Size {3,5:N0} k",
                diskFileCnt, diskUsed / 1024, (diskTotal - diskUsed) / 1024, diskTotal / 1024));
            listBoxFiles.Items.Add("");
            TotalSize += (int)diskUsed;
            FileCount += diskFileCnt;

        }
        //******************************* Process File HDOS ********************************

        private void ProcessFileHdos(string diskName) // for .H37 & H8D disks
        {
            var getHdosFile = new HDOSFile(); // create instance of HDOSFile, then call function
            var encoding = new UTF8Encoding();
            int diskUsed = 0, diskTotal = 0;

            getHdosFile.ReadHdosDir(diskName, ref diskTotal);
            var diskFileCnt = 0;

            if (getHdosFile.fileNameList.Count > 0)
            {
                diskFileCnt = 0;
                diskUsed = 0;
                listBoxFiles.Items.Add("HDOS DISK IMAGE");
                listBoxFiles.Items.Add(getHdosFile.diskLabel);
                listBoxFiles.Items.Add("=== ======== === ==== ========= ===============");
                listBoxFiles.Items.Add("Usr   FILE   EXT SIZE Date      FLAGS  ");
                listBoxFiles.Items.Add("=== ======== === ==== ========= ===============");
                foreach (var f in getHdosFile.fileNameList)
                {
                    diskFileCnt++;
                    diskUsed += f.fCluster;
                    var disk_file_entry = new DiskFileEntry();
                    disk_file_entry.DiskImageName = diskName;
                    disk_file_entry.FileName = f.fname;
                    disk_file_entry.UserArea = f.userArea;
                    disk_file_entry.ListBox2Entry = listBoxFiles.Items.Count;
                    disk_file_entry.fFlags = f.flags;
                    disk_file_entry.fDate = f.createDate;
                    disk_file_entry.fileType = "HDOS";
                    DiskFileList.Add(disk_file_entry);
                    int tt = 0;
                    var tempStr = disk_file_entry.FileName.Substring(0, 11);
                    tempStr = tempStr.Insert(8, " ");
                    int tempSize = f.fsize ;
                    var tempDate = f.createDate ;

                    listBoxFiles.Items.Add(string.Format("{0,4:G} {1,12:G} {2,4:G} {3,9:G} {4}",
                        f.userArea, tempStr, tempSize, f.createDate, f.flags));
                }
            }

            listBoxFiles.Items.Add("=== ======== === ==== ========= ===============");
            listBoxFiles.Items.Add(string.Format("Files {0}, Total {1,4:G}, Free {2,5:G}, Disk Size {3,5:G}",
                diskFileCnt, diskUsed , (diskTotal - diskUsed) , diskTotal ));
            listBoxFiles.Items.Add("");
            TotalSize += (int)diskUsed*256;
            FileCount += diskFileCnt;

        }

        //******************************* Process File MS-DOS Files for Z-100 ********************************

        private void ProcessFileDOS(string diskName) // for Z100 MS-DOS disks
        {
            if (!diskName.EndsWith(".IMG"))
            {
                MessageBox.Show("Only DOS IMG files are supported", "File Type error", MessageBoxButtons.OK);
                return;

            }
            var getDosFile = new MsdosFile(); // create instance of Msdosfile, then call function
            int diskUsed = 0, diskTotal = 0;
            getDosFile.ReadMsdosDir(diskName, ref diskTotal);
            var diskFileCnt = 0;
            var subDir = false;
            string subFname = "";

            if (getDosFile.fileNameList.Count > 0)
            {
                diskFileCnt = 0;
                diskUsed = 0;
                listBoxFiles.Items.Add("MS-DOS DISK IMAGE");
                listBoxFiles.Items.Add("========  === ==== ========= ========== =======");
                listBoxFiles.Items.Add("  FILE    EXT SIZE   FLAGS   Date       Time");
                listBoxFiles.Items.Add("========  === ==== ========= ========== =======");
                foreach (var f in getDosFile.fileNameList)
                {
                    diskFileCnt++;
                    diskUsed += f.fsize;
                    var disk_file_entry = new DiskFileEntry();
                    disk_file_entry.DiskImageName = diskName;
                    disk_file_entry.FileName = f.fname;
                    disk_file_entry.ListBox2Entry = listBoxFiles.Items.Count;
                    disk_file_entry.fDate = getDosFile.DosDateStr(f.fdate);
                    disk_file_entry.fTime = getDosFile.DosTimeStr( f.ftime);
                    disk_file_entry.fFlags = f.flags;
                    disk_file_entry.fileType = "DOS";
                    DiskFileList.Add(disk_file_entry);

                    var tempStr = f.fname;
                    if (f.isSubDir)
                    {
                        subDir = true;
                        subFname = f.fname;
                    }

                    if (subDir && f.fname != subFname)          // check if file is from previous sub directory and remove subdir name from string
                    {
                        if (f.fname.Contains(subFname))
                        {
                            var pos  = tempStr.IndexOf("\0",0);
                            tempStr = " " + tempStr.Substring(pos+1).Insert(8, " ");

                        }
                        else           // end of files in subdirectory, clear flags
                        {
                            subDir = false;
                            subFname = "";
                        }
                    }
                    if(f.fsize > 0 && !subDir)
                        tempStr = tempStr.Insert(8, "  ");

                    listBoxFiles.Items.Add(string.Format("{0} {1,4} {2,9} {3} {4}", tempStr.PadRight(13,' '), f.fsize / 1024, f.flags, disk_file_entry.fDate, disk_file_entry.fTime));
                }
            }

            listBoxFiles.Items.Add("========  === ==== ========= ========== =======");
            listBoxFiles.Items.Add(string.Format("Files {0}, Total {1,3:N0} K, Free {2,5:N0} K, Disk Size {3,5:N0} k",
                diskFileCnt, diskUsed / 1024, (diskTotal - diskUsed) / 1024, diskTotal / 1024));
            listBoxFiles.Items.Add("");
            TotalSize += (int)diskUsed;
            FileCount += diskFileCnt;
        }




        //*********************** Btn View Click *********************************

            private void BtnView_Click(object sender, EventArgs e)
        {
            //  view file
            var idx = listBoxFiles.SelectedIndex;
            if (idx != -1)
                foreach (DiskFileEntry entry in DiskFileList)
                    if (entry.ListBox2Entry == idx)
                    {
                        ViewFile(entry.DiskImageName, entry);
                        return;
                    }
        }
    //************************ View File ******************************************
        private void ViewFile(string disk_image_file, DiskFileEntry disk_file_entry)
        {
            //  view the selected file
            if (FileViewerBorder.Visible)
            {
                FileViewerBox.Clear();

                FileViewerBorder.Visible = false;

                listBoxFiles.Enabled = true;
                listBoxImages.Enabled = true;
                BtnView.Enabled = true;

                if (bImageList) BtnfileList.Enabled = true;
                BtnFolder.Enabled = true;
                return;
            }

          

                // dcp Add CPM File view for H37, IMG, and H8D disks
                //var fileType = (disk_image_file.ToUpper().Contains(".H37") || disk_image_file.ToUpper().Contains(".H8D")
                //    ||  disk_image_file.ToUpper().Contains(".IMG") )&&( !disk_image_file.ToUpper().Contains(".DOS.") 
                //                                                        && !disk_image_file.ToUpper().EndsWith(".IMD"));

                if (disk_file_entry.fileType == "CPM")
                {
                    FileViewerBorder.Visible = true;

                    var diskTotal = 0;
                    var getCpmFile = new CPMFile(); // create instance of CPMFile, then call function
                    getCpmFile.ReadCpmDir(disk_image_file, ref diskTotal);
                    var buff = getCpmFile.ViewFileCPM(disk_file_entry.FileName);
                    var encoding = new UTF8Encoding();
                    var t = encoding.GetString(buff);
                    var newTitleStr = "File Viewer: " + disk_file_entry.FileName.ToString()+" Length: " + buff.Length.ToString();
                    FileViewerBorder.Text = newTitleStr;
                    FileViewerBox.AppendText(t);
                    FileViewerBorder.BringToFront();
                    FileViewerBox.BringToFront();

                }
                else if (disk_file_entry.fileType == "HDOS")
                {
                    FileViewerBorder.Visible = true;

                    var diskTotal = 0;
                    var getHdosFile = new HDOSFile();
                    getHdosFile.ReadHdosDir(disk_image_file, ref diskTotal);
                    var buff = getHdosFile.ViewFileHDOS(disk_file_entry.FileName);
                    var encoding = new UTF8Encoding();
                    var t = encoding.GetString(buff);
                    var newTitleStr = "File Viewer: " + disk_file_entry.FileName.ToString() + " Length: " + buff.Length.ToString();
                    FileViewerBorder.Text = newTitleStr;
                    FileViewerBox.AppendText(t);
                    FileViewerBorder.BringToFront();
                    FileViewerBox.BringToFront();
                }
                else
                    MessageBox.Show("View not supported for this disk image type", "View Error", MessageBoxButtons.OK);

  
        }

        private void filebutton1_Click(object sender, EventArgs e)
        {
            BtnView_Click(sender, e);
        }



        private int ExtractHdosFile(DiskFileEntry disk_file_entry)
        {
            var result = 1; // dcp extracted file count to deal with file extract fail
         
            var getHdosFile = new HDOSFile(); // create instance of CPMFile, then call function
            result = getHdosFile.ExtractFileHDOS(disk_file_entry);


            return result;
        }

        private int DeleteHdosFile(DiskFileEntry disk_file_entry)
        {
            var result = 0; // dcp extracted file count to deal with file extract fail

            var getHdosFile = new HDOSFile(); // create instance of HDOSFile, then call function
            result = getHdosFile.DeleteFileHDOS(disk_file_entry);


            return result;
        }
        private int ExtractCpmFile(DiskFileEntry disk_file_entry)
        {
            var result = 0; // dcp extracted file count to deal with CP/M file extract fail

                // dcp Add CPM Extract
                var getCpmFile = new CPMFile(); // create instance of CPMFile, then call function
                result = getCpmFile.ExtractFileCPM(disk_file_entry);


            return result;
        }
        private int ExtractDosFile(DiskFileEntry disk_file_entry)
        {
            var result = 0; // dcp extracted file count to deal with file extract fail

            if (!disk_file_entry.fFlags.Contains("Directory"))
            {
                var getDosFile = new MsdosFile(); // create instance of CPMFile, then call function
                result = getDosFile.ExtractFileMsdos(disk_file_entry);
            }

            return result;
        }

        //************************* Extract a file *******************
        // process a file list from form 1
        private void btnExtract_Click(object sender, EventArgs e)
        {
            //  extract file
            var files_extracted = 0;
            var getCpmFile = new CPMFile(); // create instance of CPMFile, then call function
            var diskTotal = 0;


            var idx = listBoxFiles.SelectedIndex;
            if (idx != -1)
            {
                for (var i = 0; i < listBoxFiles.SelectedItems.Count; i++)
                {
                    idx = listBoxFiles.SelectedIndices[i];
                    foreach (DiskFileEntry entry in DiskFileList)
                        if (entry.ListBox2Entry == idx)
                        {
                            if (entry.DiskImageName.Contains(".IMD"))
                                //var fileNameList = getCpmFile.ReadImdDir(entry.DiskImageName, ref diskTotal);
                                files_extracted += getCpmFile.ExtractFileCPMImd(entry);
                            else if (entry.DiskImageName.Contains(".DOS"))
                                files_extracted += ExtractDosFile(entry);
                            else if(entry.fileType == "CPM")
                                 files_extracted += ExtractCpmFile(entry);
                            else if(entry.fileType == "HDOS")
                                files_extracted += ExtractHdosFile(entry);
                            //break;
                        }
                }

                listBoxFiles.ClearSelected();
            }
            else
            {
                if (MessageBox.Show(
                        string.Format("There are a total of {0} files. Extract all files?", DiskFileList.Count),
                        "EXTRACT FILES", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    foreach (DiskFileEntry entry in DiskFileList)
                    {
                        // dcp changed Extract file to return 1 if successful
                        if (entry.DiskImageName.Contains(".IMD"))
                            //var fileNameList = getCpmFile.ReadImdDir(entry.DiskImageName, ref diskTotal);
                            files_extracted += getCpmFile.ExtractFileCPMImd(entry);
                        else if (entry.DiskImageName.Contains(".DOS"))
                            files_extracted += ExtractDosFile(entry);
                        else if (entry.fileType == "CPM")
                            files_extracted += ExtractCpmFile(entry);
                        else if (entry.fileType == "HDOS")
                            files_extracted += ExtractHdosFile(entry);
                       // break; ;
                    }
            }

            if (files_extracted > 0)
            {
                var message = string.Format("{0} file(s) extracted", files_extracted);
                MessageBox.Show(this, message, "Disk Image Utility");
            }
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void listBoxFiles_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        //*********************** H8D to IMG conversion *************************
        public void BtnH8dConvert_Click(object sender, EventArgs e)
        {
            if (listBoxImages.SelectedIndex != -1)
            {
                var totalConvert = 0;
                foreach (var lb in listBoxImages.SelectedItems)
                {
                    if (lb.ToString().ToUpper().EndsWith(".H8D"))
                    {
                        // read entire file into memory
                        var diskFileName = labelFolder.Text + "\\" + lb.ToString();
                        var file = File.OpenRead(diskFileName); // read entire file into an array of byte
                        var fileByte = new BinaryReader(file);
                        var fileLen = (int) file.Length;
                        var buf = new byte[fileLen];
                        var wbuf = new byte[fileLen];
                        int wBufPtr = 0, rBufPtr = 0, firstSector = 0;

                        try
                        {
                            if (fileByte.Read(buf, 0, fileLen) != fileLen)
                                MessageBox.Show("File Read Error", "Error", MessageBoxButtons.OK);

                        }
                        catch
                        {
                            MessageBox.Show("File Access Error", "Error", MessageBoxButtons.OK);
                            return;
                        }

                        fileByte.Close();
                        file.Close();
                        var hdosFile = IsHDOSDisk(ref buf);

                        // create output file

                        diskFileName = diskFileName.Replace(".H8D", ".IMG");

                        if (File.Exists(diskFileName))
                            if (MessageBox.Show("File exists, Overwrite it?", "File Exists",
                                    MessageBoxButtons.YesNo) ==
                                DialogResult.No)
                                return;
                        FileStream fileOut;

                        try
                        {
                            fileOut = File.Create(diskFileName);
                        }
                        catch
                        {
                            MessageBox.Show("File Access Error", "Error", MessageBoxButtons.OK);
                            return;
                        }

                        var binOut = new BinaryWriter(fileOut);
                        if (hdosFile) // convert H8D to IMG
                        {
                            // byte for byte copy, no interleave adjustment
                            for (var i = 0; i < fileLen; i++)
                                wbuf[wBufPtr++] = buf[rBufPtr++];
                        }
                        else
                        {

                            var getCpm = new CPMFile();
                            var intLv = getCpm.DiskType[getCpm.DiskType.GetLength(0)-1, 5];
                            var spt = getCpm.DiskType[getCpm.DiskType.GetLength(0)-1, 6];
                            var sectSize = getCpm.DiskType[getCpm.DiskType.GetLength(0)-1, 7];
                            var skew = getCpm.BuildSkew(intLv, spt, getCpm.DiskType[getCpm.DiskType.GetLength(0)-1, 10]);
                            int sector = 0;
                            buf[5] = 0x60;      // Set disk type marker for SSDD

                            while (rBufPtr < fileLen)
                            {
                                if (sector < spt)
                                {
                                    var offset = skew[sector] * sectSize; // get the skew offset 
                                    for (var k = 0;
                                        k < sectSize;
                                        k++) // write a sector in numerical order for the IMG file
                                    {
                                        wbuf[wBufPtr++] = buf[offset + rBufPtr + k];
                                    }

                                    sector++;
                                }
                                else
                                {
                                    sector = 0;
                                    rBufPtr += spt * sectSize;
                                }

                            }

                        }

                        binOut.Write(wbuf, 0, wBufPtr);
                        binOut.Close();
                        fileOut.Dispose();
                    }
                    totalConvert++;
                }

                var fileStr = totalConvert.ToString() + " Files Converted";
                MessageBox.Show(fileStr, "H8D Conversions", MessageBoxButtons.OK);
                BtnFolder_initA();
            }
        }

        /************************ Is HDOS Disk ***************************************/
        // input: disk image buffer
        // output: true if HDOS disk
        // code from Les Bird H8DUtility
    static public bool IsHDOSDisk(ref byte[] track_buffer)
        {
            if ((track_buffer[0] == 0xAF && track_buffer[1] == 0xD3 && track_buffer[2] == 0x7D && track_buffer[3] == 0xCD) ||   //  V1.x
                (track_buffer[0] == 0xC3 && track_buffer[1] == 0xA0 && track_buffer[2] == 0x22 && track_buffer[3] == 0x20) ||   //  V2.x
                (track_buffer[0] == 0xC3 && track_buffer[1] == 0xA0 && track_buffer[2] == 0x22 && track_buffer[3] == 0x30) ||   //  V3.x
                (track_buffer[0] == 0xC3 && track_buffer[1] == 0x1D && track_buffer[2] == 0x24 && track_buffer[3] == 0x20))     //  V? Super-89
            {
                return (true);
            }
            return (false);
        }


    }

}
