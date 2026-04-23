// <copyright file="Themes.xaml.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Views
{
    using System;
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

    /// <summary>
    /// Interaction logic for Themes.xaml.
    /// </summary>
    public partial class Themes : UserControl
    {
        public Themes()
        {
            this.InitializeComponent();
        }

        private void ThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
