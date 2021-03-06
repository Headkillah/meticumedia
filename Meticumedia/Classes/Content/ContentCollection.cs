﻿// --------------------------------------------------------------------------------
// Source code available at http://code.google.com/p/meticumedia/
// This code is released under GPLv3 http://www.gnu.org/licenses/gpl.html
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;

namespace Meticumedia.Classes
{
    /// <summary>
    /// List of content with added properties. (Inheriting list to have sorting methods)
    /// </summary>
    public class ContentCollection : List<Content>, INotifyCollectionChanged
    {
        #region Properties

        /// <summary>
        /// Database time when collection was updated.
        /// </summary>
        public string LastUpdate { get; set; }

        /// <summary>
        /// Type of content stored in collection
        /// </summary>
        public ContentType ContentType { get; set; }

        /// <summary>
        /// Lock for accessing content
        /// </summary>
        public object ContentLock = new object();

        /// <summary>
        /// Lock for accessing content xml file
        /// </summary>
        public object XmlLock = new object();

        /// <summary>
        /// Name of collection
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Flag indicating collection loading is complete
        /// </summary>
        public bool LoadCompleted { get { return loaded; } }

        #endregion

        #region Events

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(action));
        }

        public void OnCollectionChanged(NotifyCollectionChangedAction action, Content item)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, item));
        }

        public void OnCollectionChanged(List<Content> items)
        {
            if (CollectionChanged != null)
            {
                NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items);
                CollectionChanged(this, args);
            }
        }

        /// <summary>
        /// Static event that fires when show loading progress changes
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> LoadProgressChange;

        /// <summary>
        /// Triggers TvShowLoadProgressChange event
        /// </summary>
        public void OnLoadProgressChange(int percent)
        {
            if (LoadProgressChange != null)
                LoadProgressChange(this, new ProgressChangedEventArgs(percent, null));
        }

        /// <summary>
        /// Static event that fires when show loading completes
        /// </summary>
        public event EventHandler LoadComplete;

        /// <summary>
        /// Triggers TvShowLoadComplete event
        /// </summary>
        public void OnLoadComplete()
        {
            if (LoadComplete != null)
                LoadComplete(this, new EventArgs());
            loaded = true;
        }

        /// <summary>
        /// Indication of whether loading is complete
        /// </summary>
        private bool loaded = false;

        /// <summary>
        /// Indicates that contents have been saved.
        /// </summary>
        public event EventHandler ContentSaved;

        /// <summary>
        /// Triggers ContentSaved event
        /// </summary>
        protected void OnContentSaved()
        {
            if (ContentSaved != null)
                ContentSaved(this, new EventArgs());
        }  

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public ContentCollection(ContentType type, string name)
            : base()
        {
            this.ContentType = type;
            this.Name = name;

            saveWorker = new BackgroundWorker();
            saveWorker.WorkerSupportsCancellation = true;
            saveWorker.DoWork += saveWorker_DoWork;
            saveWorker.RunWorkerCompleted += saveWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// Override ToString to use Name property
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }

        #endregion

        #region Constants/Variable

        /// <summary>
        /// Root string from XML
        /// </summary>
        private string XML_ROOT { get { return this.ContentType.ToString() + "s"; } }

        private BackgroundWorker saveWorker = new BackgroundWorker();

        private bool reSaveRequired = false;

        #endregion

        #region New base methods with locking

        private void AddMultiple(List<Content> items)
        {
            lock (ContentLock)
            {
               foreach(Content item in items)
                base.Add(item);
            }
            OnCollectionChanged(items);
        }

        public new void Add(Content item)
        {
            lock (ContentLock)
            {
                base.Add(item);
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
        }

        public new void Remove(Content item)
        {
            lock (ContentLock)
            {
                base.Remove(item);
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
        }

        public new void RemoveAt(int index)
        {
            Content item = this[index];
            lock (ContentLock)
            {
                base.RemoveAt(index);
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
        }

        public new void Clear()
        {
            lock (ContentLock)
            {
                base.Clear();
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public new void Sort()
        {
            lock (ContentLock)
            {
                base.Sort();
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load collection from saved XML file
        /// </summary>
        public void Load(bool doUpdating)
        {
            XmlTextReader reader = null;
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                string path = Path.Combine(Organization.GetBasePath(false), XML_ROOT + ".xml");

                if (File.Exists(path))
                {
                    // Use dummy collection to load into so that loading doesn't hog use of object
                    ContentCollection loadContent = new ContentCollection(this.ContentType, "Loading Shows");
                    lock (XmlLock)
                    {
                        // Load XML
                        reader = new XmlTextReader(path);
                        xmlDoc.Load(reader);

                        // Extract data
                        XmlNodeList contentNodes = xmlDoc.DocumentElement.ChildNodes;
                        for (int i = 0; i < contentNodes.Count; i++)
                        {
                            // Update loading progress
                            OnLoadProgressChange((int)(((double)i / contentNodes.Count) * 100));

                            // All elements will be content items or last update time
                            if (contentNodes[i].Name == "LastUpdate")
                                loadContent.LastUpdate = contentNodes[i].InnerText;
                            else
                            {
                                // Load content from element based on type
                                switch (this.ContentType)
                                {
                                    case ContentType.TvShow:
                                        TvShow show = new TvShow();
                                        if (show.Load(contentNodes[i]))
                                        {
                                            bool rootFolderExists = false;
                                            foreach (ContentRootFolder folder in Settings.GetAllRootFolders(this.ContentType, true))
                                                if (folder.FullPath == show.RootFolder)
                                                    rootFolderExists = true;

                                            if (!rootFolderExists || !Directory.Exists(show.Path))
                                                continue;
                                            
                                            loadContent.Add(show);
                                            if (doUpdating)
                                                show.UpdateMissing();
                                        }
                                        break;
                                    case ContentType.Movie:
                                        Movie movie = new Movie();
                                        if (movie.Load(contentNodes[i]))
                                            loadContent.Add(movie);
                                        break;
                                    default:
                                        throw new Exception("Unknown content type");
                                }
                            }
                        }
                    }
                    // Update progress
                    OnLoadProgressChange(100);

                    this.LastUpdate = loadContent.LastUpdate;
                    this.Clear();
                    AddMultiple(loadContent);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error loading " + this.ContentType + "s from saved data!");
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            // Start updating of TV episode in scan dirs.
            if (this.ContentType == ContentType.TvShow && doUpdating)
            {
                TvItemInScanDirHelper.DoUpdate(false);
                TvItemInScanDirHelper.StartUpdateTimer();
            }

            // Trigger load complete event
            OnLoadComplete();
        }

       

        /// <summary>
        /// Saves collection to XML file
        /// </summary>
        public void Save()
        {
            if (saveWorker.IsBusy)
            {
                reSaveRequired = true;
                saveWorker.CancelAsync();
            }
            else
                saveWorker.RunWorkerAsync();
        }

        void saveWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (reSaveRequired)
            {
                reSaveRequired = false;
                Save();
            }
        }

        void saveWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get path to xml file
            string path = Path.Combine(Organization.GetBasePath(true), XML_ROOT + ".xml");

            // Save data into temporary file, so that if application crashes in middle of saving XML is not corrupted!
            string tempPath = Path.Combine(Organization.GetBasePath(true), XML_ROOT + "_TEMP.xml");

            //Console.WriteLine(this.ToString() + " lock save");
            lock (XmlLock)
            {
                // Lock content and file
                lock (ContentLock)
                {

                    using (XmlTextWriter xw = new XmlTextWriter(tempPath, Encoding.ASCII))
                    {
                        xw.Formatting = Formatting.Indented;
                        
                        xw.WriteStartElement(XML_ROOT);
                        xw.WriteElementString("LastUpdate", this.LastUpdate);

                        foreach (Content content in this)
                        {
                            content.Save(xw);

                            if (saveWorker.CancellationPending)
                                return;
                        }
                        xw.WriteEndElement();
                    }

                    if (saveWorker.CancellationPending)
                        return;

                    // Delete previous save data
                    if (File.Exists(path))
                        File.Delete(path);

                    // Move tempoarary save file to default
                    File.Move(tempPath, path);
                }
            }
            //Console.WriteLine(this.ToString() + " release save");

            // Trigger content saved event
            OnContentSaved();
        }

        /// <summary>
        /// Remove all content that no longer exist in a root folder
        /// </summary>
        /// <param name="rootFolder">Root folder to remove missing content from</param>
        public void RemoveMissing()
        {
            for (int i = this.Count - 1; i >= 0; i--)
                if (!this[i].Found) // && rootFolder.ContainsContent(this[i], true)) - This was here before, but if a root folder was removed it's content stayed in collection forever..
                    RemoveAt(i);
        }

        /// <summary>
        /// Get a lists of content that are set to be included in scanning
        /// </summary>
        /// <returns>List of show that are included in scanning</returns>
        public List<Content> GetScannableContent(bool updateMissing, ScanType scanType)
        {
            List<Content> includeShow = new List<Content>();
            for (int i = 0; i < this.Count; i++)
            {
                bool include = true;
                switch (scanType)
                {
                    case ScanType.TvMissing:
                        include = (this[i] as TvShow).DoMissingCheck;
                        break;
                    case ScanType.TvRename:
                    case ScanType.TvFolder:
                    case ScanType.MovieFolder:
                        include = this[i].DoRenaming;
                        break;
                }

                if (include && !string.IsNullOrEmpty(this[i].DatabaseName))
                {
                    if (updateMissing)
                        this[i].UpdateMissing();
                    includeShow.Add(this[i]);
                }
            }
            return includeShow;
        }

        #endregion

    }
}
