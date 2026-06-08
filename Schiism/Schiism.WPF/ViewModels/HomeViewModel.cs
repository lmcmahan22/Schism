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
    using Schiism.Core.Configuration.Enums;
    using Schiism.Core.Configuration.FileControl;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.StateWrappers;
    using Schiism.Core.IPC.Streams;
    using Schiism.WPF.Tabs;
    using Schiism.WPF.Models;
    using Schiism.WPF.ViewModels.Abstractions;
    using Schiism.WPF.ViewModels.Tabs;
    using Schiism.WPF.Views;

    public class HomeViewModel : BindableBase, INotifyPropertyChanged
    {
        // Private variables
        private string title;
        private ObservableCollection<string> addressList;

        // ViewModel grid elements
        private ObservableCollection<string> shiftColumn;

        // Poll tab control
        private PollSettingsViewModel selectedPollTab;

        private readonly ILogger logger;

        private double watchColumnWidth = 50;
        private double nameColumnWidth = 100;
        private double dataColumnWidth = 475;

        // ViewModel Commands
        private DelegateCommand? saveClick;
        private DelegateCommand? loadClick;
        private DelegateCommand? exitClick;
        private DelegateCommand? settClick;
        private DelegateCommand? themesClick;
        private DelegateCommand? aboutClick;
        private DelegateCommand<double?> resizeWatchColumnCommand;
        private DelegateCommand<double?> resizeNameColumnCommand;
        private DelegateCommand<double?> resizeDataColumnCommand;

        public ConfigState ModbusSettState { get; }

        public StreamStore<ModbusDataDTO> ModbusDataState { get; }

        public StreamStore<ConnDiagDTO> ConnDiagState { get; }

        public InitStatus InitStatus { get; }

        public ObservableCollection<PollSettingsViewModel> PollTabs { get; }

        public ThemesControl ThemeService { get; }

        public SelectedAddressConvention SelConv { get; }

        // Inidicates server connection from the connection diagnostics state, but also turns off, if the initialized state has been turned off.
        public bool ServerConnected { get; set; }

        // ViewModel constructor
        public HomeViewModel(
            IDialogService dialogService,
            ConfigState ModbusSettState,
            StreamStore<ModbusDataDTO> ModbusDataState,
            StreamStore<ConnDiagDTO> ConnDiagState,
            InitStatus InitStatus,
            ThemesControl ThemeService,
            SelectedAddressConvention SelConv,
            ILoggerFactory factory)
        {
            this.ModbusSettState = ModbusSettState;
            this.ModbusDataState = ModbusDataState;
            this.ConnDiagState = ConnDiagState;
            this.InitStatus = InitStatus;
            this.ThemeService = ThemeService;
            this.SelConv = SelConv;
            this.logger = factory.CreateLogger<HomeViewModel>();

            title = "PVA MODBUS TCP Client";

            // ViewModel grid elements
            shiftColumn = new ObservableCollection<string>();

            addressList = [ModbusSettState.StartAddress.ToString()];

            // Selected Address Convention Handling

            UpdateShiftColumn(); // Build the initial shift column
            UpdateModbusTable(); // Build the initial table based on default parameters

            this.ModbusSettState.PropertyChanged += this.ModbusSettChanged;
            this.ModbusDataState.PropertyChanged += this.ModbusDataChanged;
            this.ConnDiagState.PropertyChanged += this.ConnDiagStateChanged;
            this.InitStatus.PropertyChanged += this.InitStatusChanged;
            this.SelConv.PropertyChanged += this.AddrConvChanged;

            // Subscribe to the tab viewmodels too? How else will you know if the Address Convention changed?
            // Only show the Status Coil and Register tabs, since these are the only two that Vision PLCs use.
            PollTabs = new ObservableCollection<PollSettingsViewModel>
            {
                new CoilPollSettingsViewModel(
                    "Status Coils",
                    PollType.CoilStatus,
                    this.ModbusSettState,
                    this.SelConv,
                    this.ThemeService),

                new RegisterPollSettingsViewModel(
                    "Holding Registers",
                    PollType.HoldingRegisters,
                    this.ModbusSettState,
                    this.SelConv,
                    this.ThemeService),
            };
        }

        public PollSettingsViewModel SelectedPollTab
        {
            get => selectedPollTab;
            set
            {
                ModbusSettState.SelectedPollType = value.PollTyp; // Update the PollType in the Service based on the selected tab, which will then trigger the subscription updates to update the UI accordingly
                SetProperty(ref selectedPollTab, value);
                OnPropertyChanged(nameof(ModbusSettState.SelectedPollType));
            }
        }

        // Public instances of the ViewModel for control in the View
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public ObservableCollection<string> AddressList
        {
            get => addressList;
            set => SetProperty(ref addressList, value);
        }

        // Jointed column widths
        public double WatchColumnWidth
        {
            get => watchColumnWidth;
            set
            {
                watchColumnWidth = value;
                OnPropertyChanged();
            }
        }

        public double NameColumnWidth
        {
            get => nameColumnWidth;
            set
            {
                nameColumnWidth = value;
                OnPropertyChanged();
            }
        }

        public double DataColumnWidth
        {
            get => dataColumnWidth;
            set
            {
                dataColumnWidth = value;
                OnPropertyChanged();
            }
        }

        // Grid collections
        public ObservableCollection<string> ShiftColumn => shiftColumn;

        // Get is required in order for XAML to see this
        public ObservableCollection<ModbusRow> ModbusRows { get; } = new ObservableCollection<ModbusRow>();

        // WPF Public Command properties
        public DelegateCommand<double?> ResizeWatchColumnCommand =>
            resizeWatchColumnCommand ??= new DelegateCommand<double?>(OnWatchResizeColumn);

        public DelegateCommand<double?> ResizeNameColumnCommand =>
            resizeNameColumnCommand ??= new DelegateCommand<double?>(OnNameResizeColumn);

        public DelegateCommand<double?> ResizeDataColumnCommand =>
            resizeDataColumnCommand ??= new DelegateCommand<double?>(OnDataResizeColumn);

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

        public void ExecuteSaveClick()
        {
            // Create a SaveData object with the current state of the ViewModel
            ConfigSaveData sD = new ConfigSaveData
            {
                SaveDeviceId = ModbusSettState.DeviceId,
                SaveStartAddress = ModbusSettState.StartAddress,
                SavePollType = ModbusSettState.SelectedPollType,
                SaveNumericBase = ModbusSettState.SelectedNumericBase,
                SaveDataSize = ModbusSettState.SelectedDataSize,
                SaveEndian = ModbusSettState.SelectedEndian,
                SaveAsciiEnable = ModbusSettState.AsciiEnable,

                // Get this from the Settings tab.
                SaveAddressConv = this.SelConv.Selected,
            };
            Save(sD);
        }

        public void ExecuteLoadClick()
        {
            ConfigSaveData lD = Load();

            // Update ViewModel properties with loaded data
            // NOTE: Setting the public instances of variables runs the logic in the setters implicitly! ;)
            SettingsConfigDTO loadData = new SettingsConfigDTO(
                null,
                null,
                lD.SaveStartAddress,
                null,
                null,
                null,
                lD.SaveDeviceId,
                lD.SaveDataSize,
                lD.SavePollType,
                lD.SaveAsciiEnable,
                lD.SaveNumericBase,
                lD.SaveEndian,
                null,
                null);

            ModbusSettState.Update(loadData);

            // ViewModel specific save parameter
            this.SelConv.Selected = lD.SaveAddressConv;

            OnPropertyChanged(nameof(this.SelConv.Selected));

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
            if (e.PropertyName is nameof(ModbusSettState.StartAddress) or nameof(ModbusSettState.DataLength))
            {
                // Update Address Headers, since we may now have more or less columns to work with
                UpdateAddressHeaders();
            }

            // Simply update the MODBUS table (shape) if we see a change here
            if (e.PropertyName is nameof(ModbusSettState.DataLength))
            {
                // Update MODBUS table, since the shape of the names and data may have changed here
                // Marshall this as well, if you haven't already!
                UpdateModbusTable();
            }
        }

        private void AddrConvChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Update the shift column if the opened settings tab changes the convention, since the values in this column are based on the convention
            if (e.PropertyName is nameof(this.SelConv.Selected))
            {
                UpdateShiftColumn();
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

        private void ConnDiagStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(this.ConnDiagState.Contents))
            {
                this.ServerConnected = (this.ConnDiagState.Contents?.IsConnected ?? false) && this.InitStatus.IsInitialized;
                this.OnPropertyChanged(nameof(this.ServerConnected));
            }
        }

        private void InitStatusChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(this.InitStatus.IsInitialized))
            {
                this.ServerConnected = (this.ConnDiagState.Contents?.IsConnected ?? false) && this.InitStatus.IsInitialized;
                this.OnPropertyChanged(nameof(this.ServerConnected));
            }
        }

        // Build the table for the main UI based on the provided MODBUS Data from the MODBUSService Model
        private void UpdateModbusTable()
        {
            // Prepare a cache of the exisitng name and updating data, in case we need to keep some of the current names around
            string[] namesCache = new string[ModbusSettState.DataLength];
            bool[] updatingCache = new bool[ModbusSettState.DataLength];

            for (int i = 0; i < ModbusSettState.DataLength; i++)
            {
                namesCache[i] = string.Empty; // Initialize the cache with empty strings to avoid null issues
                updatingCache[i] = false; // Initialize the cache with false to avoid null issues
            }

            // Retrieve the current names from every existing row of MODBUS data for the cache
            // Keep this above the loops, so you don't spam in and out of the main thread
            var app = Application.Current;

            if (app == null)
            {
                return;
            }

            app.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < ModbusRows.Count; i++)
                {
                    // Only save this name for the new display if we know that we'll see it in the new length.
                    // Otherwise, we'll risk exceeding the size of the length cache with a name we won't even need.
                    if (i < ModbusSettState.DataLength)
                    {
                        string? temp = ModbusRows[i].Name;
                        bool? tempUpdating = ModbusRows[i].IsUpdating;

                        if (temp == null)
                        {
                            namesCache[i] = string.Empty;
                        }
                        else
                        {
                            namesCache[i] = temp;
                        }

                        if (!tempUpdating.HasValue)
                        {
                            updatingCache[i] = false;
                        }
                        else
                        {
                            updatingCache[i] = tempUpdating.Value;
                        }
                    }

                    ModbusRows.Clear();
                }
            });

            app.Dispatcher.Invoke(() =>
            {
                // Add new MODBUS rows for the configured length with the names cache
                for (int i = 0; i < ModbusSettState.DataLength; i++)
                {
                    ModbusRows.Add(new ModbusRow(namesCache[i], string.Empty, updatingCache[i])); // Populate the name, data remains empty for now

                    // logger.LogInformation($"At Table Update: ModbusRow[{i}] = {namesCache[i]}, {string.Empty}");
                }

                OnPropertyChanged(nameof(ModbusRows));
            });
        }

        // Marshall these methods!
        private void UpdateAddressHeaders()
        {
            // Update the address headers that sit above the name/data columns
            var app = Application.Current;

            if (app == null)
            {
                return;
            }

            app.Dispatcher.Invoke(() =>
            {
                addressList.Clear();
                int numCols = (Math.Max(0, this.ModbusSettState.DataLength - 1) / 20) + 1;

                for (int i = 0; i < numCols; i++)
                {
                    int startAdd = Convert.ToUInt16(ModbusSettState.StartAddress);
                    int addr = startAdd + (i * 20);
                    addressList.Add(addr.ToString());
                }

                OnPropertyChanged(nameof(AddressList));
            });

            // Update MODBUS table, since the shape of the names and data may have changed here
            // UpdateModbusTable(); (You shouldn't need this here. Address is changed in View --> Address is updated in Service --> Length is updated in Service in response to the change in Address --> Change in Length gets pushed up to here and runs the above table update call)

        }

        // Update only the data in the table
        private void UpdateModbusData()
        {
            // Keep this above the loops, so you don't spam in and out of the main thread
            var app = Application.Current;

            if (app == null)
            {
                return;
            }

            app.Dispatcher.Invoke(() =>
            {
                // Loop through all 6 column pairs of MODBUS names and data
                if (ModbusRows == null)
                {
                    return;
                }

                for (int i = 0; i < ModbusRows.Count; i++)
                {

                    // Don't update if this row isn't checked for updating
                    if (!ModbusRows[i].IsUpdating)
                    {
                        continue; // Skip updating this row's data if it's currently being updated by the user in the UI
                    }

                    // Only try to take the MODBUS data if we have a connection and if the index is within the bounds of the current length.
                    // i.e. the user can change the desired data length prior to connecting, so we don't necessarily want to try reading data here (it may not exist yet)
                    string data = string.Empty;

                    // Null check helps prevent a data race, since I managed to get here before ConnDiageState properly initialized.
                    // I want to review the project for data races in the code cleanup phase
                    // The design approach with this here is to take snapshots of the desired parameters, then act only when they're permissable.

                    var contents = ConnDiagState.Contents;

                    if (contents != null &&
                        contents.IsConnected &&
                        i < ModbusDataState.Contents.Data.Count)
                    {

                        // Retrieve existing item if present; otherwise create one instance
                        data = ModbusDataState.Contents.Data[i]?.ToString() ?? string.Empty;
                    }

                    ModbusRows[i].Data = data;

                    // logger.LogInformation($"At Data Update: ModbusRow[{i}][{j}] = {ModbusRows[i].Name}, {data}");
                }

                OnPropertyChanged(nameof(ModbusRows));
            });
        }

        private void UpdateShiftColumn()
        {
            // Update the RawModbusData collection with the new data on the main UI thread
            // Keep this above the loops, so you don't spam in and out of the main thread
            var app = Application.Current;

            if (app == null)
            {
                return;
            }

            app.Dispatcher.Invoke(() =>
            {
                // Update ShiftColumn contents, since the values here will need to be modified as a result of the convention changing
                shiftColumn.Clear();
                for (int i = 0; i < 20; i++)
                {
                    // This will print each index as i if counting from 0, or i+1 if counting from 1
                    string content = $"+{(this.SelConv.Selected == AddressConvention.RegisterAddress ? i : i + 1)}";
                    this.shiftColumn.Add(new string(content));
                }

                OnPropertyChanged(nameof(this.ShiftColumn));
            });
        }

        // TAKE THESE OUT OF THE VIEWMODEL! THIS HAS NOTHING TO DO WITH WPF!

        // Logic to save data
        private void Save(ConfigSaveData sD)
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
        private ConfigSaveData Load()
        {
            // Nullable SaveData dummy until we get the data from the json file
            ConfigSaveData? lD = new();

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
                    lD = JsonSerializer.Deserialize<ConfigSaveData>(json, options);
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
                return new ConfigSaveData();
            }
        }

        private void OnWatchResizeColumn(double? delta)
        {
            if (!delta.HasValue)
            {
                return;
            }

            WatchColumnWidth = Math.Max(
                50,
                WatchColumnWidth + delta.Value);
        }

        private void OnNameResizeColumn(double? delta)
        {
            if (!delta.HasValue)
            {
                return;
            }

            NameColumnWidth = Math.Max(
                50,
                NameColumnWidth + delta.Value);
        }

        private void OnDataResizeColumn(double? delta)
        {
            if (!delta.HasValue)
            {
                return;
            }

            DataColumnWidth = Math.Max(
                50,
                DataColumnWidth + delta.Value);
        }
    }
}
