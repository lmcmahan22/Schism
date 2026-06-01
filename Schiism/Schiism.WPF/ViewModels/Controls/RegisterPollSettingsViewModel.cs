using Schiism.Core.Abstractions.IPC.States;
using Schiism.Core.Enums;
using Schiism.WPF.Services;
using Schiism.WPF.Models.Enums;
using Schiism.WPF.ViewModels.Abstractions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Schiism.WPF.ViewModels.Controls
{
    public class RegisterPollSettingsViewModel : PollSettingsViewModel
    {

        public RegisterPollSettingsViewModel(string header, PollType polltype, IWPFConfigState modbusSettState, ThemeService themeController)
            : base(header, polltype, modbusSettState, themeController)
        {
            ModbusSettState.PropertyChanged += this.ModbusSettChanged;
        }

        // Register specific contents
        public ObservableCollection<EnumUIOption<DataSize>> DataSizes { get; } =
        [
            new() { Value = DataSize.Bit16,             Display = "16 Bit",            IsEnabled = true },
            new() { Value = DataSize.Bit32,             Display = "32 Bit",            IsEnabled = true },
            new() { Value = DataSize.Bit64,             Display = "64 Bit",            IsEnabled = true },
        ];

        public ObservableCollection<EnumUIOption<Endian>> Endians { get; } =
        [
            new () {Value = Endian.BigEndian,           Display = "Big Endian",                      IsEnabled = true},
            new () {Value = Endian.LittleEndian,        Display = "Little Endian",                   IsEnabled = true},
            new () {Value = Endian.BigEndianSW,         Display = "Big Endian Byte Swap",            IsEnabled = true},
            new () {Value = Endian.LittleEndianSW,      Display = "Little Endian Byte Swap",         IsEnabled = true},
        ];

        public ObservableCollection<EnumUIOption<NumericBase>> NumericBases { get; } =
        [
            new () {Value = NumericBase.Decimal,        Display = "Decimal",            IsEnabled = true},
            new () {Value = NumericBase.Integer,        Display = "Integer",            IsEnabled = true},
            new () {Value = NumericBase.Hexadecimal,    Display = "Hexadecimal",        IsEnabled = true},
            new () {Value = NumericBase.Binary,         Display = "Binary",             IsEnabled = true},
            new () {Value = NumericBase.Float,          Display = "Float",              IsEnabled = true},
        ];

        public bool EndianEnable =>
        ModbusSettState.SelectedPollType is
        PollType.HoldingRegisters or
        PollType.InputRegisters;

        public bool HexData =>
        (ModbusSettState.SelectedPollType is
            PollType.HoldingRegisters or
            PollType.InputRegisters)
        && ModbusSettState.SelectedNumericBase is NumericBase.Hexadecimal;

        // React to MODBUSService updates, depending on what updated
        private void ModbusSettChanged(object? sender, PropertyChangedEventArgs e)
        {
            // If either the DataLength or the SelectedDataSize change, make changes accordingly
            if (e.PropertyName is nameof(ModbusSettState.DataLength))
            {
                // Update impacted UI
                UpdateUIForLengthAndDataSize();
            }

            if (e.PropertyName is nameof(ModbusSettState.SelectedDataSize))
            {
                NormalizeNumericBaseForDataSize();

                // Update impacted UI
                UpdateUIForLengthAndDataSize();
            }

            if (e.PropertyName is nameof(ModbusSettState.SelectedNumericBase))
            {
                NormalizeDataSizeForNumericBase();

                // Update impacted UI
                UpdateUIForNumericBase();
            }

            if (e.PropertyName is nameof(ModbusSettState.SelectedPollType))
            {
                // Update impacted UI
                UpdateUIForPollType();
            }
        }

        private void NormalizeDataSizeForNumericBase()
        {
            if (ModbusSettState.SelectedNumericBase is NumericBase.Float)
            {
                // Update the available data sizes for Floating Point (32 bit and 64 bit only)
                UpdateDataSizesListUI(true);
            }
            else
            {
                // Update the available data sizes for non-Floating Point numeric bases (all 3)
                UpdateDataSizesListUI(false);
            }
        }

        private void NormalizeNumericBaseForDataSize()
        {

            if (ModbusSettState.SelectedDataSize is DataSize.Bit16)
            {
                // Update the available numeric bases for 16-bit (all but Floating Point)
                UpdateNumericBasesListUI(true);
            }
            else
            {
                // Update the available numeric bases for non-16-Bit data sizes (all 5)
                UpdateNumericBasesListUI(false);
            }
        }

        private void UpdateDataSizesListUI(bool partial)
        {
            // Update the RawModbusData collection with the new data on the main UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (partial)
                {
                    // ensure valid selection
                    if (ModbusSettState.SelectedDataSize == DataSize.Bit16)
                    {
                        ModbusSettState.SelectedDataSize = DataSize.Bit32;
                    }

                    // disable 16-bit option
                    var bit16 = DataSizes.First(x => x.Value == DataSize.Bit16);
                    if (bit16.IsEnabled)
                    {
                        bit16.IsEnabled = false;
                    }
                }
                else
                {
                    // enable 16-bit option
                    var bit16 = DataSizes.First(x => x.Value == DataSize.Bit16);
                    if (!bit16.IsEnabled)
                    {
                        bit16.IsEnabled = true;
                    }
                }

                OnPropertyChanged(nameof(this.DataSizes));
                OnPropertyChanged(nameof(ModbusSettState.SelectedDataSize));
            });
        }

        private void UpdateNumericBasesListUI(bool partial)
        {
            // Update the RawModbusData collection with the new data on the main UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (partial)
                {
                    // ensure valid selection
                    if (ModbusSettState.SelectedNumericBase == NumericBase.Float)
                    {
                        ModbusSettState.SelectedNumericBase = NumericBase.Decimal;
                    }

                    // disable float option
                    var floatChoice = NumericBases.First(x => x.Value == NumericBase.Float);
                    if (floatChoice.IsEnabled)
                    {
                        floatChoice.IsEnabled = false;
                    }
                }
                else
                {
                    // enable float option
                    var floatChoice = NumericBases.First(x => x.Value == NumericBase.Float);
                    if (!floatChoice.IsEnabled)
                    {
                        floatChoice.IsEnabled = true;
                    }
                }

                OnPropertyChanged(nameof(this.NumericBases));
                OnPropertyChanged(nameof(ModbusSettState.SelectedNumericBase));
            });
        }

        private void UpdateUIForPollType()
        {
            // Update the RawModbusData collection with the new data on the main UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {

                // Update selection availability, according to current settings (ex. allow ASCII display only when using Hex as the numeric base)
                // Simply call property changed, because you already set these public variables to the logic that the need to follow. Simply refresh them.
                OnPropertyChanged(nameof(EndianEnable));
                OnPropertyChanged(nameof(HexData));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (MIGHT NEED TO PUT THIS BACK IN)
            });
        }

        private void UpdateUIForNumericBase()
        {
            // Update the RawModbusData collection with the new data on the main UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Update selection availability, according to current settings (ex. allow ASCII display only when using Hex as the numeric base)
                OnPropertyChanged(nameof(HexData));
            });
        }

        private void UpdateUIForLengthAndDataSize()
        {
            // Update the RawModbusData collection with the new data on the main UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Marshall all OnPropertyChanged calls that can be invoked from the ModbusSettChanged method
                OnPropertyChanged(nameof(this.EndianEnable));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); // (MIGHT NEED TO PUT THIS BACK IN)
            });
        }
    }
}
