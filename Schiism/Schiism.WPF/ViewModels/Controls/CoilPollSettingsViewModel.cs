using Schiism.Core.Abstractions.IPC.States;
using Schiism.Core.Enums;
using Schiism.WPF.Services;
using Schiism.WPF.ViewModels.Abstractions;

namespace Schiism.WPF.ViewModels.Controls
{
    public class CoilPollSettingsViewModel : PollSettingsViewModel
    {
        public CoilPollSettingsViewModel(string header, PollType polltype, IWPFConfigState modbusSettState, ThemeService themeController)
            : base(header, polltype, modbusSettState, themeController)
        {
        }
    }
}
