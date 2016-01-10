/*
  KeePass Favicon Downloader - KeePass plugin that downloads and stores
  favicons for entries with web URLs.
  Copyright (C) 2009-2014 Chris Tomlinson <luckyrat@users.sourceforge.net>
  Thanks to mausoma and psproduction for their contributions

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 
  Uses HtmlAgilityPack under MS-PL license: http://htmlagilitypack.codeplex.com/
*/

using System;
using System.Diagnostics;
using System.Windows.Forms;
using KeePass.Plugins;
using KeePassLib;

namespace KeePassFaviconDownloader
{
    public sealed partial class KeePassFaviconDownloaderExt : Plugin
    {
        // The plugin remembers its host in this variable.
        IPluginHost m_host;

        ToolStripSeparator m_tsSeparator1;
        ToolStripSeparator m_tsSeparator2;
        ToolStripSeparator m_tsSeparator3;
        ToolStripMenuItem menuDownloadFavicons;
        ToolStripMenuItem menuDownloadGroupFavicons;
        ToolStripMenuItem menuDownloadEntryFavicons;

        public override string UpdateUrl
        {
            get { return "https://raw.github.com/luckyrat/KeePass-Favicon-Downloader/master/versionInfo.txt"; }
        }

        /// <summary>
        /// Initializes the plugin using the specified KeePass host.
        /// </summary>
        /// <param name="host">The plugin host.</param>
        /// <returns></returns>
        public override bool Initialize(IPluginHost host)
        {
            Debug.Assert(host != null);
            if (host == null)
                return false;
            m_host = host;

            // Add a seperator and menu item to the 'Tools' menu
            ToolStripItemCollection tsMenu = m_host.MainWindow.ToolsMenu.DropDownItems;
            m_tsSeparator1 = new ToolStripSeparator();
            tsMenu.Add(m_tsSeparator1);
            menuDownloadFavicons = new ToolStripMenuItem();
            menuDownloadFavicons.Text = "Download Favicons for all entries";
            menuDownloadFavicons.Click += OnMenuDownloadFavicons;
            tsMenu.Add(menuDownloadFavicons);

            // Add a seperator and menu item to the group context menu
            ContextMenuStrip gcm = m_host.MainWindow.GroupContextMenu;
            m_tsSeparator2 = new ToolStripSeparator();
            gcm.Items.Add(m_tsSeparator2);
            menuDownloadGroupFavicons = new ToolStripMenuItem();
            menuDownloadGroupFavicons.Text = "Download Favicons";
            menuDownloadGroupFavicons.Click += OnMenuDownloadGroupFavicons;
            gcm.Items.Add(menuDownloadGroupFavicons);

            // Add a seperator and menu item to the entry context menu
            ContextMenuStrip ecm = m_host.MainWindow.EntryContextMenu;
            m_tsSeparator3 = new ToolStripSeparator();
            ecm.Items.Add(m_tsSeparator3);
            menuDownloadEntryFavicons = new ToolStripMenuItem();
            menuDownloadEntryFavicons.Text = "Download Favicons";
            menuDownloadEntryFavicons.Click += OnMenuDownloadEntryFavicons;
            ecm.Items.Add(menuDownloadEntryFavicons);

            return true; // Initialization successful
        }

        /// <summary>
        /// Terminates this instance.
        /// </summary>
        public override void Terminate()
        {
            // Remove 'Tools' menu items
            ToolStripItemCollection tsMenu = m_host.MainWindow.ToolsMenu.DropDownItems;
            tsMenu.Remove(m_tsSeparator1);
            tsMenu.Remove(menuDownloadFavicons);

            // Remove group context menu items
            ContextMenuStrip gcm = m_host.MainWindow.GroupContextMenu;
            gcm.Items.Remove(m_tsSeparator2);
            gcm.Items.Remove(menuDownloadGroupFavicons);

            // Remove entry context menu items
            ContextMenuStrip ecm = m_host.MainWindow.EntryContextMenu;
            ecm.Items.Remove(m_tsSeparator3);
            ecm.Items.Remove(menuDownloadEntryFavicons);
        }

        /// <summary>
        /// Downloads favicons for every entry in the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnMenuDownloadFavicons(object sender, EventArgs e)
        {
            if (!m_host.Database.IsOpen)
            {
                MessageBox.Show("Please open a database first.", "Favicon downloader");
                return;
            }

            KeePassLib.Collections.PwObjectList<PwEntry> output;
            output = m_host.Database.RootGroup.GetEntries(true);
            downloadSomeFavicons(output);
        }

        /// <summary>
        /// Downloads favicons for every entry in the selected groups
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnMenuDownloadGroupFavicons(object sender, EventArgs e)
        {
            PwGroup pg = m_host.MainWindow.GetSelectedGroup();
            Debug.Assert(pg != null);
            if (pg == null)
                return;
            downloadSomeFavicons(pg.Entries);
        }

        /// <summary>
        /// Downloads favicons for every selected entry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnMenuDownloadEntryFavicons(object sender, EventArgs e)
        {
            PwEntry[] pwes = m_host.MainWindow.GetSelectedEntries();
            Debug.Assert(pwes != null);
            if (pwes == null || pwes.Length == 0)
                return;
            downloadSomeFavicons(KeePassLib.Collections.PwObjectList<PwEntry>.FromArray(pwes));
        }

        void downloadSomeFavicons(KeePassLib.Collections.PwObjectList<PwEntry> entries) {
            var favicons = new Favicons(m_host);
            favicons.DownloadAll(entries);
        }
    }
}
