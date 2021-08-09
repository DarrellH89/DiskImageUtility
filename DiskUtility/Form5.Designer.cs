namespace DiskUtility
    {
    partial class Form5
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
            this.buttonFolder = new System.Windows.Forms.Button();
            this.tbFolder = new System.Windows.Forms.TextBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.btnH8d100 = new System.Windows.Forms.Button();
            this.btnH37_800 = new System.Windows.Forms.Button();
            this.btnH37_640 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnH37_360 = new System.Windows.Forms.Button();
            this.btnSz80_1440 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonFolder
            // 
            this.buttonFolder.Location = new System.Drawing.Point(30, 71);
            this.buttonFolder.Name = "buttonFolder";
            this.buttonFolder.Size = new System.Drawing.Size(84, 23);
            this.buttonFolder.TabIndex = 0;
            this.buttonFolder.Text = "Folder";
            this.buttonFolder.UseVisualStyleBackColor = true;
            this.buttonFolder.Click += new System.EventHandler(this.ButtonFolder_Click);
            // 
            // tbFolder
            // 
            this.tbFolder.Location = new System.Drawing.Point(131, 74);
            this.tbFolder.Name = "tbFolder";
            this.tbFolder.Size = new System.Drawing.Size(439, 20);
            this.tbFolder.TabIndex = 1;
            // 
            // listBox1
            // 
            this.listBox1.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.HorizontalScrollbar = true;
            this.listBox1.ItemHeight = 14;
            this.listBox1.Location = new System.Drawing.Point(30, 113);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(313, 340);
            this.listBox1.TabIndex = 2;
            this.listBox1.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
            // 
            // btnH8d100
            // 
            this.btnH8d100.Location = new System.Drawing.Point(440, 146);
            this.btnH8d100.Name = "btnH8d100";
            this.btnH8d100.Size = new System.Drawing.Size(130, 23);
            this.btnH8d100.TabIndex = 3;
            this.btnH8d100.Text = "H8D 100k";
            this.btnH8d100.UseVisualStyleBackColor = true;
            this.btnH8d100.Click += new System.EventHandler(this.ButtonH8d100_Click);
            // 
            // btnH37_800
            // 
            this.btnH37_800.Location = new System.Drawing.Point(440, 175);
            this.btnH37_800.Name = "btnH37_800";
            this.btnH37_800.Size = new System.Drawing.Size(130, 23);
            this.btnH37_800.TabIndex = 5;
            this.btnH37_800.Text = "H37 80 trk DS ED 800k";
            this.btnH37_800.UseVisualStyleBackColor = true;
            this.btnH37_800.Click += new System.EventHandler(this.Buttonh37_806f_Click);
            // 
            // btnH37_640
            // 
            this.btnH37_640.Location = new System.Drawing.Point(440, 204);
            this.btnH37_640.Name = "btnH37_640";
            this.btnH37_640.Size = new System.Drawing.Size(130, 23);
            this.btnH37_640.TabIndex = 6;
            this.btnH37_640.Text = "H37 80 trk DS DD 640k";
            this.btnH37_640.UseVisualStyleBackColor = true;
            this.btnH37_640.Click += new System.EventHandler(this.ButtonH37_806b_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(27, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(489, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Click Folder to change directories.  Double click file name to add files";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(437, 113);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(232, 18);
            this.label2.TabIndex = 8;
            this.label2.Text = "Click below to create an empty file";
            // 
            // btnH37_360
            // 
            this.btnH37_360.Location = new System.Drawing.Point(440, 233);
            this.btnH37_360.Name = "btnH37_360";
            this.btnH37_360.Size = new System.Drawing.Size(130, 21);
            this.btnH37_360.TabIndex = 10;
            this.btnH37_360.Text = "H37 40 trk 400k";
            this.btnH37_360.UseVisualStyleBackColor = true;
            this.btnH37_360.Click += new System.EventHandler(this.ButtonH37_4067_Click);
            // 
            // btnSz80_1440
            // 
            this.btnSz80_1440.Location = new System.Drawing.Point(440, 318);
            this.btnSz80_1440.Name = "btnSz80_1440";
            this.btnSz80_1440.Size = new System.Drawing.Size(130, 21);
            this.btnSz80_1440.TabIndex = 11;
            this.btnSz80_1440.Text = "Small Z-80 1,440k";
            this.btnSz80_1440.UseVisualStyleBackColor = true;
            this.btnSz80_1440.Click += new System.EventHandler(this.smallz80_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(440, 384);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(130, 21);
            this.button1.TabIndex = 12;
            this.button1.Text = "MS-DOS 360k";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.ButtonDos_360_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(440, 411);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(130, 21);
            this.button2.TabIndex = 13;
            this.button2.Text = "MS-DOS 720k";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.ButtonDos_720_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(440, 291);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(130, 21);
            this.button3.TabIndex = 14;
            this.button3.Text = "H37 40 trk 100k";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.ButtonH37_4060_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(440, 438);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(130, 21);
            this.button4.TabIndex = 15;
            this.button4.Text = "MS-DOS 1440k";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.ButtonDos_1440_Click);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(440, 345);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(130, 21);
            this.button5.TabIndex = 16;
            this.button5.Text = "Z-100 320k";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.ButtonZ100_cpm_Click);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(440, 260);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(130, 21);
            this.button6.TabIndex = 17;
            this.button6.Text = "H37 40 trk 320k";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.ButtonH37_4063_Click);
            // 
            // Form5
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(758, 557);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnSz80_1440);
            this.Controls.Add(this.btnH37_360);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnH37_640);
            this.Controls.Add(this.btnH37_800);
            this.Controls.Add(this.btnH8d100);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.tbFolder);
            this.Controls.Add(this.buttonFolder);
            this.Name = "Form5";
            this.Text = "Add Files to Disk Image";
            this.Load += new System.EventHandler(this.Form5_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

            }

        #endregion
        private System.Windows.Forms.Button buttonFolder;
        private System.Windows.Forms.TextBox tbFolder;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button btnH8d100;
        private System.Windows.Forms.Button btnH37_800;
        private System.Windows.Forms.Button btnH37_640;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnH37_360;
        private System.Windows.Forms.Button btnSz80_1440;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
    }
    }