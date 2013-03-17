﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Meticumedia
{
    public class TvMissingScan : Scan
    {

        public TvMissingScan(bool background)
            : base(background)
        {
        }
        
        /// <summary>
        /// Run through all TV shows all looks for episodes that may need to be renamed and for missing episodes.
        /// For missing episodes it attempts to match them to files from the search directories.
        /// </summary>
        /// <param name="shows">Shows to scan</param>
        /// <param name="queuedItems">Items currently in queue (to be skipped)</param>
        /// <returns></returns>
        public List<OrgItem> RunScan(List<Content> shows, List<OrgItem> queuedItems)
        {
            // Set running flag
            scanRunning = true;

            // Do directory check on all directories (to look for missing episodes)
            DirectoryScan dirScan = new DirectoryScan(false);
            List<OrgItem> directoryItems = dirScan.RunScan(Settings.ScanDirectories, queuedItems, 70, true, true);

            // Initialiaze scan items
            List<OrgItem> missingCheckItem = new List<OrgItem>();

            // Initialize item numbers
            int number = 0;

            // Go through each show
            for (int i = 0; i < shows.Count; i++)
            {
                TvShow show = (TvShow)shows[i];

                if (cancelRequested)
                    break;

                OnProgressChange(ScanProcess.TvMissing, shows[i].Name, (int)Math.Round((double)i / (shows.Count) * 30) + 70);

                // Go through missing episodes
                foreach (TvSeason season in show.Seasons)
                {
                    // Check for cancellation
                    if (cancelRequested)
                        break;

                    foreach (TvEpisode ep in season.Episodes)
                    {
                        // Check for cancellation
                        if (cancelRequested)
                            break;

                        // Skipped ignored episodes
                        if (ep.Ignored || !show.DoMissingCheck)
                            continue;

                        // Init found flag
                        bool found = false;

                        // Check if episode is missing
                        if (ep.Missing == TvEpisode.MissingStatus.Missing || ep.Missing == TvEpisode.MissingStatus.InScanDirectory)
                        {
                            // Check directory item for episode
                            foreach (OrgItem item in directoryItems)
                                if ((item.Action == OrgAction.Move || item.Action == OrgAction.Copy) && item.TvEpisode != null && item.TvEpisode.Show == show.Name)
                                {
                                    // Only add item for first part of multi-part file
                                    if (ep.Equals(item.TvEpisode))
                                    {
                                        OrgItem newItem = new OrgItem(OrgStatus.Found, item.Action, item.SourcePath, item.DestinationPath, ep, item.TvEpisode2, FileHelper.FileCategory.TvVideo, item.ScanDirectory);
                                        newItem.Check = System.Windows.Forms.CheckState.Checked;
                                        newItem.Number = number++;
                                        newItem.Show = show;
                                        if (!shows[i].IncludeInScan)
                                            newItem.Category = FileHelper.FileCategory.Ignored;
                                        missingCheckItem.Add(newItem);
                                        found = true;
                                        break;
                                    }
                                    else if (ep.Equals(item.TvEpisode2))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                        }
                        else
                            continue;

                        // TODO: loose file check - do directory scan on TV root folder with recursion off..

                        // Add empty item for missing
                        if (!found && ep.Aired && show.DoMissingCheck)
                        {
                            OrgItem newItem = new OrgItem(OrgStatus.Missing, OrgAction.None, ep, null, FileHelper.FileCategory.TvVideo, null);
                            if (!shows[i].IncludeInScan)
                                newItem.Category = FileHelper.FileCategory.Ignored;
                            newItem.Number = number++;
                            newItem.Show = show;
                            missingCheckItem.Add(newItem);
                        }
                    }
                }
            }

            // Update progress
            OnProgressChange(ScanProcess.TvMissing, string.Empty, 100);

            // Clear flags
            scanRunning = false;
            cancelRequested = false;

            // Return results
            return missingCheckItem;
        }
    }
}
