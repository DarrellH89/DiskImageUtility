using System;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using CPM;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using HDOS;
using MSDOS;


namespace DiskUtility
    {
    public partial class Form5 : Form
        {
        public GroupBox FileViewerBorder;
        public RichTextBox FileViewerBox;
        public int FileCount = 0;
        private char start = 'C';

        public Form5(char search)
            {
            InitializeComponent();
            CenterToParent();
            start = search;
            }

        private FolderBrowserDialog folderBrowserDialog1;

        /********** File buffer variables *********/
        // private const int bufferSize = 800 * 1024;
        //private byte[] buf = new byte[bufferSize];

        private void Form5_Load(object sender, EventArgs e)
        {
            tbFolder.Text = Form1.FolderStr;
            FileCount = 0;
            buttonFolder_Init(start);
            }

        private void buttonFolder_Init(char fileStart) // dcp modified code to read files store in last used directory. initA is used both on startup and when Folder Button is clicked.
            {
            listBox1.Items.Clear(); // clear file list
            DisableButtons(fileStart);
            // set file extension types to scan directory
            string[] file_list = new string[1];
            if(fileStart == 'C' || fileStart == 'H')                    // CP/M
                try
                {
                    string[] h8d_list = Directory.GetFiles(tbFolder.Text, "*.h8d");
                    string[] h37_list = Directory.GetFiles(tbFolder.Text, "*.h37");
                    string[] img_list = (string[])Directory.GetFiles(tbFolder.Text, "*.img"); //.Where(name => !name.EndsWith(".DOS.IMG"));
                    file_list = new string[h8d_list.Length + h37_list.Length + img_list.Length]; // combine filename lists
                    Array.Copy(h8d_list, file_list, h8d_list.Length);
                    Array.Copy(h37_list, 0, file_list, h8d_list.Length, h37_list.Length);
                    Array.Copy(img_list, 0, file_list, h8d_list.Length+h37_list.Length, img_list.Length);
                    if(fileStart == 'C')
                        this.Text = "Add Files to a CP/M Image";
                    else
                    {
                        this.Text = "Add Files to a HDOS Image";
                    }
                }
                catch
                {
                    // Directory not found, clear string
                    file_list[0] = "";
                    tbFolder.Text = "";
                }
            if (fileStart == 'D')           // DOS
                try
                {
                    string[] img_list = (string[])Directory.GetFiles(tbFolder.Text, "*.DOS.img"); //.Where(name => !name.EndsWith(".DOS.IMG"));
                    file_list = new string[img_list.Length];
                    Array.Copy(img_list,file_list, img_list.Length);
                    this.Text = "Add Files to a DOS Image";
                }
                catch
                {
                    // Directory not found, clear string
                    file_list[0] = "";
                    tbFolder.Text = "";
                }

            if (file_list.Length == 0)
                listBox1.Items.Add("No image files found");
            else
            {
                foreach (string files in file_list) // add file names to listbox1
                {
                    string file_name;
                    file_name = files.Substring(files.LastIndexOf("\\") + 1).ToUpper();
                    listBox1.Items.Add(file_name);
                    string file_count = string.Format("{0} disk images", listBox1.Items.Count.ToString());
                    //label4.Text = file_count;
                }
            }
            }

        public void unload()
            {

            }
        //****************** Button Click procedures **************************
        private void ButtonFolder_Click(object sender, EventArgs e)
            {
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog1.SelectedPath = tbFolder.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                tbFolder.Text = folderBrowserDialog1.SelectedPath;
                buttonFolder_Init('C');
                }

            }

        private void DisableButtons(char dos)
        {
            switch (dos)
            {
                case 'C':
                    btnDOS_360.Enabled = false;
                    btnDOS_720.Enabled = false;
                    btnDOS_1440.Enabled = false;
                    btnH80_640.Enabled = false;
                    btnH40_400.Enabled = false;
                    btnH40_100.Enabled = false;
                    break;
                case 'H':
                    btnH8d100.Enabled = false;
                    btnH37_800.Enabled= false;
                    btnH37_640.Enabled = false;
                    btnH37_400.Enabled= false;
                    btnH37_320.Enabled= false;
                    btnH37_100.Enabled= false;
                    btnSz80_1440.Enabled= false;
                    btnZ100_320.Enabled= false;
                    btnDOS_360.Enabled = false;
                    btnDOS_720.Enabled= false;
                    btnDOS_1440.Enabled = false;
                    break;
                case 'D':
                    btnH8d100.Enabled = false;
                    btnH37_800.Enabled = false;
                    btnH37_640.Enabled = false;
                    btnH37_400.Enabled = false;
                    btnH37_320.Enabled = false;
                    btnH37_100.Enabled = false;
                    btnSz80_1440.Enabled = false;
                    btnZ100_320.Enabled = false;
                    btnH80_640.Enabled = false;
                    btnH40_400.Enabled = false;
                    btnH40_100.Enabled = false;
                    break;
            }
        }
        private void ButtonH8d100_Click(object sender, EventArgs e)
            {
            fileCreate(9, "");
            }

        private void Buttonh37_806f_Click(object sender, EventArgs e)
            {
            fileCreate(2, "");
            }

        private void ButtonH37_806b_Click(object sender, EventArgs e)
            {
            fileCreate(3, "");
            }
        private void ButtonH37_4067_Click(object sender, EventArgs e)
        {
            fileCreate(4, "");
        }
        private void ButtonH37_4060_Click(object sender, EventArgs e)
        {
            fileCreate(8, "");
        }
        private void ButtonH37_4063_Click(object sender, EventArgs e)
        {
            fileCreate(7, "");
        }
        private void ButtonZ100_cpm_Click(object sender, EventArgs e)
        {
            fileCreate(5, "");
        }

        private void smallz80_Click(object sender, EventArgs e)
        {

            fileCreate(0, "");
        }
        private void ButtonDos_320_Click(object sender, EventArgs e)
        {
            fileCreateDos(320, "");
        }
        private void ButtonDos_360_Click(object sender, EventArgs e)
        {
            fileCreateDos(360,"");
        }

        private void ButtonDos_720_Click(object sender, EventArgs e)
        {
            fileCreateDos(720, "");
        }
        private void ButtonDos_1440_Click(object sender, EventArgs e)
        {
            fileCreateDos(1440, "");
        }
        private void ButtonHDOS_100_Click(object sender, EventArgs e)
        {
            fileCreateHdos(1, "");
        }
        private void ButtonHDOS_320_Click(object sender, EventArgs e)
        {
            fileCreateHdos(2, "");
        }
        private void ButtonHDOS_640_Click(object sender, EventArgs e)
        {
            fileCreateHdos(3, "");
        }

        private void btnRC2014_720_Click(object sender, EventArgs e)
        {

        }
        /******************* List Box 1 Double Click ********************/
        /* opens selected disk image file to add files */

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            string path = "";
            //foreach(var lb in listBox1.SelectedItems)
            var lb = listBox1.SelectedItem;
            {
                path = tbFolder.Text + "\\" + lb.ToString();
                if (path.Contains(".DOS.IMG"))
                    fileCreateDos(0, path);
                else
                {

                    // HDOS Check
                    byte[] tempBuf = new byte[512];
                    var file = File.OpenRead(path); // read entire file into an array of byte
                    var fileByte = new BinaryReader(file);
                    fileByte.Read(tempBuf, 0, 256);
                    file.Close();
                    file.Dispose();
                    if(Form1.IsHDOSDisk(ref tempBuf))
                        fileCreateHdos(0,path);
                    else
                        fileCreate(0, path);
                }
            }
            buttonFolder_Init(start);
        }
        /******************** File Create File for HDOS *******************************/
        /* Input disk type - really the index to the to the HDOS file type 
        // Disk Types
        // 0 - Open existing file
        // 1 - 100k, 40T, 1 side,10 SPT, 256 byte sector, 2 sectors per Group, 400 sectors
        // 2 - 400k, 80T, 1 side, 16 SPT, 256 byte sector, 6 sectors per Group, 1278 sector
        // 3 - 640k, 80T, 2 sides, 16 SPT, 256 byte sector, 10 sectors per Group, 2550 sector
         */
        private void fileCreateHdos(int diskType, string fileName)
        {
            var getHdos = new HDOSFile();
            string path = fileName;
            int diskTotalBytes = 0;

            OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();

            if (path.Length == 0) // Create disk image if no path provided
            {
                openFileDialog1.InitialDirectory = tbFolder.Text;
                openFileDialog1.Filter = "HDOS Files (*.IMG)|*.IMG";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;
                openFileDialog1.CheckFileExists = false;
                openFileDialog1.ShowDialog();
                path = openFileDialog1.FileName;
            }

            if (path.Length == 0) // no file selected, nothing to do
                return;
            var result = File.Exists(path);    // test path provided or selected
            //Console.WriteLine("Result: {0:G}", result);
            if (!result) // create blank file image
            {
                diskTotalBytes = 0;
                switch (diskType)
                {
                    case 1:
                        diskTotalBytes = 102400;
                        break;
                    case 2:
                        diskTotalBytes = 327680;
                        break;
                    case 3:
                        diskTotalBytes = 655360;
                        break;
                    default:
                        break;
                }

                if (!getHdos.InitHdosDisk(diskTotalBytes, path))
                    MessageBox.Show("HDOS File Creation Error", "HDOS Create", MessageBoxButtons.OK);
            }
            /* Use ReadHdosDir to read disk into HDOSFiles buffer. After the file is in the buffer, add
             * each new file. Write HDOSFiles buffer to disk if any files are successfully added
            */
            getHdos.ReadHdosDir(path, ref diskTotalBytes);




            // Get Files to add to image
            var startDir = Form1.addFilesLoc  ;   // get last used folder working folder is selected
            var fileCnt = 0;
            var filesSkipped = 0;
            var filesFull = 0;

            if (startDir == "") startDir = "c:\\";
            openFileDialog1.InitialDirectory = startDir;
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;
            openFileDialog1.Title = "Select Files to Add to Image";
            openFileDialog1.FileName = "";
            string temp = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var results = Globals.Results.Success;
                Form1.addFilesLoc = Path.GetDirectoryName(openFileDialog1.FileName);
                foreach (String filename in openFileDialog1.FileNames)
                {
                    if (results != Globals.Results.Full)
                    {
                        results = getHdos.InsertFileHdos(filename);
                        if ( results == Globals.Results.Success)
                            fileCnt++;
                        else
                            filesSkipped++;
                    }
                    else
                    {
                        filesFull++;
                    }
                }
            }
            if (fileCnt > 0) // Added a file or two
            {
                FileStream fsOut = new FileStream(path, FileMode.Open, FileAccess.Write);
                BinaryWriter fileOutBytes = new BinaryWriter(fsOut);

                fileOutBytes.Seek(0, SeekOrigin.Current);
                fileOutBytes.Write(getHdos.buf, 0, getHdos.fileLen);
                fileOutBytes.Close();
                fsOut.Dispose();
            }
            buttonFolder_Init(start);
            var message = string.Format("{0} file(s) Added, {1} file(s) skipped, {2} files(s) not added due to full disk", fileCnt, filesSkipped, filesFull);
            MessageBox.Show(this, message, "Insert HDOS Files");
        }

        /******************** File Create File for CP/M *******************************/
            /* Input disk type - really the index to the to the CP/M file type data array

             */
            private void fileCreate(int diskType, string fileName)
            {
            var getCpm = new CPMFile(); // create instance of CPMFile, then call function
            // bool fileNew = false;
            string path = fileName;
            OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();

            if (path.Length == 0) // Create disk image if no path provided
                {
                openFileDialog1.InitialDirectory = tbFolder.Text;
                if (diskType > 7)
                    openFileDialog1.Filter = "H8D Files (*.H8D)|*.H8D";
                else
                    openFileDialog1.Filter = "CP/M files (*.IMG)|*.IMG";
           
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;
                openFileDialog1.CheckFileExists = false;
                openFileDialog1.ShowDialog();
                path = openFileDialog1.FileName;
                }
            if (path.Length == 0)    // no file selected, nothing to do
                return;
            //Console.Write("Path: ");
            //Console.WriteLine(path);

            //Console.WriteLine(File.Exists(path));
            var result = File.Exists(path);
            //Console.WriteLine("Result: {0:G}", result);
            int diskTotalBytes = 0;
            if (!result)            // create blank file image
                {
                //Console.WriteLine("File DOES Not Exist");
                //Console.WriteLine("But it was Created: {0:G}", File.Exists(path));
                // calculate buffer size for disk image
                diskTotalBytes = getCpm.DiskType[diskType, 6] * getCpm.DiskType[diskType, 7] *
                                 getCpm.DiskType[diskType, 8] * getCpm.DiskType[diskType, 9];
                var cpmBuf = new byte[diskTotalBytes];
                for (var i = 0; i < diskTotalBytes; i++)
                    cpmBuf[i] = 0xE5;
                cpmBuf[getCpm.H37disktype] = (byte)getCpm.DiskType[diskType, 0]; // set disk type marker

                switch (cpmBuf[getCpm.H37disktype])

                    {
                    case 0x6f:
                        byte[] diskMark1 = { 0, 0x6f, 0xe5, 8, 0x10, 0xe5, 0xe5, 1, 0xe5, 0x28, 0, 4, 0xf, 0, 0x8a, 0x01, 0xff, 0, 0xf0, 0, 0x40, 0, 2, 0, 0xec };
                        for (var i = 0; i < diskMark1.Length; i++)
                            cpmBuf[getCpm.H37disktype - 1 + i] = diskMark1[i];
                        break;
                    case 0x6b:
                        byte[] diskMark2 = { 0, 0x6b, 0xe5, 2, 0x10, 0xe5, 0xe5, 1, 0xe5, 0x20, 0, 4, 0xf, 0, 0x3b, 0x01, 0xff, 0, 0xf0, 0, 0x40, 0, 2, 0, 0x4d };
                        for (var i = 0; i < diskMark2.Length; i++)
                            cpmBuf[getCpm.H37disktype - 1 + i] = diskMark2[i];

                        break;
                    case 0x23:
                        byte[] diskMark3 = { 0, 0x23, 0, 4, 0x10, 0x27, 0x80, 0, 0x20, 0x20, 0, 4, 0xf, 1, 0x9b, 0x00, 0xff, 0, 0xf0, 0, 0x40, 0, 2, 0, 1 };
                        for (var i = 0; i < diskMark3.Length; i++)
                            cpmBuf[getCpm.H37disktype - 1 + i] = diskMark3[i];

                        break;
                    case 0x63:
                        byte[] diskMark4 = { 0, 0x63, 0xe5, 2, 0x10, 0xe5, 0xe5, 0, 0xe5, 0x20, 0, 4, 0xf, 0, 0x9b, 0x00, 0xff, 0, 0xf0, 0, 0x40, 0, 2, 0, 0xf7 };
                        for (var i = 0; i < diskMark4.Length; i++)
                            cpmBuf[getCpm.H37disktype - 1 + i] = diskMark4[i];

                        break;
                    case 0x67:
                        byte[] diskMark5 = { 0, 0x67, 0xe5, 8, 0x10, 0xe5, 0xe5, 0, 0xe5, 0x28, 0, 4, 0xf, 0, 0xc2, 0x00, 0xff, 0, 0xf0, 0, 0x40, 0, 2, 0, 0xbe };
                        for (var i = 0; i < diskMark5.Length; i++)
                            cpmBuf[getCpm.H37disktype - 1 + i] = diskMark5[i];

                        break;
                    default:
                        break;
                    }
                FileStream fso = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                BinaryWriter fileOutByte = new BinaryWriter(fso);

                fileOutByte.Write(cpmBuf, 0, diskTotalBytes);
                fileOutByte.Close();
                fso.Dispose();
                }

  
             /* Use ReadCmDir to read disk into CPMFiles buffer. After the file is in the buffer, add
             * each new file. Write CPMFiles buffer to disk if any files are successfully added
             */
            getCpm.ReadCpmDir(path, ref diskTotalBytes);




            // Get Files to add to image
            var startDir = Form1.addFilesLoc;   // check if a working folder is selected
            var fileCnt = 0;
            var filesSkipped = 0;
            var filesFull = 0;

            if (startDir == "") startDir = "c:\\";
            openFileDialog1.InitialDirectory = startDir;
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;
            openFileDialog1.Title = "Select Files to Add to Image";
            string temp = "";

            var results = Globals.Results.Success;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Form1.addFilesLoc = Path.GetDirectoryName(openFileDialog1.FileName);
                foreach (String filename in openFileDialog1.FileNames)
                    if (results != Globals.Results.Full)
                    {
                        results = getCpm.InsertFileCpm(filename);
                        if (results == Globals.Results.Success)
                            fileCnt++;
                        if (results == Globals.Results.Fail)
                            filesSkipped++;
                    }
                    else
                    {
                        filesFull++;
                    }
            }

            if (fileCnt > 0) // Added a file or two
                {
                FileStream fsOut = new FileStream(path, FileMode.Open, FileAccess.Write);
                BinaryWriter fileOutBytes = new BinaryWriter(fsOut);

                fileOutBytes.Seek(0, SeekOrigin.Current);
                fileOutBytes.Write(getCpm.buf, 0, diskTotalBytes);
                fileOutBytes.Close();
                fsOut.Dispose();
                }
            buttonFolder_Init(start);
            var message = string.Format("{0} file(s) Added, {1} file(s) skipped, {2} files(s) not added due to full disk", fileCnt, filesSkipped, filesFull);
            MessageBox.Show(this, message, "Insert CP/M Files");
        }
        /*
        /******************** File Create File for MS-DOS *******************************/
        /* Input: disk size - used to determine media type in file type data array

         */
        private void fileCreateDos(int diskSize, string fileName)
        {
            var getDos = new MsdosFile(); // create instance of CPMFile, then call function
            // bool fileNew = false;
            string path = fileName;
            OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();

            if (path.Length == 0) // Create disk image if no path provided
            {
                openFileDialog1.InitialDirectory = tbFolder.Text;
                openFileDialog1.Filter = "DOS Files (*.DOS.IMG)|*.DOS.IMG";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;
                openFileDialog1.CheckFileExists = false;
                openFileDialog1.ShowDialog();
                path = openFileDialog1.FileName;
            }

            if (path.Length == 0)    // no file selected, nothing to do
                return;
            //Console.Write("Path: ");
            //Console.WriteLine(path);

            //Console.WriteLine(File.Exists(path));
            var result = File.Exists(path);

            int diskTotalBytes = 0;
            var diskType =0;
            

            if (!result)                // create blank file image
            {
                var ctr = 0;
               var ds = diskSize * 1024;
                for (; ctr < getDos.DiskType.GetLength(0); ctr++)
                    if (ds == getDos.DiskType[ctr, 6]* getDos.DiskType[ctr, 12])
                        break;
                if (ctr == getDos.DiskType.GetLength(0))
                    return;
                diskType = getDos.DiskType[ctr, 0];         // get media descriptor
                
                diskTotalBytes = getDos.DiskType[ctr, 12] * getDos.DiskType[ctr, 6] ;
                var dosBuf = new byte[diskTotalBytes];
                for (var i = 0; i < diskTotalBytes; i++)            // fill image with E5
                    dosBuf[i] = 0xE5;

                        // offset Length   Value
                        //  0B      2       bytes per sector
                        //  0D      1       Sect / cluster
                        //  0E      2       Reserved logical sectors
                        //  10      1       sectors per FAT
                        //  11      2       Max root DIR entries
                        //  13      2       Total Sectors
                        //  15      1       Media Descriptor
                        //  16      2       Logical sectors per FAT
                        //  18      2       Physical Sector per track
                        //  1A      2       # heads
                        //  1C      2       # hidden sectors before partition. 0 for floppy
                switch (diskSize)
                {
                    case 360:
                    {
                        byte[] diskMark1 = { 0, 2, 2, 1, 0,
                                             2,112, 0, 0x60, 9, 0xfd,2, 0, 9, 0, 2, 0};
                        for (var i = 0; i < diskMark1.Length; i++)
                            dosBuf[0xb+i] = diskMark1[i];
                        break;
                    }
                    case 320:
                    {
                        byte[] diskMark1 = {   0, 2, 2, 1, 0,
                                               2, 112, 0, 0x80, 2, 0xff, 2, 0, 8, 0, 2, 0 };
                        //xa, 0, 1, 3, 0, 1, 0x18, 0xb0, 0, 0x3e, 0x37, 0x0f, 0x12, 0x18, 0x5f, 0 };
                        for (var i = 0; i < diskMark1.Length; i++)
                            dosBuf[4 + i] = diskMark1[i];
                            break;
                    }
                    case 1440:
                    {
                        byte[] diskMark1 = { 0, 0x20, 1, 1, 0, 
                                             2, 240, 0, 0x40, 0xb, 0xf0, 9, 0 ,0x12, 0, 2, 0, 0, 0};
                        for (var i = 0; i < diskMark1.Length; i++)
                            dosBuf[0xb + i] = diskMark1[i];
                        break;
                    }
                    case 720:
                    {
                        byte[] diskMark1 = { 0, 2, 2, 1, 0, 
                                             2, 112, 0, 0xa0, 0x5, 0xf9, 3, 0, 9, 0, 2, 0 };
                        for (var i = 0; i < diskMark1.Length; i++)
                            dosBuf[0xb + i] = diskMark1[i];
                        break;
                        }
                    case 1200:
                    {
                        byte[] diskMark1 = { 0, 2, 2, 1, 1,
                                             2, 240, 0, 0x60, 9, 0xf9, 7, 0, 9, 0, 2, 0 };
                        for (var i = 0; i < diskMark1.Length; i++)
                            dosBuf[0xb + i] = diskMark1[i];
                        break;
                    }
                    default:
                        break;
                }
                // create FAT
                var fatStart = getDos.DiskType[ctr, 9];
                var fatSize = getDos.DiskType[ctr, 11] *3/2;
                var fatOffset = getDos.DiskType[ctr, 10] * getDos.DiskType[ctr, 6];
                var t = fatStart;
                dosBuf[t++] = (byte) getDos.DiskType[ctr, 0];
                dosBuf[t++] = 0xff;
                dosBuf[t++] = 0xff;
                for (var i = fatStart+3; i <  fatSize+fatStart; i++)
                    dosBuf[i] = 0;
                            // copy to second FAT
                for (var i = fatStart ; i < fatSize+fatStart; i++)
                    dosBuf[i + fatOffset] = dosBuf[i];

                FileStream fso = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                BinaryWriter fileOutByte = new BinaryWriter(fso);

                fileOutByte.Write(dosBuf, 0, diskTotalBytes);
                fileOutByte.Close();
                fso.Dispose();
            }
            getDos.ReadMsdosDir(path, ref diskTotalBytes);
            // Get Files to add to image
            var startDir = Form1.addFilesLoc;   // check if a working folder is selected
            var fileCnt = 0;
            var filesSkipped = 0;
            var filesFull = 0;

            if (startDir == "") startDir = "c:\\";
            openFileDialog1.InitialDirectory = startDir;
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;
            openFileDialog1.Title = "Select Files to Add to Image";
            string temp = "";
            var results = Globals.Results.Success;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Form1.addFilesLoc = Path.GetDirectoryName( openFileDialog1.FileName);
                foreach (String filename in openFileDialog1.FileNames)
                    if (results != Globals.Results.Full)
                    {
                        results = getDos.InsertFileDos(filename);
                        if (results == Globals.Results.Success)
                            fileCnt++;
                        if(results == Globals.Results.Fail)
                            filesSkipped++;
                    }
                    else
                    {
                        filesFull++;
                    }
            }

            if (fileCnt > 0) // Added a file or two
            {
                FileStream fsOut = new FileStream(path, FileMode.Open, FileAccess.Write);
                BinaryWriter fileOutBytes = new BinaryWriter(fsOut);

                fileOutBytes.Seek(0, SeekOrigin.Current);
                fileOutBytes.Write(getDos.buf, 0, diskTotalBytes);
                fileOutBytes.Close();
                fsOut.Dispose();
            }
            buttonFolder_Init(start);
            var message = string.Format("{0} file(s) Added, {1} file(s) skipped, {2} files(s) not added due to full disk", fileCnt, filesSkipped, filesFull);
            MessageBox.Show(this, message, "Insert MS-DOS Files");
        }







        //private void TestCPM_click(object sender, EventArgs e)
        //{
        //    var cpmTest = new CPMFile();
        //    OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
        //    var path = "TestCPM.H37";
        //    openFileDialog1.InitialDirectory = tbFolder.Text;
        //    openFileDialog1.Filter = "H37 files (*.H37)|*.H37";
        //        openFileDialog1.FilterIndex = 2;
        //        openFileDialog1.RestoreDirectory = true;
        //        openFileDialog1.CheckFileExists = false;
        //        openFileDialog1.ShowDialog();
        //        path = openFileDialog1.FileName;

        //    int diskType = 0x6f;
        //    int diskTotalBytes = cpmTest.DiskType[diskType, 6] * cpmTest.DiskType[diskType, 7] *
        //                     cpmTest.DiskType[diskType, 8];
        //    var cpmBuf = new byte[diskTotalBytes];
        //    for (var i = 0; i < diskTotalBytes; i++)
        //        cpmBuf[i] = 0xE5;
        //    cpmBuf[cpmTest.H37disktype] = (byte)cpmTest.DiskType[diskType, 0]; // set disk type marker

        //    byte[] diskMark1 = { 0, 0x6f, 0xe5, 8, 0x10, 0xe5, 0xe5, 1, 0xe5, 0x28, 0, 4, 0xf, 0, 0x8a, 0x01, 0xff, 0, 0xf0, 0, 0x40, 0, 2, 0, 0xec };
        //            for (var i = 0; i < diskMark1.Length; i++)
        //                cpmBuf[cpmTest.H37disktype - 1 + i] = diskMark1[i];
        //    FileStream fso = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        //    BinaryWriter fileOutByte = new BinaryWriter(fso);

        //    fileOutByte.Write(cpmBuf, 0, diskTotalBytes);
        //    fileOutByte.Close();
        //    fso.Dispose();
        //    cpmTest.ReadCpmDir(path, ref diskTotalBytes);
        //    var startDir =
        //        tbFolder.Text; // openFileDialog1.InitialDirectory; // check if a working folder is selected
        //    var fileCnt = 0;

        //    if (startDir == "") startDir = "c:\\";
        //    openFileDialog1.InitialDirectory = startDir;
        //    openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
        //    openFileDialog1.FilterIndex = 2;
        //    openFileDialog1.RestoreDirectory = true;
        //    openFileDialog1.Multiselect = true;
        //    openFileDialog1.Title = "Select Files to Add to Image";
        //    string temp = "";
        //    if (openFileDialog1.ShowDialog() == DialogResult.OK)
        //        foreach (String filename in openFileDialog1.FileNames)
        //            fileCnt += cpmTest.InsertFileCpm(filename);


        //    FileStream fsOut = new FileStream(path, FileMode.Open, FileAccess.Write);
        //    BinaryWriter fileOutBytes = new BinaryWriter(fsOut);

        //    fileOutBytes.Seek(0, SeekOrigin.Current);
        //    fileOutBytes.Write(cpmTest.buf, 0, diskTotalBytes);
        //    fileOutBytes.Close();
        //    fsOut.Dispose();
        //    }
        }
}
