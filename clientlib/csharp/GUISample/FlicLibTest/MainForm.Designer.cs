namespace FlicLibTest
{
    partial class MainForm
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
            buttonsList = new System.Windows.Forms.FlowLayoutPanel();
            lblConnectionStatus = new System.Windows.Forms.Label();
            lblBluetoothStatus = new System.Windows.Forms.Label();
            txtServer = new System.Windows.Forms.TextBox();
            lblServer = new System.Windows.Forms.Label();
            lblPort = new System.Windows.Forms.Label();
            txtPort = new System.Windows.Forms.TextBox();
            btnConnectDisconnect = new System.Windows.Forms.Button();
            btnAddNewFlic = new System.Windows.Forms.Button();
            lblScanWizardStatus = new System.Windows.Forms.Label();
            btnPing = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // buttonsList
            // 
            buttonsList.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            buttonsList.AutoScroll = true;
            buttonsList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            buttonsList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            buttonsList.Location = new System.Drawing.Point(15, 15);
            buttonsList.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonsList.Name = "buttonsList";
            buttonsList.Size = new System.Drawing.Size(262, 413);
            buttonsList.TabIndex = 0;
            buttonsList.WrapContents = false;
            // 
            // lblConnectionStatus
            // 
            lblConnectionStatus.AutoSize = true;
            lblConnectionStatus.Location = new System.Drawing.Point(320, 15);
            lblConnectionStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblConnectionStatus.Name = "lblConnectionStatus";
            lblConnectionStatus.Size = new System.Drawing.Size(181, 15);
            lblConnectionStatus.TabIndex = 1;
            lblConnectionStatus.Text = "Connection status: Disconnected";
            // 
            // lblBluetoothStatus
            // 
            lblBluetoothStatus.AutoSize = true;
            lblBluetoothStatus.Location = new System.Drawing.Point(318, 30);
            lblBluetoothStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblBluetoothStatus.Name = "lblBluetoothStatus";
            lblBluetoothStatus.Size = new System.Drawing.Size(150, 15);
            lblBluetoothStatus.TabIndex = 2;
            lblBluetoothStatus.Text = "Bluetooth controller status:";
            // 
            // txtServer
            // 
            txtServer.Location = new System.Drawing.Point(322, 117);
            txtServer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtServer.Name = "txtServer";
            txtServer.Size = new System.Drawing.Size(116, 23);
            txtServer.TabIndex = 3;
            txtServer.Text = "localhost";
            // 
            // lblServer
            // 
            lblServer.AutoSize = true;
            lblServer.Location = new System.Drawing.Point(318, 98);
            lblServer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblServer.Name = "lblServer";
            lblServer.Size = new System.Drawing.Size(42, 15);
            lblServer.TabIndex = 4;
            lblServer.Text = "Server:";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new System.Drawing.Point(318, 143);
            lblPort.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblPort.Name = "lblPort";
            lblPort.Size = new System.Drawing.Size(32, 15);
            lblPort.TabIndex = 5;
            lblPort.Text = "Port:";
            // 
            // txtPort
            // 
            txtPort.Location = new System.Drawing.Point(322, 162);
            txtPort.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtPort.Name = "txtPort";
            txtPort.Size = new System.Drawing.Size(116, 23);
            txtPort.TabIndex = 6;
            txtPort.Text = "5551";
            // 
            // btnConnectDisconnect
            // 
            btnConnectDisconnect.Location = new System.Drawing.Point(321, 193);
            btnConnectDisconnect.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnConnectDisconnect.Name = "btnConnectDisconnect";
            btnConnectDisconnect.Size = new System.Drawing.Size(118, 27);
            btnConnectDisconnect.TabIndex = 7;
            btnConnectDisconnect.Text = "Connect";
            btnConnectDisconnect.UseVisualStyleBackColor = true;
            btnConnectDisconnect.Click += btnConnectDisconnect_Click;
            // 
            // btnAddNewFlic
            // 
            btnAddNewFlic.Enabled = false;
            btnAddNewFlic.Location = new System.Drawing.Point(323, 261);
            btnAddNewFlic.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnAddNewFlic.Name = "btnAddNewFlic";
            btnAddNewFlic.Size = new System.Drawing.Size(115, 27);
            btnAddNewFlic.TabIndex = 8;
            btnAddNewFlic.Text = "Add new Flic";
            btnAddNewFlic.UseVisualStyleBackColor = true;
            btnAddNewFlic.Click += btnAddNewFlic_Click;
            // 
            // lblScanWizardStatus
            // 
            lblScanWizardStatus.AutoSize = true;
            lblScanWizardStatus.Location = new System.Drawing.Point(323, 295);
            lblScanWizardStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblScanWizardStatus.Name = "lblScanWizardStatus";
            lblScanWizardStatus.Size = new System.Drawing.Size(113, 15);
            lblScanWizardStatus.TabIndex = 9;
            lblScanWizardStatus.Text = "lblScanWizardStatus";
            // 
            // btnPing
            // 
            btnPing.Enabled = false;
            btnPing.Location = new System.Drawing.Point(323, 226);
            btnPing.Name = "btnPing";
            btnPing.Size = new System.Drawing.Size(115, 23);
            btnPing.TabIndex = 10;
            btnPing.Text = "Ping";
            btnPing.UseVisualStyleBackColor = true;
            btnPing.Click += btnPing_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(742, 442);
            Controls.Add(btnPing);
            Controls.Add(lblScanWizardStatus);
            Controls.Add(btnAddNewFlic);
            Controls.Add(btnConnectDisconnect);
            Controls.Add(txtPort);
            Controls.Add(lblPort);
            Controls.Add(lblServer);
            Controls.Add(txtServer);
            Controls.Add(lblBluetoothStatus);
            Controls.Add(lblConnectionStatus);
            Controls.Add(buttonsList);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "MainForm";
            Text = "Flic Sample";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel buttonsList;
        private System.Windows.Forms.Label lblConnectionStatus;
        private System.Windows.Forms.Label lblBluetoothStatus;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Button btnConnectDisconnect;
        private System.Windows.Forms.Button btnAddNewFlic;
        private System.Windows.Forms.Label lblScanWizardStatus;
        private System.Windows.Forms.Button btnPing;
    }
}

