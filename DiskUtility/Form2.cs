using System;
using MSDOS;
using CPM;
using DiskUtility;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MSDOSAdd
{
    public partial class Form2 : Form
    {
        public GroupBox FileViewerBorder;
        public RichTextBox FileViewerBox;
        public int FileCount = 0;
        public Form2()
        {
            InitializeComponent();
            CenterToParent();
        }
        private FolderBrowserDialog folderBrowserDialog1;

        /********** File buffer variables *********/
        // private const int bufferSize = 800 * 1024;
        //private byte[] buf = new byte[bufferSize];

        private void Form2_Load(object sender, EventArgs e)
        {
            tbFolder.Text = Form1.FolderStr;
            FileCount = 0;
            buttonFolder_Init();
        }

        private void
            buttonFolder_Init() // dcp modified code to read files store in last used directory. initA is used both on startup and when Folder Button is clicked.
        {
            listBox1.Items.Clear(); // clear file list
            // set file extension types to scan directory
            string[] file_list = new string[1];
            try
            {
                string[] z100_list = Directory.GetFiles(tbFolder.Text, "*.z100.*");
                string[] msdos_list = Directory.GetFiles(tbFolder.Text, "*.dos.*");
                file_list = new string[z100_list.Length + msdos_list.Length]; // combine filename lists
                Array.Copy(z100_list, file_list, z100_list.Length);
                Array.Copy(msdos_list, 0,file_list, z100_list.Length,msdos_list.Length);
            }
            catch
            {
                // Directory not found, clear string
                file_list = null;
                tbFolder.Text = "";
            }


            if (file_list.Length == 0)
            {
                listBox1.Items.Add("No image files found");

            }
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

        private void ButtonFolder_Click(object sender, EventArgs e)
        {
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog1.SelectedPath = tbFolder.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                tbFolder.Text = folderBrowserDialog1.SelectedPath;
                buttonFolder_Init();
            }

        }

        private void btnDos_320_Click(object sender, EventArgs e)
        {
            fileCreate(1, "");
        }

        private void btnDos360_Click(object sender, EventArgs e)
        {
            fileCreate(0, "");
        }

        private void btnDos640_Click(object sender, EventArgs e)
        {
            fileCreate(2, "");
        }

        private void fileCreate(int diskType, string fileName)
        {
            var getDos = new MsdosFile(); // create instance of MsdosFile, then call function

            
            // bool fileNew = false;
            string path = fileName;
            OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();

            if (path.Length == 0) // Create disk image if no path provided
            {
                openFileDialog1.InitialDirectory = tbFolder.Text;
                openFileDialog1.Filter = "DOS Files (*.DOS)|*.DOS.*";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;
                openFileDialog1.CheckFileExists = false;
                openFileDialog1.ShowDialog();
                path = openFileDialog1.FileName;
            }

            //Console.Write("Path: ");
            //Console.WriteLine(path);

            //Console.WriteLine(File.Exists(path));
            var result = File.Exists(path);
            //Console.WriteLine("Result: {0:G}", result);
            int diskTotalBytes = 0;
            if (!result)
            {
                //Console.WriteLine("File DOES Not Exist");
                //Console.WriteLine("But it was Created: {0:G}", File.Exists(path));
                // calculate buffer size for disk image
                diskTotalBytes = getDos.DiskType[diskType, 5] * getDos.DiskType[diskType, 6] *
                                 getDos.DiskType[diskType, 7];
                var dosBuf = new byte[diskTotalBytes];
                for (var i = 0; i < diskTotalBytes; i++)
                    dosBuf[i] = 0xE5;
                
                switch (diskTotalBytes)

                {
                    case 0x2D0:
                        byte[] diskMark1 = { 0xe9,0x83,0,0x31,0x56,0x68,0x23,0x29,0x49,0x48,0x43,0,0x02,0x02,0x01,0,0x02,0x70,0,0x80,2,0xff ,1};
                        for (var i = 0; i < diskMark1.Length; i++)
                            dosBuf[i] = diskMark1[i];
                        break;
                    case 0x280:
                        byte[] diskMark2 = { 0xe9, 0x83, 0, 0x31, 0x43, 0x53, 0x62,0x52, 0x49, 0x48, 0x43, 0, 0x02, 0x02, 0x01, 0, 0x02, 0x70, 0, 0xd0, 2, 0xfd,2 };
                        for (var i = 0; i < diskMark2.Length; i++)
                            dosBuf[i] = diskMark2[i];

                        break;
                    case 0x500:
                        byte[] diskMark3 = { 0xe9, 0x83, 0, 0x31, 0x43, 0x53, 0x62, 0x52, 0x49, 0x48, 0x43, 0, 0x02, 0x02, 0x01, 0, 0x02, 0x70, 0, 0x00, 5, 0xfb, 2};
                        for (var i = 0; i < diskMark3.Length; i++)
                            dosBuf[i] = diskMark3[i];

                        break;
                    default:
                        break;
                }
                FileStream fso = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                BinaryWriter fileOutByte = new BinaryWriter(fso);

                fileOutByte.Write(dosBuf, 0, diskTotalBytes);
                fileOutByte.Close();
                fso.Dispose();

            }

            /*
             * Use ReadCmDir to read disk into CPMFiles buffer. After the file is in the buffer, add
             * each new file. Write CPMFiles buffer to disk if any files are successfully added
             */
            getDos.ReadMsdosDir(path, ref diskTotalBytes);




            // Get Files to add to image
            var startDir =
                tbFolder.Text; // openFileDialog1.InitialDirectory; // check if a working folder is selected
            var fileCnt = 0;

            if (startDir == "") startDir = "c:\\";
            openFileDialog1.InitialDirectory = startDir;
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;
            openFileDialog1.Title = "Select Files to Add to Image";
            string temp = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                foreach (String filename in openFileDialog1.FileNames)
                    fileCnt += getDos.InsertFileMsdos(filename);
            if (fileCnt > 0) // Added a file or two
            {
                FileStream fsOut = new FileStream(path, FileMode.Open, FileAccess.Write);
                BinaryWriter fileOutBytes = new BinaryWriter(fsOut);

                fileOutBytes.Seek(0, SeekOrigin.Current);
                fileOutBytes.Write(getDos.buf, 0, diskTotalBytes);
                fileOutBytes.Close();
                fsOut.Dispose();
            }
        }




        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            string path = "";
            foreach (var lb in listBox1.SelectedItems)
            {
                path = tbFolder.Text + "\\" + lb.ToString();
                fileCreate(0, path);
            }
        }

        private void TestCPM_click(object sender, EventArgs e)
        {
            var cpmTest = new CPMFile();
            OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            var path = "TestCPM.H37";
            openFileDialog1.InitialDirectory = tbFolder.Text;
            openFileDialog1.Filter = "H37 files (*.H37)|*.H37";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.CheckFileExists = false;
            openFileDialog1.ShowDialog();
            path = openFileDialog1.FileName;

            int diskType = 0x6f;
            int diskTotalBytes = cpmTest.DiskType[diskType, 6] * cpmTest.DiskType[diskType, 7] *
                             cpmTest.DiskType[diskType, 8];
            var cpmBuf = new byte[diskTotalBytes];
            for (var i = 0; i < diskTotalBytes; i++)
                cpmBuf[i] = 0xE5;
            cpmBuf[cpmTest.H37disktype] = (byte)cpmTest.DiskType[diskType, 0]; // set disk type marker

            byte[] diskMark1 = { 0, 0x6f, 0xe5, 8, 0x10, 0xe5, 0xe5, 1, 0xe5, 0x28, 0, 4, 0xf, 0, 0x8a, 0x01, 0xff, 0, 0xf0, 0, 0x40, 0, 2, 0, 0xec };
            for (var i = 0; i < diskMark1.Length; i++)
                cpmBuf[cpmTest.H37disktype - 1 + i] = diskMark1[i];
            FileStream fso = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            BinaryWriter fileOutByte = new BinaryWriter(fso);

            fileOutByte.Write(cpmBuf, 0, diskTotalBytes);
            fileOutByte.Close();
            fso.Dispose();
            cpmTest.ReadCpmDir(path, ref diskTotalBytes);
            var startDir =
                tbFolder.Text; // openFileDialog1.InitialDirectory; // check if a working folder is selected
            var fileCnt = 0;

            if (startDir == "") startDir = "c:\\";
            openFileDialog1.InitialDirectory = startDir;
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;
            openFileDialog1.Title = "Select Files to Add to Image";
            string temp = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                foreach (String filename in openFileDialog1.FileNames)
                    fileCnt += cpmTest.InsertFileCpm(filename);


            FileStream fsOut = new FileStream(path, FileMode.Open, FileAccess.Write);
            BinaryWriter fileOutBytes = new BinaryWriter(fsOut);

            fileOutBytes.Seek(0, SeekOrigin.Current);
            fileOutBytes.Write(cpmTest.buf, 0, diskTotalBytes);
            fileOutBytes.Close();
            fsOut.Dispose();
        }

 
    }
}
