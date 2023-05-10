namespace SirenSharp
{
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            linkLabel1 = new LinkLabel();
            linkLabel2 = new LinkLabel();
            label4 = new Label();
            label5 = new Label();
            linkLabel3 = new LinkLabel();
            label6 = new Label();
            linkLabel4 = new LinkLabel();
            label7 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(186, 42);
            label1.Name = "label1";
            label1.Size = new Size(150, 37);
            label1.TabIndex = 0;
            label1.Text = "SirenSharp";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(223, 90);
            label2.Name = "label2";
            label2.Size = new Size(81, 15);
            label2.TabIndex = 1;
            label2.Text = "Version: v0.2.1";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(67, 167);
            label3.Name = "label3";
            label3.Size = new Size(48, 15);
            label3.TabIndex = 2;
            label3.Text = "GitHub:";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(121, 167);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(215, 15);
            linkLabel1.TabIndex = 3;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "https://github.com/BJDubb/SirenSharp";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(127, 199);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(202, 15);
            linkLabel2.TabIndex = 5;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "https://www.youtube.com/@bjdubb";
            linkLabel2.LinkClicked += linkLabel2_LinkClicked;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(67, 199);
            label4.Name = "label4";
            label4.Size = new Size(56, 15);
            label4.TabIndex = 4;
            label4.Text = "YouTube:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(214, 114);
            label5.Name = "label5";
            label5.Size = new Size(90, 15);
            label5.TabIndex = 6;
            label5.Text = "Author: BJDubb";
            // 
            // linkLabel3
            // 
            linkLabel3.AutoSize = true;
            linkLabel3.Location = new Point(123, 231);
            linkLabel3.Name = "linkLabel3";
            linkLabel3.Size = new Size(184, 15);
            linkLabel3.TabIndex = 8;
            linkLabel3.TabStop = true;
            linkLabel3.Text = "https://discord.gg/GCMRtBNCXR";
            linkLabel3.LinkClicked += linkLabel3_LinkClicked;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(67, 231);
            label6.Name = "label6";
            label6.Size = new Size(50, 15);
            label6.TabIndex = 7;
            label6.Text = "Discord:";
            // 
            // linkLabel4
            // 
            linkLabel4.AutoSize = true;
            linkLabel4.Location = new Point(145, 261);
            linkLabel4.Name = "linkLabel4";
            linkLabel4.Size = new Size(142, 15);
            linkLabel4.TabIndex = 10;
            linkLabel4.TabStop = true;
            linkLabel4.Text = "https://ko-fi.com/bjdubb";
            linkLabel4.LinkClicked += linkLabel4_LinkClicked;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(67, 261);
            label7.Name = "label7";
            label7.Size = new Size(72, 15);
            label7.TabIndex = 9;
            label7.Text = "Support Me:";
            // 
            // AboutForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(537, 318);
            Controls.Add(linkLabel4);
            Controls.Add(label7);
            Controls.Add(linkLabel3);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(linkLabel2);
            Controls.Add(label4);
            Controls.Add(linkLabel1);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "AboutForm";
            Text = "About";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private LinkLabel linkLabel1;
        private LinkLabel linkLabel2;
        private Label label4;
        private Label label5;
        private LinkLabel linkLabel3;
        private Label label6;
        private LinkLabel linkLabel4;
        private Label label7;
    }
}