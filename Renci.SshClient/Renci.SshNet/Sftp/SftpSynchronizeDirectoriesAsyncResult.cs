﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Common;
using System.IO;

namespace Renci.SshNet.Sftp
{    
    public class SftpSynchronizeDirectoriesAsyncResult : AsyncResult<IEnumerable<FileInfo>>
    {
        /// <summary>
        /// Gets the number of files read so far.
        /// </summary>
        public int FilesRead { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpListDirectoryAsyncResult"/> class.
        /// </summary>
        /// <param name="asyncCallback">The async callback.</param>
        /// <param name="state">The state.</param>
        public SftpSynchronizeDirectoriesAsyncResult(AsyncCallback asyncCallback, Object state)
            : base(asyncCallback, state)
        {

        }

        /// <summary>
        /// Updates asynchronous operation status information.
        /// </summary>
        /// <param name="filesRead">The files read.</param>
        internal void Update(int filesRead)
        {
            this.FilesRead = filesRead;
        }
    }
}
