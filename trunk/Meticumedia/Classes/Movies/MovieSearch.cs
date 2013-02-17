﻿// --------------------------------------------------------------------------------
// Source code available at http://code.google.com/p/meticumedia/
// This code is released under GPLv3 http://www.gnu.org/licenses/gpl.html
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meticumedia
{
    /// <summary>
    /// Class for performing database searching for a movie.
    /// </summary>
    public class MovieSearch : ContentSearch
    {
        /// <summary>
        /// Performs search for movie in database
        /// </summary>
        /// <param name="search">string to search for</param>
        /// <param name="includeSummaries">whether to get summary of each return search result</param>
        /// <returns>List of content matching search string from online database</returns>
        protected override List<Content> PerformSearch(string search, bool includeSummaries)
        {
            return TheMovieDbHelper.PerformMovieSearch(search);
        }

        /// <summary>
        /// Attempts to match string to movie from the online database.
        /// </summary>
        /// <param name="search">Search string to match against</param>
        /// <param name="rootFolder">The root folder the content will belong to</param>
        /// <param name="folderPath">Folder path where the content should be moved to</param>
        /// <returns>Match movie item, null if no match</returns>
        public new Movie ContentMatch(string search, string rootFolder, string folderPath)
        {
            return this.ContentMatch(search, rootFolder, folderPath, true);
        }

        /// <summary>
        /// Attempts to match string to movie from the online database.
        /// </summary>
        /// <param name="search">Search string to match against</param>
        /// <param name="rootFolder">The root folder the content will belong to</param>
        /// <param name="folderPath">Folder path where the content should be moved to</param>
        /// <param name="threaded">Whether search is threaded, setting to false can help with debugging</param>
        /// <returns>Match movie item, null if no match</returns>
        public new Movie ContentMatch(string search, string rootFolder, string folderPath, bool threaded)
        {
            Content match = base.ContentMatch(search, rootFolder, folderPath, threaded);
            Movie movie = new Movie(match);
            return TheMovieDbHelper.UpdateMovieInfo(movie); 
        }

        /// <summary>
        /// Match a folder path to movie in database
        /// </summary>
        /// <param name="rootFolder">Root folder content will belong to</param>
        /// <param name="path">Current path of content</param>
        /// <returns>Movie from database that was matched, null if no match</returns>
        public new Movie PathMatch(string rootFolder, string path)
        {
            Content match = base.PathMatch(rootFolder, path);
            Movie movie = new Movie(match);
            if (movie.Id > 0)
                return TheMovieDbHelper.UpdateMovieInfo(movie);
            else
                return movie;
        }

    }
}
