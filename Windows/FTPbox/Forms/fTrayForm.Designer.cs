namespace FTPbox.Forms
{
    partial class fTrayForm
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
            this.lCurrentStatus = new System.Windows.Forms.Label();
            this.fRecentList = new System.Windows.Forms.FlowLayoutPanel();
            this.pLocalFolder = new System.Windows.Forms.PictureBox();
            this.pSettings = new System.Windows.Forms.PictureBox();
            this.lTitle = new System.Windows.Forms.Label();
            this.lSize = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pLocalFolder)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pSettings)).BeginInit();
            this.SuspendLayout();
            // 
            // lCurrentStatus
            // 
            this.lCurrentStatus.BackColor = System.Drawing.Color.Transparent;
            this.lCurrentStatus.Location = new System.Drawing.Point(88, 273);
            this.lCurrentStatus.Name = "lCurrentStatus";
            this.lCurrentStatus.Size = new System.Drawing.Size(250, 13);
            this.lCurrentStatus.TabIndex = 1;
            this.lCurrentStatus.Text = "All files synchronized";
            this.lCurrentStatus.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // fRecentList
            // 
            this.fRecentList.AutoScroll = true;
            this.fRecentList.BackColor = System.Drawing.Color.White;
            this.fRecentList.Location = new System.Drawing.Point(0, 40);
            this.fRecentList.Name = "fRecentList";
            this.fRecentList.Size = new System.Drawing.Size(344, 219);
            this.fRecentList.TabIndex = 2;
            // 
            // pLocalFolder
            // 
            this.pLocalFolder.AccessibleDescription = "open the local folder";
            this.pLocalFolder.BackColor = System.Drawing.Color.Transparent;
            this.pLocalFolder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pLocalFolder.Image = global::FTPbox.Properties.Resources.folder;
            this.pLocalFolder.Location = new System.Drawing.Point(42, 271);
            this.pLocalFolder.Name = "pLocalFolder";
            this.pLocalFolder.Size = new System.Drawing.Size(16, 16);
            this.pLocalFolder.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pLocalFolder.TabIndex = 3;
            this.pLocalFolder.TabStop = false;
            this.pLocalFolder.Click += new System.EventHandler(this.pLocalFolder_Click);
            // 
            // pSettings
            // 
            this.pSettings.AccessibleDescription = "open the settings form";
            this.pSettings.BackColor = System.Drawing.Color.Transparent;
            this.pSettings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pSettings.Image = global::FTPbox.Properties.Resources.settings;
            this.pSettings.Location = new System.Drawing.Point(12, 271);
            this.pSettings.Name = "pSettings";
            this.pSettings.Size = new System.Drawing.Size(16, 16);
            this.pSettings.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pSettings.TabIndex = 4;
            this.pSettings.TabStop = false;
            this.pSettings.Click += new System.EventHandler(this.pSettings_Click);
            // 
            // lTitle
            // 
            this.lTitle.AutoSize = true;
            this.lTitle.Location = new System.Drawing.Point(9, 13);
            this.lTitle.Name = "lTitle";
            this.lTitle.Size = new System.Drawing.Size(44, 13);
            this.lTitle.TabIndex = 5;
            this.lTitle.Text = "FTPbox";
            // 
            // lSize
            // 
            this.lSize.BackColor = System.Drawing.Color.Transparent;
            this.lSize.Location = new System.Drawing.Point(82, 13);
            this.lSize.Name = "lSize";
            this.lSize.Size = new System.Drawing.Size(250, 13);
            this.lSize.TabIndex = 6;
            this.lSize.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // fTrayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(344, 301);
            this.ControlBox = false;
            this.Controls.Add(this.lSize);
            this.Controls.Add(this.lTitle);
            this.Controls.Add(this.pSettings);
            this.Controls.Add(this.pLocalFolder);
            this.Controls.Add(this.fRecentList);
            this.Controls.Add(this.lCurrentStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximumSize = new System.Drawing.Size(350, 307);
            this.MinimumSize = new System.Drawing.Size(350, 307);
            this.Name = "fTrayForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Deactivate += new System.EventHandler(this.fTrayForm_Deactivate);
            this.Load += new System.EventHandler(this.fTrayForm_Load);
            this.Leave += new System.EventHandler(this.fTrayForm_Leave);
            ((System.ComponentModel.ISupportInitialize)(this.pLocalFolder)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pSettings)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lCurrentStatus;
        private System.Windows.Forms.FlowLayoutPanel fRecentList;
        private System.Windows.Forms.PictureBox pLocalFolder;
        private System.Windows.Forms.PictureBox pSettings;
        private System.Windows.Forms.Label lTitle;
        private System.Windows.Forms.Label lSize;
    }
}