// <copyright file="Home.xaml.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schism.Views
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for Home.xaml.
    /// </summary>
    public partial class Home : UserControl
    {
        public Home()
        {
            this.InitializeComponent();
        }

        private void BaseSystemSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
