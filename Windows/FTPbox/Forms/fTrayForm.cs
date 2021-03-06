﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FTPboxLib;

namespace FTPbox.Forms
{
    public partial class fTrayForm : Form
    {
        /// <summary>
        ///     Contains the space occupied on the remote server
        /// </summary>
        private long remoteTotalSize = 0;

        /// <summary>
        ///     This item will be added to the recent list when a file transfer is in progress
        /// </summary>
        private readonly trayFormListItem _transferItem = new trayFormListItem();

        /// <summary>
        ///     the latest status used to set the status label
        /// </summary>
        private TrayTextNotificationArgs _lastStatus = new TrayTextNotificationArgs
        {
            AssossiatedFile = null,
            sizeValue = 0,
            customString = null,
            MessageType = MessageType.AllSynced
        };

        public fTrayForm()
        {
            InitializeComponent();

            Notifications.TrayTextNotification += (o, n) =>
            {
                if (IsHandleCreated)
                    Invoke(new MethodInvoker(() => SetStatusLabel(o, n)));
                else
                    _lastStatus = n;
            };
            // Make status label same color as the icons
            lCurrentStatus.ForeColor = Color.FromArgb(105, 105, 105);
            lTitle.ForeColor = Color.FromArgb(105, 105, 105);
            lSize.ForeColor = Color.FromArgb(105, 105, 105);
        }

        public class NewProgressBar : ProgressBar
        {
            public NewProgressBar()
            {
                this.SetStyle(ControlStyles.UserPaint, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Rectangle rec = e.ClipRectangle;

                rec.Width = (int)(rec.Width * ((double)Value / Maximum));
                if (ProgressBarRenderer.IsSupported)
                    ProgressBarRenderer.DrawHorizontalBar(e.Graphics, e.ClipRectangle);
                rec.Height = rec.Height;
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0,153,255)), 0, 0, rec.Width, rec.Height);
            }
        }

        private NewProgressBar progressBar1;

        private void fTrayForm_Load(object sender, EventArgs e)
        {
            // Make sure the border doesn't appear
            Text = string.Empty;

            progressBar1 = new NewProgressBar();
            //progressBar1.Dock = DockStyle.Top;
            progressBar1.Height = 10;
            progressBar1.Width = 344;
            progressBar1.Margin = new Padding(0);
            progressBar1.Location = new Point(0, 249);
            progressBar1.Visible = false;
            this.Controls.Add(progressBar1);

            Notifications.RecentListChanged += (o, n) => Invoke(new MethodInvoker(LoadRecent));

            Program.Account.Client.TransferProgress += (o, n) =>
            {
                // Only when Downloading/Uploading.
                if (string.IsNullOrWhiteSpace(_lastStatus.AssossiatedFile)) return;
                // Update item in recent list
                Invoke(new MethodInvoker(() =>
                {
                    // Get status progress for the transfer
                    
                    var progress = string.Format("{2} / {3} - {0,3}% - {1} ", n.Progress, n.Rate, TransferProgressArgs.ConvertSize(n.TotalTransferred), TransferProgressArgs.ConvertSize(n.Item.Item.Size));

                    _transferItem.FileStatusLabel = string.Format(_transferItem.SubTitleFormat, progress);

                    if (fRecentList.Height != 209)
                    {
                        fRecentList.Height = 209;
                        progressBar1.Visible = true;
                    }
                    
                    progressBar1.Value = n.Progress;

                    if (n.Progress == 100)
                    {
                        progressBar1.Visible = false;
                        fRecentList.Height = 219;
                    }
                }));
            };
            // Set the status label and load the recent files
            SetStatusLabel(null, _lastStatus);
            LoadRecent();
        }

        /// <summary>
        ///     Load the recent items list
        /// </summary>
        private void LoadRecent()
        {
            var list = new List<FileLogItem>(Program.Account.RecentList);
            // Update the Recent List
            fRecentList.Controls.Clear();
            list.ForEach(f =>
            {
                var fullPath = Path.GetFullPath(Path.Combine(Program.Account.Paths.Local, f.CommonPath));
                var t = new trayFormListItem
                {
                    FileNameLabel = Common._name(f.CommonPath),
                    FileStatusLabel = Common.Languages[UiControl.Modified] + " " + f.LatestChangeTime().FormatDate()
                };
                // Open the file in explorer.exe on click
                t.Click += (sender, args) => Process.Start("explorer.exe", @"/select, " + fullPath);
                fRecentList.Controls.Add(t);
            });
            if (list.Count == 0)
            {
                // If recent list is empty, just add a single 'Not Available' item
                fRecentList.Controls.Add(new trayFormListItem
                {
                    FileNameLabel = Common.Languages[MessageType.NotAvailable],
                    FileStatusLabel = string.Empty
                });
            }
        }

        public void SetStatusLabel(object o, TrayTextNotificationArgs e)
        {
            try
            {
                // Save latest status
                _lastStatus = e;

                if (!string.IsNullOrWhiteSpace(e.AssossiatedFile))
                {
                    var name = Common._name(e.AssossiatedFile);

                    var format = Common.Languages[e.MessageType];

                    _transferItem.FileNameLabel = name;
                    _transferItem.SubTitleFormat = format;
                    _transferItem.FileStatusLabel = string.Format(format, string.Empty);
                    // Add to top of recent list
                    fRecentList.Controls.Add(_transferItem);
                    fRecentList.Controls.SetChildIndex(_transferItem, 0);
                }

                switch (e.MessageType)
                {
                    case MessageType.Uploading:
                    case MessageType.Downloading:
                        lCurrentStatus.Text = Common.Languages[MessageType.Syncing];
                        break;
                    case MessageType.Listing:
                        lCurrentStatus.Text = (Program.Account.Account.SyncMethod == SyncMethod.Automatic)
                            ? Common.Languages[MessageType.AllSynced]
                            : Common.Languages[MessageType.Listing];
                        break;
                    case MessageType.Size:
                        remoteTotalSize += e.sizeValue;
                        if (remoteTotalSize < 0) remoteTotalSize = 0;
                        lSize.Text = Common.Languages[MessageType.Size] + " " + TransferProgressArgs.ConvertSize(remoteTotalSize);
                        break;
                    case MessageType.NullSize:
                        remoteTotalSize = 0;
                        lSize.Text = "";
                        break;
                    case MessageType.Scanning:
                        if (e.customString.Length > 40)
                            e.customString = "..." + e.customString.Substring(e.customString.Length-40, 40);
                        lSize.Text = e.customString;
                        break;
                    default:
                        lCurrentStatus.Text = Common.Languages[e.MessageType];
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        private void fTrayForm_Leave(object sender, EventArgs e)
        {
            Hide();
        }

        private void fTrayForm_Deactivate(object sender, EventArgs e)
        {
            Hide();
        }

        /// <summary>
        ///     Positions the form according to the location of the Windows taskbar and our tray icon
        /// </summary>
        /// <param name="MousePosition">the point the user clicked at when opening the form</param>
        public void PositionProperly(Point MousePosition)
        {
            // Get the taskbar location and type
            Win32.AppBarLocation taskbarType;
            var taskbarRectangle = Win32.GetTaskbar(out taskbarType);

            var x = 0;
            var y = 0;

            // Calculate where the form should be placed
            if (taskbarType == Win32.AppBarLocation.Bottom)
            {
                x = MousePosition.X - Width/2;
                y = taskbarRectangle.Y - Height - 10;
            }
            else if (taskbarType == Win32.AppBarLocation.Top)
            {
                x = MousePosition.X - Width/2;
                y = taskbarRectangle.Height + 10;
            }
            else if (taskbarType == Win32.AppBarLocation.Left)
            {
                x = taskbarRectangle.X + taskbarRectangle.Width + 10;
                y = MousePosition.Y - Height/2;
            }
            else if (taskbarType == Win32.AppBarLocation.Right)
            {
                x = taskbarRectangle.X - Width - 10;
                y = MousePosition.Y - Height/2;
            }
            // Make sure the form does not get outside the screen bounds
            if (taskbarType == Win32.AppBarLocation.Bottom || taskbarType == Win32.AppBarLocation.Top)
            {
                if (x + Width > taskbarRectangle.Right - 10)
                    x = taskbarRectangle.Right - 10 - Width;
            }
            else
            {
                if (y + Height > taskbarRectangle.Bottom - 10)
                    y = taskbarRectangle.Bottom - 10 - Height;
            }
            // Set the location
            Location = new Point(x, y);
        }

        private void pLocalFolder_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Program.Account.Paths.Local);
        }

        private void pSettings_Click(object sender, EventArgs e)
        {
            ((fMain) Tag).Show();
            ((fMain) Tag).Activate();
        }

        public void Set_Language()
        {
            if (!IsHandleCreated) return;

            Invoke(new MethodInvoker(() =>
            {
                // Set the status label and load the recent files
                SetStatusLabel(null, _lastStatus);
                LoadRecent();
            }));
        }
    }
}