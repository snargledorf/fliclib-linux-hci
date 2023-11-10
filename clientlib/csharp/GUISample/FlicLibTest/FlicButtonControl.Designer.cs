namespace FlicLibTest
{
    partial class FlicButtonControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblBdAddr = new System.Windows.Forms.Label();
            lblStatus = new System.Windows.Forms.Label();
            pictureBox = new System.Windows.Forms.PictureBox();
            chkListen = new System.Windows.Forms.CheckBox();
            btnDisconnect = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            SuspendLayout();
            // 
            // lblBdAddr
            // 
            lblBdAddr.AutoSize = true;
            lblBdAddr.Location = new System.Drawing.Point(5, 10);
            lblBdAddr.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblBdAddr.Name = "lblBdAddr";
            lblBdAddr.Size = new System.Drawing.Size(94, 15);
            lblBdAddr.TabIndex = 0;
            lblBdAddr.Text = "00:00:00:00:00:00";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new System.Drawing.Point(5, 25);
            lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(79, 15);
            lblStatus.TabIndex = 1;
            lblStatus.Text = "Disconnected";
            // 
            // pictureBox
            // 
            pictureBox.BackColor = System.Drawing.Color.Red;
            pictureBox.Location = new System.Drawing.Point(155, 10);
            pictureBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pictureBox.Name = "pictureBox";
            pictureBox.Size = new System.Drawing.Size(84, 58);
            pictureBox.TabIndex = 3;
            pictureBox.TabStop = false;
            // 
            // chkListen
            // 
            chkListen.Appearance = System.Windows.Forms.Appearance.Button;
            chkListen.Location = new System.Drawing.Point(5, 43);
            chkListen.Name = "chkListen";
            chkListen.Size = new System.Drawing.Size(62, 25);
            chkListen.TabIndex = 4;
            chkListen.Text = "Listen";
            chkListen.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            chkListen.UseVisualStyleBackColor = true;
            chkListen.CheckedChanged += chkListen_CheckedChanged;
            // 
            // btnDisconnect
            // 
            btnDisconnect.Location = new System.Drawing.Point(73, 45);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new System.Drawing.Size(75, 23);
            btnDisconnect.TabIndex = 5;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // FlicButtonControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(btnDisconnect);
            Controls.Add(chkListen);
            Controls.Add(pictureBox);
            Controls.Add(lblStatus);
            Controls.Add(lblBdAddr);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "FlicButtonControl";
            Size = new System.Drawing.Size(253, 81);
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        public System.Windows.Forms.Label lblBdAddr;
        public System.Windows.Forms.Label lblStatus;
        public System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.CheckBox chkListen;
        private System.Windows.Forms.Button btnDisconnect;
    }
}
