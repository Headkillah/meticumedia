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
using Meticumedia.Classes;

namespace MeticumediaTesting
{
    /// <summary>
    /// Interaction logic for MatchTestControl.xaml
    /// </summary>
    public partial class MatchTestControl : UserControl
    {
        private MatchTestWindowViewModel viewModel;
        
        public MatchTestControl()
        {
            InitializeComponent();
            Organization.LoadScanDirLog(this, new System.ComponentModel.DoWorkEventArgs(null));

            viewModel = new MatchTestWindowViewModel();
            this.DataContext = viewModel;
        }
    }
}
