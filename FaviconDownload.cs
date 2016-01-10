// FaviconDownloader.cs
//
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
    using System;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using KeePassLib;
    using System.Threading.Tasks;
    using System.Threading;

    public delegate void ProgressChangedHandler(FaviconDownload download);
    public class FaviconDownload
    {
        public const string UserAgent = "Mozilla/5.0 (Windows 6.1; rv:27.0) Gecko/20100101 Firefox/27.0";
        public const string AcceptContentType = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

        public event ProgressChangedHandler ProgressChanged;
        public int Progress
        {
            get;
            private set;
        }

        public string Error
        {
            get;
            private set;
        }

        public bool HasError
        {
            get
            {
                return String.IsNullOrEmpty(Error);
            }
        }

        public byte[] Data
        {
            get;
            private set;
        }

        public PwEntry Entry
        {
            get;
            private set;
        }

        public FaviconDownload(PwEntry pwe)
        {
            Entry = pwe;
        }

        static byte[] ProcessResult(byte[] responseData)
        {
            using (var pngBuffer = new MemoryStream())
            {
                using (var responseBuffer = new MemoryStream(responseData))
                {
                    FaviconStreamCodec.Encode(pngBuffer,
                        FaviconStreamCodec.Process(
                            FaviconStreamCodec.Decode(responseBuffer)
                        )
                    );
                }

                return pngBuffer.ToArray();
            }
        }

        static Uri GetFullUri(PwEntry entry)
        {
            Uri fullURI = GetFaviconUri(entry);

            var favicon = new Elmah.Io.FaviconLoader.Favicon();
            return favicon.Load(fullURI);
        }

        static byte[] GetFaviconData(Uri faviconUri)
        {
            WebClient downloadClient = new WebClient();
            downloadClient.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);
            downloadClient.Headers.Add(HttpRequestHeader.AcceptLanguage, "*");
            downloadClient.Headers.Add(HttpRequestHeader.Accept, AcceptContentType);

            return downloadClient.DownloadData(faviconUri);
        }

        public Task<FaviconDownload> DownloadTask(CancellationToken token)
        {
            var task = new Task<FaviconDownload>(() => Download(token), token);

            return task;
        }

        void SetProgress(int progress) {
            Progress = progress;

            if (null != ProgressChanged) {
                ProgressChanged.Invoke(this);
            }
        }
        
        FaviconDownload Download(CancellationToken token)
        {
            Progress = 0;
            if (token.IsCancellationRequested)
            {
                return this;
            }

            Uri faviconUri;
            try
            {
                faviconUri = GetFullUri(Entry);
            }
            catch (Exception ex)
            {
                Error = "Could not get favicon URL: " + ex.Message;
                return this;
            }

            SetProgress(33);
            if (token.IsCancellationRequested)
            {
                return this;
            }

            byte[] data;
            try
            {
                data = GetFaviconData(faviconUri);
            }
            catch (Exception ex)
            {
                Error = "Could not download favicon: " + ex.Message;
                return this;
            }

            SetProgress(66);
            if (token.IsCancellationRequested)
            {
                return this;
            }

            try
            {
                Data = ProcessResult(data);
                Error = "";
            }
            catch (Exception ex)
            {
                Error = "Could not process downloaded favicon: " + ex.Message;
            }

            SetProgress(100);
            return this;
        }

        static Uri GetFaviconUri(PwEntry pwe)
        {
            string url = pwe.Strings.ReadSafe("URL");

            if (string.IsNullOrEmpty(url))
                url = pwe.Strings.ReadSafe("Title");

            // If we still have no URL, quit
            if (string.IsNullOrEmpty(url))
                return null;

            // If we have a URL with specific scheme that is not http or https, quit
            if (!url.StartsWith("http://") && !url.StartsWith("https://")
                && url.Contains("://"))
                return null;

            int dotIndex = url.IndexOf(".");
            return dotIndex >= 0 ? new Uri((url.StartsWith("http://") || url.StartsWith("https://")) ? url : "http://" + url, UriKind.Absolute) : null;
        }

        static class FaviconStreamCodec
        {
            public static Image Decode(Stream responseBuffer)
            {
                Image responseImage;
                try
                {
                    var icon = new Icon(responseBuffer);
                    icon = new Icon(icon, 16, 16);
                    responseImage = icon.ToBitmap();
                }
                catch (Exception)
                {
                    // This shouldn't be useful unless someone has messed up their favicon format
                    try
                    {
                        responseImage = Image.FromStream(responseBuffer);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                return responseImage;
            }

            public static Bitmap Process(Image responseImage)
            {
                var renderBuffer = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(renderBuffer))
                {
                    // set the resize quality modes to high quality
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(responseImage, 0, 0, renderBuffer.Width, renderBuffer.Height);
                }

                return renderBuffer;
            }

            public static void Encode(Stream pngBuffer, Bitmap renderBuffer)
            {
                renderBuffer.Save(pngBuffer, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }

}

