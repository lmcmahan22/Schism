// <copyright file="MainModule.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using Prism.Ioc;
    using Prism.Modularity;
    using Prism.Navigation.Regions;
    using Prism.Unity;
    using Schiism.ViewModels;
    using Schiism.Views;

    // Implements the IModule interface, which is a contract for modules in a Prism application. This class is responsible for initializing the module and registering any types or services that the module provides.
    public class MainModule : IModule
    {
        // This method is called when the module is initialized. It is responsible for registering views with regions and performing any necessary setup for the module, all within a "Container" object.
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion("ContentRegion", typeof(Home));
        }

        // This method is called to register types with the container. It is responsible for registering any services, view models, or other types that the module provides.
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
