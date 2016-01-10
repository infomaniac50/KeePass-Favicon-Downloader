// Author:
//       Derek Chafin <infomaniac50@gmail.com>
//
// Copyright (c) 2016 Derek Chafin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace KeePassFaviconDownloader
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using KeePass.Forms;
    using KeePass.Plugins;
    using KeePassLib;

    //
    // Favicons.cs
    public class Favicons
    {
        List<ErrorMessage> errorList;
        List<Task<FaviconDownload>> taskList;
        StatusProgressForm progressForm;
        float overallProgress;
        readonly IPluginHost m_host;

        public Favicons(IPluginHost m_host)
        {
            this.m_host = m_host;
            progressForm = new StatusProgressForm();

            progressForm.InitEx("Downloading Favicons", true, false, m_host.MainWindow);
        }

        public struct ErrorMessage
        {
            public ErrorMessage(string url, string message)
            {
                Url = url;
                Message = message;
            }

            public readonly string Message;
            public readonly string Url;
        }

        void OnProgressChanged(FaviconDownload download) {
            
        }

        public async void DownloadAll(KeePassLib.Collections.PwObjectList<PwEntry> entries)
        {
            progressForm.Show();
            progressForm.SetProgress(0);

            errorList = new List<ErrorMessage>();
            taskList = new List<Task<FaviconDownload>>();
            var cancelTokenSource = new CancellationTokenSource();

            foreach (PwEntry pwe in entries)
            {
                var faviconDownload = new FaviconDownload(pwe);
                faviconDownload.ProgressChanged += OnProgressChanged;
                taskList.Add(faviconDownload.DownloadTask(cancelTokenSource.Token));
            }

            foreach (Task<FaviconDownload> downloadTask in taskList)
            {
                if (progressForm.UserCancelled)
                {
                    cancelTokenSource.Cancel();
                    break;
                }

                FaviconDownload faviconDownloader = await downloadTask;

                if (faviconDownloader.HasError)
                {
                    errorList.Add(new ErrorMessage(
                            faviconDownloader.Entry.Strings.ReadSafe("URL"),
                            faviconDownloader.Error
                        ));
                }
                else
                {
                    DownloadComplete(faviconDownloader);
                }
            }

            progressForm.Hide();
            progressForm.Close();

            m_host.MainWindow.UpdateUI(false, null, false, null,
                true, null, true);
            m_host.MainWindow.UpdateTrayIcon();
        }

        public void ShowErrors() {
            //            if (errorMessage != "")
            //            {
            //                if (errorCount == 1)
            //                    MessageBox.Show(errorMessage, "Download error");
            //                else
            //                    MessageBox.Show(errorCount + " errors occurred. The last error message is shown here. To see the other messages, select a smaller group of entries and use the right click menu to start the download.\n" + errorMessage, "Download errors");
            //            }
        }

        public void DownloadComplete(FaviconDownload faviconDownloader)
        {
            PwCustomIcon icon = GetExistingOrNew(faviconDownloader.Data);

            PwEntry entry = faviconDownloader.Entry;
            entry.CustomIconUuid = icon.Uuid;
            entry.Touch(true);
            m_host.Database.UINeedsIconUpdate = true;
        }

        PwCustomIcon GetExistingOrNew(byte[] data)
        {
            foreach (PwCustomIcon item in m_host.Database.CustomIcons)
            {
                // re-use existing custom icon if it's already in the database
                // (This will probably fail if database is used on 
                // both 32 bit and 64 bit machines - not sure why...)
                if (KeePassLib.Utility.MemUtil.ArraysEqual(data, item.ImageDataPng))
                {
                    return item;
                }
            }

            // Create a new custom icon for use with this entry
            var icon = new PwCustomIcon(new PwUuid(true), data);
            m_host.Database.CustomIcons.Add(icon);
            return icon;
        }
    }
}

