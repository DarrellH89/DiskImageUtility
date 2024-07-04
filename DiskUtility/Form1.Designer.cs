
namespace DiskUtility
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBoxImages = new System.Windows.Forms.ListBox();
            this.listBoxFiles = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.BtnFolder = new System.Windows.Forms.Button();
            this.BtnExtract = new System.Windows.Forms.Button();
            this.BtnAddCpm = new System.Windows.Forms.Button();
            this.BtnAddMsdos = new System.Windows.Forms.Button();
            this.BtnImdConvert = new System.Windows.Forms.Button();
            this.BtnDelete = new System.Windows.Forms.Button();
            this.BtnView = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.folderBrowserDialog2 = new System.Windows.Forms.FolderBrowserDialog();
            this.BtnfileList = new System.Windows.Forms.Button();
            this.labelVersion = new System.Windows.Forms.Label();
            this.labelFolder = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.BtnAddHdos = new System.Windows.Forms.Button();
            this.btnOption = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxImages
            // 
            this.listBoxImages.FormattingEnabled = true;
            this.listBoxImages.HorizontalScrollbar = true;
            this.listBoxImages.Location = new System.Drawing.Point(2, 29);
            this.listBoxImages.Name = "listBoxImages";
            this.listBoxImages.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBoxImages.Size = new System.Drawing.Size(306, 433);
            this.listBoxImages.TabIndex = 0;
            // 
            // listBoxFiles
            // 
            this.listBoxFiles.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxFiles.FormattingEnabled = true;
            this.listBoxFiles.HorizontalScrollbar = true;
            this.listBoxFiles.ItemHeight = 15;
            this.listBoxFiles.Location = new System.Drawing.Point(0, 30);
            this.listBoxFiles.Name = "listBoxFiles";
            this.listBoxFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBoxFiles.Size = new System.Drawing.Size(455, 439);
            this.listBoxFiles.TabIndex = 1;
            this.listBoxFiles.Click += new System.EventHandler(this.BtnListboxfilesCopy_Click);
            this.listBoxFiles.SelectedIndexChanged += new System.EventHandler(this.listBoxFiles_SelectedIndexChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.listBoxImages);
            this.groupBox1.Location = new System.Drawing.Point(20, 53);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(308, 461);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Disk Image List";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(101, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "label3";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.listBoxFiles);
            this.groupBox2.Location = new System.Drawing.Point(390, 51);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(461, 463);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "File List";
            // 
            // BtnFolder
            // 
            this.BtnFolder.Location = new System.Drawing.Point(22, 25);
            this.BtnFolder.Name = "BtnFolder";
            this.BtnFolder.Size = new System.Drawing.Size(85, 22);
            this.BtnFolder.TabIndex = 4;
            this.BtnFolder.Text = "Folder";
            this.BtnFolder.UseVisualStyleBackColor = true;
            this.BtnFolder.Click += new System.EventHandler(this.BtnFolder_Click);
            // 
            // BtnExtract
            // 
            this.BtnExtract.Location = new System.Drawing.Point(491, 539);
            this.BtnExtract.Name = "BtnExtract";
            this.BtnExtract.Size = new System.Drawing.Size(85, 22);
            this.BtnExtract.TabIndex = 5;
            this.BtnExtract.Text = "Extract";
            this.BtnExtract.UseVisualStyleBackColor = true;
            this.BtnExtract.Click += new System.EventHandler(this.btnExtract_Click);
            // 
            // BtnAddCpm
            // 
            this.BtnAddCpm.Location = new System.Drawing.Point(595, 539);
            this.BtnAddCpm.Name = "BtnAddCpm";
            this.BtnAddCpm.Size = new System.Drawing.Size(85, 22);
            this.BtnAddCpm.TabIndex = 6;
            this.BtnAddCpm.Text = "Add CP/M";
            this.BtnAddCpm.UseVisualStyleBackColor = true;
            this.BtnAddCpm.Click += new System.EventHandler(this.buttonCreateCPM_click);
            // 
            // BtnAddMsdos
            // 
            this.BtnAddMsdos.Location = new System.Drawing.Point(595, 595);
            this.BtnAddMsdos.Name = "BtnAddMsdos";
            this.BtnAddMsdos.Size = new System.Drawing.Size(85, 22);
            this.BtnAddMsdos.TabIndex = 7;
            this.BtnAddMsdos.Text = "Add MS-DOS";
            this.BtnAddMsdos.UseVisualStyleBackColor = true;
            this.BtnAddMsdos.Click += new System.EventHandler(this.buttonCreateMSDOS_click);
            // 
            // BtnImdConvert
            // 
            this.BtnImdConvert.Location = new System.Drawing.Point(702, 539);
            this.BtnImdConvert.Name = "BtnImdConvert";
            this.BtnImdConvert.Size = new System.Drawing.Size(106, 22);
            this.BtnImdConvert.TabIndex = 8;
            this.BtnImdConvert.Text = "IMD/IMG Convert";
            this.BtnImdConvert.UseVisualStyleBackColor = true;
            this.BtnImdConvert.Click += new System.EventHandler(this.BtnImdConvert_click);
            // 
            // BtnDelete
            // 
            this.BtnDelete.Location = new System.Drawing.Point(491, 595);
            this.BtnDelete.Name = "BtnDelete";
            this.BtnDelete.Size = new System.Drawing.Size(85, 22);
            this.BtnDelete.TabIndex = 9;
            this.BtnDelete.Text = "Delete";
            this.BtnDelete.UseVisualStyleBackColor = true;
            this.BtnDelete.Click += new System.EventHandler(this.ButtonDelete);
            // 
            // BtnView
            // 
            this.BtnView.Location = new System.Drawing.Point(491, 567);
            this.BtnView.Name = "BtnView";
            this.BtnView.Size = new System.Drawing.Size(85, 22);
            this.BtnView.TabIndex = 15;
            this.BtnView.Text = "View";
            this.BtnView.UseVisualStyleBackColor = true;
            this.BtnView.Click += new System.EventHandler(this.BtnView_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(444, 520);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 16;
            this.label1.Text = "File Operations";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(643, 520);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 13);
            this.label2.TabIndex = 17;
            this.label2.Text = "Disk Operations";
            // 
            // BtnfileList
            // 
            this.BtnfileList.Location = new System.Drawing.Point(390, 539);
            this.BtnfileList.Name = "BtnfileList";
            this.BtnfileList.Size = new System.Drawing.Size(85, 22);
            this.BtnfileList.TabIndex = 18;
            this.BtnfileList.Text = "File List";
            this.BtnfileList.UseVisualStyleBackColor = true;
            this.BtnfileList.Click += new System.EventHandler(this.BtnFileList_Click);
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Location = new System.Drawing.Point(170, 635);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(35, 13);
            this.labelVersion.TabIndex = 19;
            this.labelVersion.Text = "label4";
            // 
            // labelFolder
            // 
            this.labelFolder.AutoSize = true;
            this.labelFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFolder.Location = new System.Drawing.Point(121, 28);
            this.labelFolder.Name = "labelFolder";
            this.labelFolder.Size = new System.Drawing.Size(76, 16);
            this.labelFolder.TabIndex = 20;
            this.labelFolder.Text = "labelFolder";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(702, 567);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(106, 22);
            this.button1.TabIndex = 21;
            this.button1.Text = "H8D Convert to SS";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.BtnH8dConvert_Click);
            // 
            // BtnAddHdos
            // 
            this.BtnAddHdos.Location = new System.Drawing.Point(595, 567);
            this.BtnAddHdos.Name = "BtnAddHdos";
            this.BtnAddHdos.Size = new System.Drawing.Size(85, 22);
            this.BtnAddHdos.TabIndex = 8;
            this.BtnAddHdos.Text = "Add HDOS";
            this.BtnAddHdos.UseVisualStyleBackColor = true;
            this.BtnAddHdos.Click += new System.EventHandler(this.buttonCreateHdos_click);
            // 
            // btnOption
            // 
            this.btnOption.Location = new System.Drawing.Point(22, 595);
            this.btnOption.Name = "btnOption";
            this.btnOption.Size = new System.Drawing.Size(85, 22);
            this.btnOption.TabIndex = 22;
            this.btnOption.Text = "Options";
            this.btnOption.UseVisualStyleBackColor = true;
            this.btnOption.Click += new System.EventHandler(this.btnOption_click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(972, 712);
            this.Controls.Add(this.btnOption);
            this.Controls.Add(this.BtnAddHdos);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.labelFolder);
            this.Controls.Add(this.labelVersion);
            this.Controls.Add(this.BtnfileList);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.BtnView);
            this.Controls.Add(this.BtnDelete);
            this.Controls.Add(this.BtnImdConvert);
            this.Controls.Add(this.BtnAddMsdos);
            this.Controls.Add(this.BtnAddCpm);
            this.Controls.Add(this.BtnExtract);
            this.Controls.Add(this.BtnFolder);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.Name = "Form1";
            this.Text = "Disk Image Utility 2021 by Darrell Pelan";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxImages;
        private System.Windows.Forms.ListBox listBoxFiles;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button BtnFolder;
        private System.Windows.Forms.Button BtnExtract;
        private System.Windows.Forms.Button BtnAddCpm;
        private System.Windows.Forms.Button BtnAddMsdos;
        private System.Windows.Forms.Button BtnImdConvert;
        private System.Windows.Forms.Button BtnDelete;
        private System.Windows.Forms.Button BtnView;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button BtnfileList;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label labelFolder;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button BtnAddHdos;
        private System.Windows.Forms.Button btnOption;
    }
}

