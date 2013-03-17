﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Meticumedia
{
    public class TvRenameScan : Scan
    {
        public TvRenameScan(bool background)
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

                OnProgressChange(ScanProcess.TvRename, shows[i].Name, (int)Math.Round((double)i / (shows.Count) * 30) + 70);

                // Go each show
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
                        if (ep.Ignored)
                            continue;

                        // Init found flag
                        bool found = false;

                        // Rename check
                        if (ep.Missing == TvEpisode.MissingStatus.Located)
                        {
                            if (shows[i].DoRenaming)
                            {
                                found = true;
                                TvEpisode ep2 = null;

                                if (ep.File.MultiPart)
                                {
                                    if (ep.File.Part == 1)
                                    {
                                        // TODO: Need episode collection for addressing episodes like seasons, so I can do the following:
                                        // TvEpisode ep2 = show.Seasons[ep.Season].Episodes[ep.Number + 1];
                                        // instead of the below:
                                        foreach (TvEpisode epEnumerated in show.Seasons[ep.Season].Episodes)
                                            if (epEnumerated.Number == ep.Number + 1)
                                            {
                                                ep2 = epEnumerated;
                                                break;
                                            }
                                    }
                                    else
                                        continue;
                                }

                                // Build desired path
                                string builtPath = show.BuildFilePath(ep, ep2, Path.GetExtension(ep.File.FilePath));

                                // Check if rename needed (or move within folder)
                                if (ep.File.FilePath != builtPath)
                                {
                                    OrgItem newItem = new OrgItem(OrgStatus.Organization, OrgAction.Rename, ep.File.FilePath, builtPath, ep, ep2, FileHelper.FileCategory.TvVideo, null);
                                    newItem.Check = System.Windows.Forms.CheckState.Checked;
                                    if (!shows[i].IncludeInScan)
                                        newItem.Category = FileHelper.FileCategory.Ignored;
                                    newItem.Number = number++;
                                    newItem.Show = show;
                                    missingCheckItem.Add(newItem);
                                }
                            }
                            else
                                continue;
                        }
                        else
                            continue;

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

            // Convert all TV folders to org folders
            List<OrgFolder> tvFoldersAsOrgFolders = new List<OrgFolder>();
            foreach (ContentRootFolder tvFolder in Settings.TvFolders)
            {
                OrgFolder orgFolder = new OrgFolder(tvFolder.FullPath, false, false, false, false);
                tvFoldersAsOrgFolders.Add(orgFolder);
            }

            // Update progress
            OnProgressChange(ScanProcess.TvRename, string.Empty, 100);

            // Clear flags
            scanRunning = false;
            cancelRequested = false;

            // Return results
            return missingCheckItem;
        }
    }
}
