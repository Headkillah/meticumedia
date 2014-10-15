﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Meticumedia.Classes;
using Meticumedia.Controls;
using Meticumedia.WPF;

namespace Meticumedia.Windows
{
    public class SettingsWindowViewModel : ViewModel
    {
        #region Events

        public event EventHandler ResultsSet;

        private void OnResultsSet()
        {
            if (ResultsSet != null)
                ResultsSet(this, new EventArgs());
        }

        #endregion

        #region Properties

        public GeneralSettingsControlViewModel GeneralSettingsViewModel
        {
            get
            {
                return generalSettingsViewModel;
            }
            set
            {
                generalSettingsViewModel = value;
                OnPropertyChanged(this, "GeneralSettingsViewModel");
            }
        }
        private GeneralSettingsControlViewModel generalSettingsViewModel;

        public ScanFoldersControlViewModel ScanFoldersViewModel
        {
            get
            {
                return scanFoldersViewModel;
            }
            set
            {
                scanFoldersViewModel = value;
                OnPropertyChanged(this, "ScanFoldersViewModel");
            }
        }
        private ScanFoldersControlViewModel scanFoldersViewModel;

        public RootFolderControlViewModel MovieRootFoldersViewModel
        {
            get
            {
                return movieRootFoldersViewModel;
            }
            set
            {
                movieRootFoldersViewModel = value;
                OnPropertyChanged(this, "MovieRootFoldersViewModel");
            }
        }
        private RootFolderControlViewModel movieRootFoldersViewModel;

        public RootFolderControlViewModel TvRootFoldersViewModel
        {
            get
            {
                return tvRootFoldersViewModel;
            }
            set
            {
                tvRootFoldersViewModel = value;
                OnPropertyChanged(this, "TvRootFoldersViewModel");
            }
        }
        private RootFolderControlViewModel tvRootFoldersViewModel;

        public FileNameControlViewModel MovieFileNameViewModel
        {
            get
            {
                return movieFileNameViewModel;
            }
            set
            {
                movieFileNameViewModel = value;
                OnPropertyChanged(this, "MovieFileNameViewModel");
            }
        }
        private FileNameControlViewModel movieFileNameViewModel;

        public FileNameControlViewModel TvFileNameViewModel
        {
            get
            {
                return tvFileNameViewModel;
            }
            set
            {
                tvFileNameViewModel = value;
                OnPropertyChanged(this, "TvFileNameViewModel");
            }
        }
        private FileNameControlViewModel tvFileNameViewModel;

        public FileTypesControlViewModel VideoFileTypesViewModel
        {
            get
            {
                return videoFileTypesViewModel;
            }
            set
            {
                videoFileTypesViewModel = value;
                OnPropertyChanged(this, "VideoFileTypesViewModel");
            }
        }
        private FileTypesControlViewModel videoFileTypesViewModel;

        public FileTypesControlViewModel DeleteFileTypesViewModel
        {
            get
            {
                return seleteFileTypesViewModel;
            }
            set
            {
                seleteFileTypesViewModel = value;
                OnPropertyChanged(this, "DeleteFileTypesViewModel");
            }
        }
        private FileTypesControlViewModel seleteFileTypesViewModel;

        public FileTypesControlViewModel IgnoreFileTypesViewModel
        {
            get
            {
                return ignoreFileTypesViewModel;
            }
            set
            {
                ignoreFileTypesViewModel = value;
                OnPropertyChanged(this, "IgnoreFileTypesViewModel");
            }
        }
        private FileTypesControlViewModel ignoreFileTypesViewModel;

        #endregion

        #region Commands

        private ICommand okCommand;
        public ICommand OkCommand
        {
            get
            {
                if (okCommand == null)
                {
                    okCommand = new RelayCommand(
                        param => this.Ok()
                    );
                }
                return okCommand;
            }
        }

        private ICommand cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(
                        param => this.Cancel()
                    );
                }
                return cancelCommand;
            }
        }

        #endregion

        #region Constructor

        public SettingsWindowViewModel()
        {
            this.GeneralSettingsViewModel = new GeneralSettingsControlViewModel(Settings.General);
            this.ScanFoldersViewModel = new ScanFoldersControlViewModel(Settings.ScanDirectories);
            this.MovieRootFoldersViewModel = new RootFolderControlViewModel(Settings.MovieFolders, ContentType.Movie);
            this.TvRootFoldersViewModel = new RootFolderControlViewModel(Settings.TvFolders, ContentType.TvShow);
            this.TvFileNameViewModel = new FileNameControlViewModel(Settings.TvFileFormat, ContentType.TvShow);
            this.MovieFileNameViewModel = new FileNameControlViewModel(Settings.MovieFileFormat, ContentType.Movie);
            this.VideoFileTypesViewModel = new FileTypesControlViewModel(Settings.VideoFileTypes);
            this.DeleteFileTypesViewModel = new FileTypesControlViewModel(Settings.DeleteFileTypes);
            this.IgnoreFileTypesViewModel = new FileTypesControlViewModel(Settings.IgnoreFileTypes);
        }

        #endregion

        #region Methods

        private void Ok()
        {
            Settings.General.Clone(this.GeneralSettingsViewModel.GeneralSettings);
            
            Settings.ScanDirectories.Clear();
            foreach (OrgFolder fldr in this.ScanFoldersViewModel.Folders)
               Settings.ScanDirectories.Add(fldr);

            Settings.MovieFolders.Clear();
            foreach (ContentRootFolder folder in this.MovieRootFoldersViewModel.RootFolders)
                Settings.MovieFolders.Add(folder);

            Settings.TvFolders.Clear();
            foreach (ContentRootFolder folder in this.TvRootFoldersViewModel.RootFolders)
                Settings.TvFolders.Add(folder);

            Settings.MovieFileFormat.Clone(this.MovieFileNameViewModel.FileNameFormat);

            Settings.TvFileFormat.Clone(this.TvFileNameViewModel.FileNameFormat);

            Settings.VideoFileTypes.Clear();
            foreach (string fileType in this.VideoFileTypesViewModel.FileTypes)
                Settings.VideoFileTypes.Add(fileType);

            Settings.DeleteFileTypes.Clear();
            foreach (string fileType in this.DeleteFileTypesViewModel.FileTypes)
                Settings.DeleteFileTypes.Add(fileType);

            Settings.IgnoreFileTypes.Clear();
            foreach (string fileType in this.IgnoreFileTypesViewModel.FileTypes)
                Settings.IgnoreFileTypes.Add(fileType);

            Settings.Save();
            OnResultsSet();
        }

        private void Cancel()
        {
            OnResultsSet();
        }

        #endregion
    }
}
