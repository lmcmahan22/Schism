// <copyright file="MainWindow.xaml.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schism.Views
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Initializes a new instance of the MainWindow class. This constructor is responsible for setting up the user interface components defined in the XAML file associated with this window.

        // NOTE: This code here is known as the "Code-Behind". This is essentially logic needed to control the View, but ideally, we want all of that managed in the ViewModel classes.
        // Keep this class small!
        public MainWindow()
        {
            this.InitializeComponent();
        }
    }
}