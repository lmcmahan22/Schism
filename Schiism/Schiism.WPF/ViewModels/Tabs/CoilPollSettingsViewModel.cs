using Schiism.Core.Configuration;
using Schiism.Core.Configuration.Enums;
using Schiism.Core.Configuration.StateControl;
using Schiism.WPF.Models;
using Schiism.WPF.Tabs;
using Schiism.WPF.ViewModels.Abstractions;

namespace Schiism.WPF.ViewModels.Tabs
{
    public class CoilPollSettingsViewModel : PollSettingsViewModel
    {
        public CoilPollSettingsViewModel(string header, PollType polltype, ConfigState modbusSettState, SelectedAddressConvention selConv, ThemesControl themeController)
            : base(header, polltype, modbusSettState, selConv, themeController)
        {
        }
    }
}