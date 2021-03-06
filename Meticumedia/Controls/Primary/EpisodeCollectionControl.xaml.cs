﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Meticumedia.Controls
{
    /// <summary>
    /// Interaction logic for EpisodesControl.xaml
    /// </summary>
    public partial class EpisodeCollectionControl : UserControl
    {
        public EpisodeCollectionControl()
        {
            InitializeComponent();
        }

        private void BindableMultiSelectListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext != null && this.DataContext is EpisodeCollectionControlViewModel)
            {
                EpisodeCollectionControlViewModel vm = this.DataContext as EpisodeCollectionControlViewModel;
                vm.HandleDoubleClick();
            }
        }
    }
}
