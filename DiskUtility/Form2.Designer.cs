
namespace MSDOSAdd
{
    partial class Form2
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
            this.btnFolder = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnDos_320 = new System.Windows.Forms.Button();
            this.btnDos_360 = new System.Windows.Forms.Button();
            this.btnDos_640 = new System.Windows.Forms.Button();
            this.tbFolder = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnFolder
            // 
            this.btnFolder.Location = new System.Drawing.Point(47, 63);
            this.btnFolder.Name = "btnFolder";
            this.btnFolder.Size = new System.Drawing.Size(81, 26);
            this.btnFolder.TabIndex = 0;
            this.btnFolder.Text = "Folder";
            this.btnFolder.UseVisualStyleBackColor = true;
            this.btnFolder.Click += new System.EventHandler(this.ButtonFolder_Click);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(47, 106);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(349, 381);
            this.listBox1.TabIndex = 1;
            this.listBox1.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(44, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(489, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Click Folder to change directories.  Double click file name to add files";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(487, 117);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(280, 18);
            this.label2.TabIndex = 3;
            this.label2.Text = "Click below to create an blank disk image";
            // 
            // btnDos_320
            // 
            this.btnDos_320.Location = new System.Drawing.Point(490, 164);
            this.btnDos_320.Name = "btnDos_320";
            this.btnDos_320.Size = new System.Drawing.Size(110, 24);
            this.btnDos_320.TabIndex = 4;
            this.btnDos_320.Text = "MS-DOS 320k";
            this.btnDos_320.UseVisualStyleBackColor = true;
            this.btnDos_320.Click += new System.EventHandler(this.btnDos_320_Click);
            // 
            // btnDos_360
            // 
            this.btnDos_360.Location = new System.Drawing.Point(490, 206);
            this.btnDos_360.Name = "btnDos_360";
            this.btnDos_360.Size = new System.Drawing.Size(110, 24);
            this.btnDos_360.TabIndex = 5;
            this.btnDos_360.Text = "MS-DOS 360k";
            this.btnDos_360.UseVisualStyleBackColor = true;
            this.btnDos_360.Click += new System.EventHandler(this.btnDos360_Click);
            // 
            // btnDos_640
            // 
            this.btnDos_640.Location = new System.Drawing.Point(490, 247);
            this.btnDos_640.Name = "btnDos_640";
            this.btnDos_640.Size = new System.Drawing.Size(110, 24);
            this.btnDos_640.TabIndex = 6;
            this.btnDos_640.Text = "MS-DOS 640k";
            this.btnDos_640.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDos_640.UseVisualStyleBackColor = true;
            this.btnDos_640.Click += new System.EventHandler(this.btnDos640_Click);
            // 
            // tbFolder
            // 
            this.tbFolder.Location = new System.Drawing.Point(137, 65);
            this.tbFolder.Name = "tbFolder";
            this.tbFolder.Size = new System.Drawing.Size(380, 20);
            this.tbFolder.TabIndex = 7;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(808, 593);
            this.Controls.Add(this.tbFolder);
            this.Controls.Add(this.btnDos_640);
            this.Controls.Add(this.btnDos_360);
            this.Controls.Add(this.btnDos_320);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.btnFolder);
            this.Name = "Form2";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add files to MS-DOS image";
            this.Load += new System.EventHandler(this.Form2_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnFolder;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnDos_320;
        private System.Windows.Forms.Button btnDos_360;
        private System.Windows.Forms.Button btnDos_640;
        private System.Windows.Forms.TextBox tbFolder;
    }
}