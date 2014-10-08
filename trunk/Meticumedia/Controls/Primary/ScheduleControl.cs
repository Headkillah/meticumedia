﻿// --------------------------------------------------------------------------------
// Source code available at http://code.google.com/p/meticumedia/
// This code is released under GPLv3 http://www.gnu.org/licenses/gpl.html
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Meticumedia
{
    /// <summary>
    /// Control for display upcoming and recent TV episodes
    /// </summary>
    public partial class ScheduleControl : UserControl
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public ScheduleControl()
        {
            InitializeComponent();

            // Setup Type combo box
            foreach (TvScheduleType type in Enum.GetValues(typeof(TvScheduleType)))
                cmbType.Items.Add(type);
            cmbType.SelectedIndex = 0;

            // Remove overview column
            lvResults.Columns.Remove(colOverview);

            // Build episode filters
            cmbEpFilter.Items.Clear();
            List<TvEpisodeFilter> epFilters = TvEpisodeFilter.BuildFilters(null, false, false);
            foreach (TvEpisodeFilter filter in epFilters)
                cmbEpFilter.Items.Add(filter);
            cmbEpFilter.SelectedIndex = 0;
            cmbEpFilter.SelectedIndexChanged += cmbEpFilter_SelectedIndexChanged;

            // Setup legend
            SetLegend();
            TvEpisode.BackColourChanged += new EventHandler(TvEpisode_BackColourChanged);

            // Register updates to TV items in scan folders
            TvItemInScanDirHelper.ItemsUpdate += ScanHelper_TvScanItemsUpdate;

            // Setup context menu for listview
            lvResults.ContextMenu = contextMenu;
            contextMenu.Popup += new EventHandler(contextMenu_Popup);

            Organization.Shows.LoadComplete += Shows_Changed;
            Organization.Shows.ContentAdded += Shows_Changed;

            this.Load += ScheduleControl_Load;
        }

        void ScheduleControl_Load(object sender, EventArgs e)
        {
            if (updateOnLoadRequired)
                UpdateShows();
        }

        void Shows_Changed(object sender, EventArgs e)
        {
            if (!this.IsHandleCreated)
            {
                updateOnLoadRequired = true;
                return;
            }
            
            this.Invoke((MethodInvoker)delegate
            {
                UpdateShows();
            });
        }

        #endregion

        #region Context Menu

        /// <summary>
        /// Context menu for episodes list
        /// </summary>
        private ContextMenu contextMenu = new ContextMenu();

        /// <summary>
        /// Build context menu on right-click
        /// </summary>
        void contextMenu_Popup(object sender, EventArgs e)
        {
            BuildContextMenu();
        }

        /// <summary>
        /// Builds context menu based on what is currently selected
        /// </summary>
        private void BuildContextMenu()
        {
            contextMenu.MenuItems.Clear();

            // Get selected episodes
            TvEpisode ep;
            if (!GetSelectedEpisode(out ep))
                return;

            if (ep.Missing != TvEpisode.MissingStatus.Missing)
                contextMenu.MenuItems.Add("Play", new EventHandler(HandlePlay));
            contextMenu.MenuItems.Add("Copy Episode String to Clipboard", new EventHandler(HandleCopyToClipboard));
            contextMenu.MenuItems.Add("Exclude Show From Schedule", new EventHandler(HandleExcludeFromSchedule));
        }

        /// <summary>
        /// Handle play selection in context menu
        /// </summary>
        private void HandlePlay(object sender, EventArgs e)
        {
            PlaySelectedEpisode();
        }

        /// <summary>
        /// Handle copy to clipboard selection in context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleCopyToClipboard(object sender, EventArgs e)
        {
            CopyEpInfoToClipboard();
        }

        /// <summary>
        /// Handles turn off include in scan for show selection from context menu
        /// </summary>
        private void HandleExcludeFromSchedule(object sender, EventArgs e)
        {
            // Ensure items are selected
            if (lvResults.SelectedIndices.Count == 0)
                return;

            // Get selected shows
            List<TvShow> showsToBeSet = new List<TvShow>();
            for (int i = lvResults.SelectedIndices.Count - 1; i >= 0; i--)
            {
                TvEpisode currEp = results[lvResults.SelectedIndices[i]];
                TvShow show = currEp.GetShow();
                show.IncludeInSchedule = false;
            }

            Organization.Shows.Save();
            UpdateResults();
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Handle updates to TV items in scan folders event
        /// </summary>
        void ScanHelper_TvScanItemsUpdate(object sender, EventArgs e)
        {
            if (!this.IsHandleCreated)
                return;

            this.Invoke((MethodInvoker)delegate
            {
                UpdateResults();
            });
        }

        /// <summary>
        /// Changes to episode colours mirrored to legend
        /// </summary>
        private void TvEpisode_BackColourChanged(object sender, EventArgs e)
        {
            SetLegend();
        }

        /// <summary>
        /// Sets legend to episode colours
        /// </summary>
        private void SetLegend()
        {
            lbMissing.Color = TvEpisode.MissingBackColor;
            lbScanDir.Color = TvEpisode.InScanDirectoryColor;
            lbIgnored.Color = TvEpisode.IgnoredBackColor;
            lbUnaired.Color = TvEpisode.UnairedBackColor;
        }

        /// <summary>
        /// Schedule type combo box selection change refresh display
        /// </summary>
        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Refresh schedule results
            UpdateResults();

            // Set text so that schedule setup sentence makes sense
            if (((TvScheduleType)cmbType.SelectedItem) == TvScheduleType.Upcoming)
                lblLastNext.Text = "for next";
            else
                lblLastNext.Text = "from last";
        }

        /// <summary>
        /// Show selection change causes results to be refreshed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbShows_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateResults();
        }

        /// <summary>
        /// Number of days changes cause results to be refreshed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numDays_ValueChanged(object sender, EventArgs e)
        {
            UpdateResults();
        }

        /// <summary>
        /// Display overview checkbox shows/hides overview column in listview
        /// </summary>
        private void chkDisplayOverview_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDisplayOverview.Checked)
                lvResults.Columns.Add(colOverview);
            else
                lvResults.Columns.Remove(colOverview);
        }

        /// <summary>
        /// Double click copy show name and episode number to clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvResults_DoubleClick(object sender, EventArgs e)
        {
            TvEpisode ep;
            if (!GetSelectedEpisode(out ep))
                return;

            if (ep.Missing == TvEpisode.MissingStatus.Missing)
                CopyEpInfoToClipboard();
            else
                PlaySelectedEpisode();
        }

        /// <summary>
        /// Copy selected episode show name and episode number to clipboard
        /// </summary>
        private void CopyEpInfoToClipboard()
        {
            TvEpisode ep;
            if (!GetSelectedEpisode(out ep))
                return;

            Clipboard.SetText(ep.BuildEpString());
        }

        /// <summary>
        /// Get selected episode from list
        /// </summary>
        /// <param name="ep">Selected episode</param>
        /// <returns>Whether an episode is selected</returns>
        private bool GetSelectedEpisode(out TvEpisode ep)
        {
            ep = null;

            // Check for selection
            if (lvResults.SelectedItems.Count == 0)
                return false;

            // Get selected episode
            ep = results[lvResults.SelectedIndices[0]];
            return true;
        }

        /// <summary>
        /// Play episode that is selected
        /// </summary>
        private void PlaySelectedEpisode()
        {
            TvEpisode ep;
            if (!GetSelectedEpisode(out ep))
                return;

            ep.PlayEpisodeFile();
        }

        private void cmbEpFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateResults();
        }

        #endregion

        #region Updating

        private bool updateOnLoadRequired = false;

        /// <summary>
        /// Updates shows contained in show selection combobox
        /// </summary>
        public void UpdateShows()
        {            
            // Save current selection
            string currSelection = string.Empty;
            if(cmbShows.SelectedItem != null)
                currSelection = cmbShows.SelectedItem.ToString();

            // Rebuilt combobox items
            cmbShows.Items.Clear();
            cmbShows.Items.Add("All Shows");
            bool selected = false;
                for(int i=0;i<Organization.Shows.Count;i++)
                {
                    // Add item
                    if (!string.IsNullOrEmpty(Organization.Shows[i].Name) && Organization.Shows[i].Id != 0)
                        cmbShows.Items.Add(Organization.Shows[i].Name);

                    // Check if item should be selected
                    if (!string.IsNullOrWhiteSpace(currSelection) && Organization.Shows[i].Name == currSelection)
                    {
                        cmbShows.SelectedIndex = cmbShows.Items.Count - 1;
                        selected = true;
                    }
                }

            // Set selection to first item if previous was not reselected
            if (!selected)
                cmbShows.SelectedIndex = 0;
        }

        /// <summary>
        /// Updates and display resulting TV episodes
        /// </summary>
        private void UpdateResults()
        {          
            // Get shows to check from combo box
            List<Content> shows;
            if (cmbShows.SelectedIndex < 1)
                shows = Organization.Shows;
            else
            {
                shows = new List<Content>();
                Content show = Organization.Shows[cmbShows.SelectedIndex - 1];
                //show.UpdateMissing();
                shows.Add(show);
            }

            // Get filter
            TvEpisodeFilter epFilter = (TvEpisodeFilter)cmbEpFilter.SelectedItem;
            if (epFilter == null)
                epFilter = new TvEpisodeFilter(TvEpisodeFilter.FilterType.All, 0);
            
            // Get results
            results = BuildEpisodeList(shows, (int)numDays.Value, ((TvScheduleType)cmbType.SelectedItem) == TvScheduleType.Upcoming, epFilter);

            // Put results in list
            lvResults.Items.Clear();
            foreach (TvEpisode ep in results)
            {
                ListViewItem item = lvResults.Items.Add(ep.Show);
                item.SubItems.Add(ep.Season.ToString());
                item.SubItems.Add(ep.Number.ToString());
                item.SubItems.Add(ep.Name);
                item.SubItems.Add(String.Format("{0:MMM dd yyyy}", ep.AirDate));
                item.SubItems.Add(ep.Overview);
                item.BackColor = ep.GetBackColor();
            }
        }

        /// <summary>
        /// Result currently displayed in listview
        /// </summary>
        List<TvEpisode> results;

        /// <summary>
        /// Gets list of episodes matching user input for scheduling.
        /// </summary>
        /// <param name="shows">List of shows to look for episodes from</param>
        /// <param name="days">Number of days to look for episodes in</param>
        /// <param name="upcoming">Whether to check for upcoming or recent episodes</param>
        /// <returns>List of episodes</returns>
        private List<TvEpisode> BuildEpisodeList(List<Content> shows, int days, bool upcoming, TvEpisodeFilter epFilter)
        {
            // Initialize episode list
            List<TvEpisode> epList = new List<TvEpisode>();

            // Check every shows for episode that match schedule criteria
            for(int i=0;i<shows.Count;i++)
                if (((TvShow)shows[i]).IncludeInSchedule)
                {
                    foreach (TvSeason season in ((TvShow)shows[i]).Seasons)
                        foreach (TvEpisode episode in season.Episodes)
                        {
                            TimeSpan timeDiff = episode.AirDate.Subtract(DateTime.Now);
                            if (!episode.Ignored && epFilter.FilterEpisode(episode) && Math.Abs(timeDiff.Days) < days)
                            {
                                if (upcoming && timeDiff.Days >= 0)
                                    epList.Add(episode);
                                else if (!upcoming && timeDiff.Days < 0)
                                    epList.Add(episode);
                            }
                        }
                }

            // Sort (reverse to get latest at top) and return episodes  
            epList.Sort(TvEpisode.CompareByAirDate);
            if (!upcoming)
                epList.Reverse();
            return epList;
        }

        #endregion

        
    }
}
