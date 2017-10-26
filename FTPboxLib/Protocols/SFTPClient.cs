/* License
 * This file is part of FTPbox - Copyright (C) 2012-2013 ftpbox.org
 * FTPbox is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published 
 * by the Free Software Foundation, either version 3 of the License, or (at your option) any later version. This program is distributed 
 * in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
 * See the GNU General Public License for more details. You should have received a copy of the GNU General Public License along with this program. 
 * If not, see <http://www.gnu.org/licenses/>.
 */
/* Client.cs
 * The client class handles communication with the server, combining the FTP and SFTP libraries.
 */

// #define __MonoCs__

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using FluentFTP;
using System.ComponentModel;
#if !__MonoCs__
using FileIO = Microsoft.VisualBasic.FileIO;

#endif

namespace FTPboxLib
{
    public class SFTPClient : Client
    {
        public Object ftpcLock = new object();
        public SFTPClient(AccountController account)
        {
            _controller = account;
            _certificates = new X509Certificate2Collection();
        }

        #region Private Fields

        private SftpClient _sftpc; // And our SFTP client

        private bool _reconnecting; // true when client is already attempting to reconnect

        private readonly X509Certificate2Collection _certificates;

        private Timer _tKeepAlive;

        private readonly AccountController _controller;

        private BackgroundWorker connectionState = null;

        #endregion

        #region Public Events

        public override event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        public override event EventHandler ReconnectingFailed;
        public override event EventHandler<ValidateCertificateEventArgs> ValidateCertificate;
        public override event EventHandler<TransferProgressArgs> TransferProgress;

        #endregion

        #region Methods

        /// <summary>
        ///     Connect to the remote servers, with the details from Profile
        /// </summary>
        /// <param name="reconnecting">True if this is an attempt to re-establish a closed connection</param>
        public override void Connect(bool reconnecting = false)
        {
            Notifications.ChangeTrayText(reconnecting ? MessageType.Reconnecting : MessageType.Connecting);
            Log.Write(l.Debug, "{0} client...", reconnecting ? "Reconnecting" : "Connecting");


            lock (ftpcLock)
            {
                ConnectionInfo connectionInfo;
                if (_controller.IsPrivateKeyValid)
                    connectionInfo = new PrivateKeyConnectionInfo(_controller.Account.Host, _controller.Account.Port,
                        _controller.Account.Username,
                        new PrivateKeyFile(_controller.Account.PrivateKeyFile, _controller.Account.Password));
                else
                    connectionInfo = new PasswordConnectionInfo(_controller.Account.Host, _controller.Account.Port,
                        _controller.Account.Username, _controller.Account.Password);

                _sftpc = new SftpClient(connectionInfo);
                _sftpc.ConnectionInfo.AuthenticationBanner += (o, x) => Log.Write(l.Warning, x.BannerMessage);

                _sftpc.HostKeyReceived += (o, x) =>
                {
                    var fingerPrint = x.FingerPrint.GetCertificateData();

                    // if ValidateCertificate handler isn't set, accept the certificate and move on
                    if (ValidateCertificate == null || Settings.TrustedCertificates.Contains(fingerPrint))
                    {
                        Log.Write(l.Client, "Trusted: {0}", fingerPrint);
                        x.CanTrust = true;
                        return;
                    }

                    var e = new ValidateCertificateEventArgs
                    {
                        Fingerprint = fingerPrint,
                        Key = x.HostKeyName,
                        KeySize = x.KeyLength.ToString()
                    };
                    // Prompt user to validate
                    ValidateCertificate(null, e);
                    x.CanTrust = e.IsTrusted;
                };

                _sftpc.Connect();

                _sftpc.ErrorOccurred += (o, e) =>
                {
                    if (!isConnected) Notifications.ChangeTrayText(MessageType.Nothing);
                    if (ConnectionClosed != null)
                        ConnectionClosed(null, new ConnectionClosedEventArgs { Text = e.Exception.Message });

                    if (e.Exception is SftpPermissionDeniedException)
                        Log.Write(l.Warning, "Permission denied error occured");
                    if (e.Exception is SshConnectionException)
                        Reconnect();
                };

            }

            _controller.HomePath = WorkingDirectory;

            if (isConnected)
            {
                if (!string.IsNullOrWhiteSpace(_controller.Paths.Remote) && !_controller.Paths.Remote.Equals("/"))
                    WorkingDirectory = _controller.Paths.Remote;

                if (connectionState == null)
                {
                    connectionState = new BackgroundWorker();
                    connectionState.DoWork += new DoWorkEventHandler((o, e) =>
                    {
                        while (true)
                        {
                            if (!isConnected)
                            {
                                // RECONNECT
                                //_controller.Client.Reconnect();
                            }
                            Thread.Sleep(5000);
                        }
                    });

                    connectionState.RunWorkerAsync();
                }

            }

            Log.Write(l.Debug, "Client connected sucessfully");
            Notifications.ChangeTrayText(MessageType.Ready);

            if (Settings.IsDebugMode)
                LogServerInfo();

            // Periodically send NOOP (KeepAlive) to server if a non-zero interval is set            
            SetKeepAlive();
        }

        /// <summary>
        ///     Attempt to reconnect to the server. Called when connection has closed.
        /// </summary>
        public override void Reconnect()
        {
            if (_reconnecting) return;
            try
            {
                _reconnecting = true;
                Connect();
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
                Notifications.ChangeTrayText(MessageType.Disconnected);
                ReconnectingFailed.SafeInvoke(null, EventArgs.Empty);
            }
            finally
            {
                _reconnecting = false;
            }
        }

        /// <summary>
        ///     Close connection to server
        /// </summary>
        public override void Disconnect()
        {
            lock (ftpcLock)
            {
                _sftpc.Disconnect();
            }
        }

        /// <summary>
        ///     Keep the connection to the server alive by sending the NOOP command
        /// </summary>
        private void SendNoOp()
        {
            if (_controller.SyncQueue.sync.IsBusy) return;

            try
            {
                Console.WriteLine("NOOP");

                lock (ftpcLock)
                {
                    _sftpc.SendKeepAlive();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
                Reconnect();
            }
        }

        /// <summary>
        ///     Set a timer that will periodically send the NOOP
        ///     command to the server if a non-zero interval is set
        /// </summary>
        public void SetKeepAlive()
        {
            // Dispose the existing timer
            UnsetKeepAlive();

            if (_tKeepAlive == null) _tKeepAlive = new Timer(state => SendNoOp());

            if (_controller.Account.KeepAliveInterval > 0)
                _tKeepAlive.Change(1000 * 10, 1000 * _controller.Account.KeepAliveInterval);
        }

        /// <summary>
        ///     Dispose the existing KeepAlive timer
        /// </summary>
        public void UnsetKeepAlive()
        {
            if (_tKeepAlive != null) _tKeepAlive.Change(0, 0);
        }

        /// <summary>
        ///     Upload to a temporary file.
        ///     If the transfer is successful, replace the old file with the temporary one.
        ///     If not, delete the temporary file.
        /// </summary>
        /// <param name="i">The item to upload</param>
        /// <returns>TransferStatus.Success on success, TransferStatus.Success on failure</returns>
        public override TransferStatus SafeUpload(SyncQueueItem i)
        {
            // is this the first time we check the files?
            //if (_controller.FileLog.IsEmpty())
            //{
            //TODO: allow user to select if the following should happen
            // skip synchronization if the file already exists and has the exact same size
            if (Exists(i.CommonPath) && SizeOf(i.CommonPath) == i.Item.Size)
            {
                Log.Write(l.Client, "File seems to be already synced (skipping): {0}", i.CommonPath);
                return TransferStatus.Success;
            }
            //}

            Notifications.ChangeTrayText(MessageType.Uploading, i.Item.Name);
            var temp = Common._tempName(i.CommonPath, _controller.Account.TempFilePrefix);

            try
            {
                var startedOn = DateTime.Now;
                long transfered = 0;
                // upload to a temp file...

                using (var file = File.Open(i.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    lock (ftpcLock)
                    {
                        _sftpc.UploadFile(file, temp, true,
                        d =>
                        {
                            ReportTransferProgress(new TransferProgressArgs((long)d - transfered, (long)d, i,
                                startedOn));
                            transfered = (long)d;
                        });

                        Notifications.ChangeTrayText(MessageType.Size, null, i.Item.Size);
                    }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
                return TransferStatus.Failure;
            }

            Thread.Sleep(2000); //wait syncing

            long size = SizeOf(temp);

            if (i.Item.Size == size)
            {
                if (Exists(i.CommonPath)) Remove(i.CommonPath);
                Rename(temp, i.CommonPath);

                return TransferStatus.Success;
            }
            Remove(temp);
            return TransferStatus.Failure;
        }

        public void Download(string cpath, string lpath)
        {

            using (var f = new FileStream(lpath, FileMode.Create, FileAccess.ReadWrite))
                lock (ftpcLock)
                {
                    _sftpc.DownloadFile(cpath, f);
                }
        }

        /// <summary>
        ///     Download to a temporary file.
        ///     If the transfer is successful, replace the old file with the temporary one.
        ///     If not, delete the temporary file.
        /// </summary>
        /// <param name="i">The item to download</param>
        /// <returns>TransferStatus.Success on success, TransferStatus.Success on failure</returns>
        public override TransferStatus SafeDownload(SyncQueueItem i)
        {
            Notifications.ChangeTrayText(MessageType.Downloading, i.Item.Name);
            var temp = Common._tempLocal(i.LocalPath, _controller.Account.TempFilePrefix);
            try
            {
                var startedOn = DateTime.Now;
                long transfered = 0;
                // download to a temp file...

                using (var f = new FileStream(temp, FileMode.Create, FileAccess.ReadWrite))
                    lock (ftpcLock)
                    {
                        _sftpc.DownloadFile(i.CommonPath, f,
                        d =>
                        {
                            ReportTransferProgress(new TransferProgressArgs((long)d - transfered, (long)d, i,
                                startedOn));
                            transfered = (long)d;
                        });
                    }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
                goto Finish;
            }

            if (i.Item.Size == new FileInfo(temp).Length)
            {
                _controller.FolderWatcher.Pause(); // Pause Watchers
                if (File.Exists(i.LocalPath))
#if __MonoCs__
                    File.Delete(i.LocalPath);
#else
                    FileIO.FileSystem.DeleteFile(i.LocalPath, FileIO.UIOption.OnlyErrorDialogs,
                        FileIO.RecycleOption.SendToRecycleBin);
#endif
                File.Move(temp, i.LocalPath);
                _controller.FolderWatcher.Resume(); // Resume Watchers
                return TransferStatus.Success;
            }

            Finish:
            if (File.Exists(temp))
#if __MonoCs__
                File.Delete(temp);
#else
                FileIO.FileSystem.DeleteFile(temp, FileIO.UIOption.OnlyErrorDialogs,
                    FileIO.RecycleOption.SendToRecycleBin);
#endif
            return TransferStatus.Failure;
        }

        public override void Rename(string oldname, string newname)
        {

            lock (ftpcLock)
            {
                _sftpc.RenameFile(oldname, newname);
            }
        }

        public override void MakeFolder(string cpath)
        {
            try
            {

                lock (ftpcLock)
                {
                    _sftpc.CreateDirectory(cpath);
                }
            }
            catch
            {
                if (!Exists(cpath)) throw;
            }
        }

        /// <summary>
        ///     Delete a file
        /// </summary>
        /// <param name="cpath">Path to the file</param>
        public override void Remove(string cpath)
        {

            lock (ftpcLock)
            {
                var removedSpace = SizeOf(cpath);
                _sftpc.Delete(cpath);
                Notifications.ChangeTrayText(MessageType.Size, null, -1 * removedSpace);
            }
        }

        /// <summary>
        ///     Delete a remote folder and everything inside it
        /// </summary>
        /// <param name="path">Path to folder that will be deleted</param>
        /// <param name="skipIgnored">if true, files that are normally ignored will not be deleted</param>
        public void RemoveFolder(string path, bool skipIgnored = true)
        {


            if (!Exists(path)) return;

            Log.Write(l.Client, "About to delete: {0}", path);
            // Empty the folder before deleting it
            // List is reversed to delete an files before their parent folders
            foreach (var i in ListRecursive(path, skipIgnored).Reverse())
            {
                Console.Write("\r Removing: {0,50}", i.FullPath);
                if (i.Type == ClientItemType.File)
                    Remove(i.FullPath);
                else
                {

                    lock (ftpcLock)
                    {
                        _sftpc.DeleteDirectory(i.FullPath);
                    }
                }
            }

            lock (ftpcLock)
            {
                _sftpc.DeleteDirectory(path);
            }

            Log.Write(l.Client, "Deleted: {0}", path);
        }

        /// <summary>
        ///     Make sure that our client's working directory is set to the user-selected Remote Path.
        ///     If a previous operation failed and the working directory wasn't properly restored, this will prevent further
        ///     issues.
        /// </summary>
        /// <returns>false if changing to RemotePath fails, true in any other case</returns>
        internal override bool CheckWorkingDirectory()
        {
            try
            {
                var cd = WorkingDirectory;
                if (cd != _controller.Paths.Remote)
                {
                    Log.Write(l.Warning, "pwd is: {0} should be: {1}", cd, _controller.Paths.Remote);
                    WorkingDirectory = _controller.Paths.Remote;
                }
                return true;
            }
            catch (Exception ex)
            {
                if (!isConnected) Log.Write(l.Warning, "Client not connected!");
                Common.LogError(ex);
                return false;
            }
        }

        /// <summary>
        ///     Throttle the file transfer if speed limits apply.
        /// </summary>
        /// <param name="limit">The download or upload rate to limit to, in kB/s.</param>
        /// <param name="transfered">bytes already transferred.</param>
        /// <param name="startedOn">when did the transfer start.</param>
        private void ThrottleTransfer(int limit, long transfered, DateTime startedOn)
        {
            var elapsed = DateTime.Now.Subtract(startedOn);
            var rate = (int)(elapsed.TotalSeconds < 1 ? transfered : transfered / elapsed.TotalSeconds);
            if (limit > 0 && rate > 1000 * limit)
            {
                double millisecDelay = (transfered / limit - elapsed.TotalMilliseconds);

                if (millisecDelay > Int32.MaxValue)
                    millisecDelay = Int32.MaxValue;

                Thread.Sleep((int)millisecDelay);
            }
        }

        /// <summary>
        ///     Displays some server info in the log/console
        /// </summary>
        public void LogServerInfo()
        {
            Log.Write(l.Client, "////////////////////Server Info///////////////////");

            Log.Write(l.Client, "Protocol Version: {0}", _sftpc.ProtocolVersion);
            Log.Write(l.Client, "Client Compression Algorithm: {0}",
                _sftpc.ConnectionInfo.CurrentClientCompressionAlgorithm);
            Log.Write(l.Client, "Server Compression Algorithm: {0}",
                _sftpc.ConnectionInfo.CurrentServerCompressionAlgorithm);
            Log.Write(l.Client, "Client encryption: {0}", _sftpc.ConnectionInfo.CurrentClientEncryption);
            Log.Write(l.Client, "Server encryption: {0}", _sftpc.ConnectionInfo.CurrentServerEncryption);


            Log.Write(l.Client, "//////////////////////////////////////////////////");
        }

        /// <summary>
        ///     Safely invoke TransferProgress.
        /// </summary>
        private void ReportTransferProgress(TransferProgressArgs e)
        {
            if (TransferProgress != null)
                TransferProgress(null, e);
        }

        /// <summary>
        ///     Returns the file size of the file in the given bath, in both SFTP and FTP
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <returns>The file's size</returns>
        public override long SizeOf(string path)
        {

            long size = -1;

            lock (ftpcLock)
            {
                size = _sftpc.GetAttributes(path).Size;
            }

            return size;
        }

        /// <summary>
        ///     Does the specified path exist on the remote folder?
        /// </summary>
        public override bool Exists(string cpath)
        {

            bool exists = false;
            lock (ftpcLock)
            {
                exists = _sftpc.Exists(cpath);
            }
            return exists;

        }

        /// <summary>
        ///     Returns the LastWriteTime of the specified file/folder
        /// </summary>
        /// <param name="path">The common path to the file/folder</param>
        /// <returns></returns>
        public override DateTime GetLwtOf(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return DateTime.MinValue;

            if (path.StartsWith("/")) path = path.Substring(1);
            var dt = DateTime.MinValue;

            try
            {
                lock (ftpcLock)
                {
                    dt = _sftpc.GetLastWriteTime(path);
                }
            }
            catch (Exception ex)
            {
                Log.Write(l.Client, "===> {0} is a folder", path);
                Common.LogError(ex);
            }

            DateTime tmp = DateTime.MinValue;
            lock (ftpcLock)
            {
                try
                {
                    tmp = _sftpc.GetLastAccessTimeUtc(path);
                }
                catch (Exception ex)
                {
                    Log.Write(l.Client, "===> some files/directory changed");
                    Common.LogError(ex);
                }
            }
            Log.Write(l.Client, "Got LWT: {0} UTC: {1}", dt, tmp);

            return dt;
        }

        /// <summary>
        ///     Convert SftpFile to ClientItemType
        /// </summary>
        private ClientItemType _ItemTypeOf(SftpFile f)
        {
            if (f.IsDirectory)
                return ClientItemType.Folder;
            if (f.IsRegularFile)
                return ClientItemType.File;
            return ClientItemType.Other;
        }

        /// <summary>
        ///     Convert FtpFileSystemObjectType to ClientItemType
        /// </summary>
        private ClientItemType _ItemTypeOf(FtpFileSystemObjectType f)
        {
            if (f == FtpFileSystemObjectType.File)
                return ClientItemType.File;
            if (f == FtpFileSystemObjectType.Directory)
                return ClientItemType.Folder;
            return ClientItemType.Other;
        }

        #endregion

        #region Properties


        public override bool isConnected
        {
            get { return _sftpc.IsConnected; }
        }

        public override bool ListingFailed { get; set; }

        public override string WorkingDirectory
        {
            get
            {
                return _sftpc.WorkingDirectory;
            }
            set
            {
                lock (ftpcLock)
                {
                    _sftpc.ChangeDirectory(value);
                }
                Log.Write(l.Client, "cd {0}", value);
            }
        }

        #endregion

        #region Listing

        /// <summary>
        ///     Returns a non-recursive list of files/folders inside the specified path
        /// </summary>
        /// <param name="cpath">path to folder to list inside</param>
        /// <param name="skipIgnored">if true, ignored items are not returned</param>
        public override IEnumerable<ClientItem> List(string cpath, bool skipIgnored = true)
        {
            ListingFailed = false;
            UnsetKeepAlive();

            List<ClientItem> list = new List<ClientItem>();

            lock (ftpcLock)
            {
                bool ok = false;

                while (!ok)
                {
                    try
                    {
                        if (_controller.Client.WorkingDirectory.CompareTo(_controller.Paths.Remote) != 0)
                            _controller.Client.WorkingDirectory = _controller.Paths.Remote;

                        Notifications.ChangeTrayText(MessageType.Scanning, null, 0, cpath);
                        var Listed = _sftpc.ListDirectory(cpath);
                        list = Array.ConvertAll(new List<SftpFile>(Listed).ToArray(), ConvertItem).ToList();
                        ok = true;
                    }
                    catch (TimeoutException ex)
                    {
                        Common.LogError(ex);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex);
                        yield break;
                    }
                }
            }

            list.RemoveAll(x => x.Name == "." || x.Name == "..");

            foreach (var f in list.Where(x => x.Type != ClientItemType.Other))
                yield return f;

            SetKeepAlive();
        }

        /// <summary>
        ///     Get a full list of files/folders inside the specified path
        /// </summary>
        /// <param name="cpath">path to folder to list inside</param>
        /// <param name="skipIgnored">if true, ignored items are not returned</param>
        public override IEnumerable<ClientItem> ListRecursive(string cpath, bool skipIgnored = true)
        {
            var list = new List<ClientItem>(List(cpath, skipIgnored).ToList());
            if (ListingFailed) yield break;

            if (skipIgnored)
                list.RemoveAll(x => !_controller.ItemGetsSynced(x.FullPath, false));

            foreach (var f in list.Where(x => x.Type == ClientItemType.File))
                yield return f;

            foreach (var d in list.Where(x => x.Type == ClientItemType.Folder))
                foreach (var f in ListRecursiveInside(d, skipIgnored))
                    yield return f;
        }

        /// <summary>
        ///     Returns a fully recursive listing inside the specified (directory) item
        /// </summary>
        /// <param name="p">The clientItem (should be of type directory) to list inside</param>
        /// <param name="skipIgnored">if true, ignored items are not returned</param>
        private IEnumerable<ClientItem> ListRecursiveInside(ClientItem p, bool skipIgnored = true)
        {
            yield return p;

            var cpath = _controller.GetCommonPath(p.FullPath, false);

            var list = new List<ClientItem>(List(cpath, skipIgnored).ToList());
            if (ListingFailed) yield break;

            if (skipIgnored)
                list.RemoveAll(x => !_controller.ItemGetsSynced(x.FullPath, false));

            foreach (var f in list.Where(x => x.Type == ClientItemType.File))
                yield return f;

            foreach (var d in list.Where(x => x.Type == ClientItemType.Folder))
                foreach (var f in ListRecursiveInside(d, skipIgnored))
                    yield return f;
        }

        /// <summary>
        ///     Convert an FtpItem to a ClientItem
        /// </summary>
        private ClientItem ConvertItem(FtpListItem f)
        {
            var fullPath = f.FullName;
            if (fullPath.StartsWith("./"))
            {
                var cwd = WorkingDirectory;
                var wd = (_controller.Paths.Remote != null && cwd.StartsWithButNotEqual(_controller.Paths.Remote) &&
                          cwd != "/")
                    ? cwd
                    : _controller.GetCommonPath(cwd, false);
                fullPath = fullPath.Substring(2);
                if (wd != "/")
                    fullPath = string.Format("/{0}/{1}", wd, fullPath);
                fullPath = fullPath.Replace("//", "/");
            }

            return new ClientItem
            {
                Name = f.Name,
                FullPath = fullPath,
                Type = _ItemTypeOf(f.Type),
                Size = f.Size,
                LastWriteTime = f.Modified
            };
        }

        /// <summary>
        ///     Convert an SftpFile to a ClientItem
        /// </summary>
        private ClientItem ConvertItem(SftpFile f)
        {
            return new ClientItem
            {
                Name = f.Name,
                FullPath = _controller.GetCommonPath(f.FullName, false),
                Type = _ItemTypeOf(f),
                Size = f.Attributes.Size,
                LastWriteTime = f.LastWriteTime
            };
        }

        #endregion
    }
}