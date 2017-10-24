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
    public abstract class Client
    {
        public virtual event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        public virtual event EventHandler ReconnectingFailed;
        public virtual event EventHandler<ValidateCertificateEventArgs> ValidateCertificate;
        public virtual event EventHandler<TransferProgressArgs> TransferProgress;

        public virtual bool ListingFailed { get; set; }
        public virtual bool isConnected { get; }
        public virtual string WorkingDirectory { get; set; }

        internal virtual bool CheckWorkingDirectory()
        {
            throw new NotImplementedException();
        }

        public virtual void Connect(bool reconnecting = false)
        {
            throw new NotImplementedException();
        }

        public virtual void Disconnect()
        {
            throw new NotImplementedException();
        }

        public virtual bool Exists(string cp)
        {
            throw new NotImplementedException();
        }

        public virtual DateTime GetLwtOf(string commonPath)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<ClientItem> List(string cpath, bool skipIgnored = true)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<ClientItem> ListRecursive(string cpath, bool skipIgnored = true)
        {
            throw new NotImplementedException();
        }

        public virtual void MakeFolder(string commonPath)
        {
            throw new NotImplementedException();
        }

        public virtual void Reconnect()
        {
            throw new NotImplementedException();
        }

        public virtual void Remove(string commonPath)
        {
            throw new NotImplementedException();
        }

        internal virtual void RemoveFolder(string commonPath)
        {
            throw new NotImplementedException();
        }

        public virtual void Rename(string commonPath, string newCommonPath)
        {
            throw new NotImplementedException();
        }

        public virtual TransferStatus SafeDownload(SyncQueueItem sqi)
        {
            throw new NotImplementedException();
        }

        public virtual TransferStatus SafeUpload(SyncQueueItem item)
        {
            throw new NotImplementedException();
        }

        public virtual long SizeOf(string cpath)
        {
            throw new NotImplementedException();
        }
    }
}