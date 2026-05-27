// <copyright file="HomeViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Windows;
    using Microsoft.Extensions.Logging;
    using Microsoft.Win32;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using Schiism.Core.Models.IPC.DTOs.Streams;
    using Schiism.WPF.Models;
    using Schiism.WPF.Models.Enums;
    using Schiism.WPF.Models.Implementations.States;
    using Schiism.WPF.Services;
    using Schiism.WPF.Views;

    public class HomeViewModel : BindableBase, INotifyPropertyChanged
    {
        // Private variables
        private string title;
        private string[] addressList;
        private AddressConvention selectedAddressConvention;

        // Visibility control
        private ObservableCollection<Visibility> colsVis;

        // ViewModel grid elements
        private ObservableCollection<string> shiftColumn;

        private readonly ILogger logger;

        // ViewModel Commands
        private DelegateCommand? saveClick;
        private DelegateCommand? loadClick;
        private DelegateCommand? exitClick;
        private DelegateCommand? settClick;
        private DelegateCommand? themesClick;
        private DelegateCommand? aboutClick;

        public IWPFConfigState ModbusSettState { get; }

        public WPFStreamDataState<ModbusData> ModbusDataState { get; }

        public WPFStreamDataState<ConnectionDiagnostics> ConnDiagState { get; }

        public WPFInitializedState InitState { get; }

        // ViewModel constructor
        public HomeViewModel(
            IDialogService dialogService,
            IWPFConfigState ModbusSettState,
            WPFStreamDataState<ModbusData> ModbusDataState,
            WPFStreamDataState<ConnectionDiagnostics> ConnDiagState,
            WPFInitializedState InitState,
            ILoggerFactory loggerFactory)
        {
            this.ModbusSettState = ModbusSettState;
            this.ModbusDataState = ModbusDataState;
            this.ConnDiagState = ConnDiagState;
            this.InitState = InitState;
            this.logger = loggerFactory.CreateLogger<HomeViewModel>();

            title = "PVA MODBUS TCP Client";
            addressList = ["0", "20", "40", "60", "80", "100"];

            // Visibility control
            colsVis = new ObservableCollection<Visibility> { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };

            // ViewModel grid elements
            shiftColumn = new ObservableCollection<string>();

            // Selected Address Convention Handling

            UpdateShiftColumn(); // Build the initial shift column
            UpdateModbusTable(); // Build the initial table based on default parameters in the Model

            ModbusSettState.PropertyChanged += this.ModbusSettChanged;
            ModbusDataState.PropertyChanged += this.ModbusDataChanged;
        }

        // Enum control

        public ObservableCollection<EnumOption<AddressConvention>> AddressConventions { get; } =
        [
            new() { Value = AddressConvention.RegisterAddress, Display = "Register Address (starting from 0)" },
            new() { Value = AddressConvention.RegisterNumber,  Display = "Register Number (starting from 1)" },
        ];

        public AddressConvention SelectedAddressConvention
        {
            get => selectedAddressConvention;
            set
            {
                SetProperty(ref selectedAddressConvention, value);
                UpdateShiftColumn();
            }
        }

        public ObservableCollection<EnumUIOption<PollType>> PollTypes { get; } =
        [
            new() { Value = PollType.CoilStatus,        Display = "Coil Status",            IsEnabled = true },
            new() { Value = PollType.InputStatus,       Display = "Input Status",           IsEnabled = true },
            new() { Value = PollType.HoldingRegisters,  Display = "Holding Registers",      IsEnabled = true },
            new() { Value = PollType.InputRegisters,    Display = "Input Registers",        IsEnabled = true }
        ];

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

        // Service Singleton instances
        public ThemeController TS => ThemeController.Instance;

        // Public instances of the ViewModel for control in the View
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public bool NonBoolData =>
            ModbusSettState.SelectedPollType is
                PollType.HoldingRegisters or
                PollType.InputRegisters;

        public bool EndianEnable =>
            ModbusSettState.SelectedPollType is
                PollType.HoldingRegisters or
                PollType.InputRegisters;

        public bool HexData =>
            (ModbusSettState.SelectedPollType is
                PollType.HoldingRegisters or
                PollType.InputRegisters)
            && ModbusSettState.SelectedNumericBase is NumericBase.Hexadecimal;

        public string[] AddressList
        {
            get => addressList;
            set => SetProperty(ref addressList, value);
        }

        public ObservableCollection<Visibility> ColsVis
        {
            get => colsVis;
            set => SetProperty(ref colsVis, value);
        }

        // Grid collections
        public ObservableCollection<string> ShiftColumn => shiftColumn;

        // Get is required in order for XAML to see this
        public ObservableCollection<ModbusRow>[] ModbusRows { get; } = 
            [new ObservableCollection<ModbusRow>(),
            new ObservableCollection<ModbusRow>(),
            new ObservableCollection<ModbusRow>(),
            new ObservableCollection<ModbusRow>(),
            new ObservableCollection<ModbusRow>(),
            new ObservableCollection<ModbusRow>()];

        // WPF Public Command properties
        public DelegateCommand SaveClick =>
            saveClick ??= new DelegateCommand(ExecuteSaveClick);

        public DelegateCommand LoadClick =>
            loadClick ??= new DelegateCommand(ExecuteLoadClick);

        public DelegateCommand ExitClick =>
            exitClick ??= new DelegateCommand(ExecuteExitClick);

        public DelegateCommand SettClick =>
            settClick ??= new DelegateCommand(ExecuteSettClick);

        public DelegateCommand ThemesClick =>
            themesClick ??= new DelegateCommand(ExecuteThemesClick);

        public DelegateCommand AboutClick =>
            aboutClick ??= new DelegateCommand(ExecuteAboutClick);

        // Remaining variables (ex. ScanRate) will need to be managed in the ConnSettings ViewModel!

        public void ExecuteSaveClick()
        {
            // Create a SaveData object with the current state of the ViewModel
            SaveData sD = new SaveData
            {
                SaveDeviceId = ModbusSettState.DeviceId,
                SaveStartAddress = ModbusSettState.StartAddress,
                SaveLength = ModbusSettState.DataLength,
                SavePollType = ModbusSettState.SelectedPollType,
                SaveNumericBase = ModbusSettState.SelectedNumericBase,
                SaveDataSize = ModbusSettState.SelectedDataSize,
                SaveEndian = ModbusSettState.SelectedEndian,
                SaveAsciiEnable = ModbusSettState.AsciiEnable,

                SaveAddressConv = SelectedAddressConvention,
            };
            Save(sD);
        }

        public void ExecuteLoadClick()
        {
            SaveData lD = Load();

            // Update ViewModel properties with loaded data
            // NOTE: Setting the public instances of variables runs the logic in the setters implicitly! ;)
            SettingsConfig loadData = new SettingsConfig(
                null,
                lD.SaveLength,
                lD.SaveStartAddress,
                null,
                null,
                null,
                lD.SaveDeviceId,
                lD.SaveDataSize,
                lD.SavePollType,
                lD.SaveAsciiEnable,
                lD.SaveNumericBase,
                lD.SaveEndian
                );

            ModbusSettState.Update(loadData);

            // ViewModel specific save parameter
            SelectedAddressConvention = lD.SaveAddressConv;

            OnPropertyChanged(nameof(SelectedAddressConvention));

            // Since you're updating these parameters in the Service, your subscription from the constructor will catch this and update the UI automatically.
            // Load --> Update Service --> Subscription pings --> Table is rebuilt
        }

        public void ExecuteExitClick()
        {
            // Close the app!
            Application.Current.Shutdown();
        }

        public void ExecuteSettClick()
        {
            // Create the Connection Settings window
            Window connSettings = new Window
            {
                // Open the window
                Content = new ConnSettings(),
                SizeToContent = SizeToContent.WidthAndHeight,
                Topmost = true,
            };
            connSettings.ShowDialog();
        }

        public void ExecuteThemesClick()
        {
            // Create the About window
            Window themes = new Window
            {
                // Open the window
                Content = new Themes(),
                SizeToContent = SizeToContent.WidthAndHeight,
                Topmost = true,
            };
            themes.ShowDialog();
        }

        public void ExecuteAboutClick()
        {
            // Create the About window
            Window about = new Window
            {
                // Open the window
                Content = new About(),
                SizeToContent = SizeToContent.WidthAndHeight,
                Topmost = true,
            };
            about.ShowDialog();
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        // React to MODBUSService updates, depending on what updated
        private void ModbusSettChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Starting Address should update the table headers as well as call a table update, in case the starting address requires a length change
            if (e.PropertyName is nameof(ModbusSettState.StartAddress))
            {
                UpdateAddressHeaders();
            }

            // If either the DataLength or the SelectedDataSize change, make changes accordingly
            if (e.PropertyName is nameof(ModbusSettState.DataLength))
            {
                // In case the user entered a length that's incompatible with the current data size, simply change the length back on them
                NormalizeLengthForDataSize();

                // Update impacted UI
                UpdateUIForLengthAndDataSize();
            }

            if (e.PropertyName is nameof(ModbusSettState.SelectedDataSize))
            {
                NormalizeLengthForDataSize();
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

            // Simply update the MODBUS table (shape) if we see a change here
            if (e.PropertyName is nameof(ModbusSettState.DataLength))
            {
                // Update MODBUS table, since the shape of the names and data may have changed here
                // Marshall this as well, if you haven't already!
                UpdateModbusTable();
            }
        }

        private void ModbusDataChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ModbusDataState.Contents))
            {
                // Update MODBUS Data in the UI
                // NOTE: since the RawModbusData updates via a loop on another thread, this method will get called constantly!
                UpdateModbusData();
            }
        }

        // Build the table for the main UI based on the provided MODBUS Data from the MODBUSService Model
        private void UpdateModbusTable()
        {
            // Prepare a cache of the name data, in case we need to keep some of the current names around
            string[] namesCache = new string[ModbusSettState.DataLength];
            for (int i = 0; i < namesCache.Length; i++)
            {
                namesCache[i] = string.Empty; // Initialize the cache with empty strings to avoid null issues
            }

            // Retrieve the current names from every existing row of MODBUS data for the cache

            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < ModbusRows.Length; i++)
                {
                    for (int j = 0; j < ModbusRows[i].Count; j++)
                    {
                        int idx = i * 20 + j; // Calculate the overall index based on column and row

                        // Only save this name for the new display if we know that we'll see it in the new length.
                        // Otherwise, we'll risk exceeding the size of the length cache with a name we won't even need.
                        if (idx < ModbusSettState.DataLength)
                        {
                            string? temp = ModbusRows[i][j].Name;
                            if (temp == null)
                            {
                                namesCache[idx] = string.Empty;
                            }
                            else
                            {
                                namesCache[idx] = temp;
                            }
                        }
                    }

                    ModbusRows[i].Clear();
                }
            });

            // Calculate how many columns we need based on the lengthm which may have changed (integer division)
            byte reqCols = (byte)(((ModbusSettState.DataLength - 1) / 20) + 1);

            // Add new MODBUS rows for the configured length with the names cache
            for (int i = 0; i < reqCols; i++)
            {
                // Calculate how many rows we need in each column, ensuring we don't exceed the total length
                int remaining = Math.Max(0, ModbusSettState.DataLength - (i * 20));
                byte reqRows = (byte)Math.Min(remaining, 20);

                // Add the new names and data iteratively to the new table
                for (int j = 0; j < reqRows; j++)
                {
                    int idx = i * 20 + j; // Calculate the overall index based on column and row
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ModbusRows[i].Add(new ModbusRow(namesCache[idx], string.Empty)); // Populate the name, data remains empty for now
                    });

                    // logger.LogInformation($"At Table Update: ModbusRow[{i}][{j}] = {namesCache[idx]}, {string.Empty}");
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Determine which columns should be visible, based on the provided length of the data
                colsVis.Clear();
                for (int i = 0; i < 6; i++)
                {
                    colsVis.Add(ModbusSettState.DataLength > i * 20 ? Visibility.Visible : Visibility.Collapsed);
                }

                OnPropertyChanged(nameof(ModbusRows));
            });
        }

        // Update only the data in the table
        private void UpdateModbusData()
        {
            // Keep this above the loops, so you don't spam in and out of the main thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Loop through all 6 column pairs of MODBUS names and data
                for (int i = 0; i < ModbusRows.Length; i++)
                {
                    // Loop through each of the twenty rows
                    for (int j = 0; j < ModbusRows[i].Count; j++)
                    {
                        int idx = i * 20 + j; // Calculate the overall index based on current  column and row

                        // Only try to take the MODBUS data if we have a connection and if the index is within the bounds of the current length.
                        // i.e. the user can change the desired data length prior to connecting, so we don't necessarily want to try reading data here (it may not exist yet)
                        string data = string.Empty;

                        // Null check helps prevent a data race, since I managed to get here before ConnDiageState properly initialized.
                        // I want to review the project for data races in the code cleanup phase
                        // The design approach with this here is to take snapshots of the desired parameters, then act only when they're permissable.

                        var contents = ConnDiagState.Contents;

                        if (contents != null &&
                            contents.IsConnected &&
                            idx < ModbusDataState.Contents.Data.Count)
                        {

                            // Retrieve existing item if present; otherwise create one instance
                            data = ModbusDataState.Contents.Data[idx]?.ToString() ?? string.Empty;
                        }

                        ModbusRows[i][j].Data = data;

                        // logger.LogInformation($"At Data Update: ModbusRow[{i}][{j}] = {ModbusRows[i][j].Name}, {data}");
                    }
                }
            });
        }

        // Marshall these methods!
        private void UpdateAddressHeaders()
        {
            // Update the address headers that sit above the data columns
            for (int i = 0; i < addressList.Length; i++)
            {
                ushort startAdd = Convert.ToUInt16(ModbusSettState.StartAddress);
                int addr = startAdd + i * 20;
                addressList[i] = addr.ToString();
            }

            // Update the RawModbusData collection with the new data on the main UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(AddressList));
            });

            // Update MODBUS table, since the shape of the names and data may have changed here
            // UpdateModbusTable(); (You shouldn't need this here. Address is changed in View --> Address is updated in Service --> Length is updated in Service in response to the change in Address --> Change in Length gets pushed up to here and runs the above table update call)

        }

        private void NormalizeLengthForDataSize()
        {
            if (ModbusSettState.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters)
            {
                // If we're attempting to poll an odd number of registers while in a numeric base that requires an even number of registers, reduce the length until it is a multiple of 2 to ensure we don't exceed the length with our data display.
                if (ModbusSettState.SelectedDataSize == DataSize.Bit32 && ModbusSettState.DataLength % 2 != 0 && ModbusSettState.DataLength > 2)
                {
                    ModbusSettState.DataLength = (byte)(ModbusSettState.DataLength - ModbusSettState.DataLength % 2);
                }

                // If we're attempting to poll a number of registers that isn't a multiple of 4 while in a numeric base that requires a multiple of 4, reduce the length until it is a multiple of 4 to ensure we don't exceed the length with our data display.
                else if (ModbusSettState.SelectedDataSize == DataSize.Bit64 && ModbusSettState.DataLength % 4 != 0 && ModbusSettState.DataLength > 4)
                {
                    ModbusSettState.DataLength = (byte)(ModbusSettState.DataLength - ModbusSettState.DataLength % 4);
                }
            }

            OnPropertyChanged(nameof(ModbusSettState.DataLength));
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
                OnPropertyChanged(nameof(NonBoolData));
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

        private void UpdateShiftColumn()
        {
            // Update the RawModbusData collection with the new data on the main UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Update ShiftColumn contents, since the values here will need to be modified as a result of the convention changing
                shiftColumn.Clear();
                for (int i = 0; i < 20; i++)
                {
                    // This will print each index as i if counting from 0, or i+1 if counting from 1
                    string content = $"+{(SelectedAddressConvention == AddressConvention.RegisterAddress ? i : i + 1)}";
                    this.shiftColumn.Add(new string(content));
                }

                OnPropertyChanged(nameof(this.ShiftColumn));
            });
        }

        // Logic to save data
        private void Save(SaveData sD)
        {
            // Specify the saving directory upon pop up. If it doesn't exist, create it!
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Schiism");
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            // Write up the json
            string json = JsonSerializer.Serialize(new
            {
                sD.SaveDeviceId,
                sD.SaveStartAddress,
                sD.SaveLength,
                sD.SavePollType,
                sD.SaveNumericBase,
                sD.SaveDataSize,
                sD.SaveEndian,
                sD.SaveAsciiEnable,
                sD.SaveAddressConv,
            });

            // Display a File Explorer window for the user to save the json file
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "userData"; // Default file name
            saveFileDialog.DefaultExt = ".sav"; // Default file extension

            // Filter files by extension. The format is "Description|Pattern"
            saveFileDialog.Filter = "Schiism Save File (.sav)|*.sav|All files (*.*)|*.*";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // Show save file dialog box
            bool? result = saveFileDialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = saveFileDialog.FileName;

                // Example of saving text from a TextBox named 'txtEditor'
                try
                {
                    File.WriteAllText(filename, json);
                    MessageBox.Show("File saved successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}");
                }
            }
        }

        // Logic to load data
        private SaveData Load()
        {
            // Nullable SaveData dummy until we get the data from the json file
            SaveData? lD = new();

            OpenFileDialog? openFileDialog = new OpenFileDialog();

            // Optional: Configure the dialog box
            openFileDialog.FileName = "userData"; // Default file name
            openFileDialog.DefaultExt = ".sav"; // Default file extension
            openFileDialog.Filter = "Schiism Save File (.sav)|*.sav|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Initial Directory

            // Show open file dialog box
            bool? result = openFileDialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                };

                try
                {
                    lD = JsonSerializer.Deserialize<SaveData>(json, options);
                }
                catch
                {
                    MessageBox.Show("Failed to load the file. The file may be corrupted or not in the correct format.");
                }
            }

            if (lD != null)
            {
                // return loaded data
                return lD;
            }
            else
            {
                // return empty data (i.e. nothing is loaded for the user)
                return new SaveData();
            }
        }
    }
}
