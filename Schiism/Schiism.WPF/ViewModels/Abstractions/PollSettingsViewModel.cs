namespace Schiism.WPF.ViewModels.Abstractions
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using Schiism.Core.Configuration;
    using Schiism.WPF.Tabs;
    using Schiism.Core.Configuration.Enums;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.WPF.Models;

    public abstract class PollSettingsViewModel : BindableBase
    {

        // private variables
        private string title;

        // Header control
        public string Header { get; }

        public PollType PollTyp { get; }

        // DI
        public ConfigState ModbusSettState { get; }

        public ThemesControl ThemeService { get; }

        // Constructor
        public PollSettingsViewModel(string header, PollType polltype, ConfigState modbusSettState, SelectedAddressConvention SelConv, ThemesControl themeController)
        {
            this.Header = header;
            this.PollTyp = polltype;
            this.ModbusSettState = modbusSettState;
            this.ThemeService = themeController;
        }

        // Public instances of the ViewModel for control in the View
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        // Don't need to define Device ID, since it's in the modbusSettState already. We just yoink it directly.

        // Starting Address control to handle strings at UI level before Model level
        public string StartAddress
        {
            get => ModbusSettState.StartAddress.ToString();
            set
            {
                // temp variable to help store the incoming decimal value, after possible hex conversion
                ushort attemptDecVal = 0;

                // StartAddress changed to ushort, because this string handling should be managed netirely in the UI
                // If the value contains "h"
                if (value.Contains('h'))
                {
                    // Get rid of the "h" at the end ex. "Ah -> A"
                    string trun = value.Substring(0, value.Length - 1);

                    // convert hex string into a decimal int ex. "A -> 10"
                    attemptDecVal = Convert.ToUInt16(trun, 16);
                }

                // If the value contains just numbers (no "h")
                else
                {
                    attemptDecVal = Convert.ToUInt16(value);
                }

                // We can now confirm that the attempted decimal converted value is a short (1-65535), so we can type cast it!
                ushort decVal = Convert.ToUInt16(attemptDecVal);

                this.ModbusSettState.StartAddress = decVal;
                OnPropertyChanged(nameof(this.ModbusSettState.StartAddress));
            }
        }

        // Enum control
        public ObservableCollection<EnumOption<AddressConvention>> AddressConventions { get; } =
        [
            new() { Value = AddressConvention.RegisterAddress, Display = "Register Address (starting from 0)" },
            new() { Value = AddressConvention.RegisterNumber,  Display = "Register Number (starting from 1)" },
        ];

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }
    }
}
