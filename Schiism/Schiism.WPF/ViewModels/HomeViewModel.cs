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
    using Schiism.WPF.Services;
    using Schiism.WPF.Models;
    using Schiism.WPF.Models.Implementations.States;
    using Schiism.WPF.ViewModels.Abstractions;
    using Schiism.WPF.ViewModels.Controls;
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

        public ObservableCollection<PollSettingsViewModel> PollTabs { get; }

        public ThemeService ThemeService { get; }

        // ViewModel constructor
        public HomeViewModel(
            IDialogService dialogService,
            IWPFConfigState ModbusSettState,
            WPFStreamDataState<ModbusData> ModbusDataState,
            WPFStreamDataState<ConnectionDiagnostics> ConnDiagState,
            WPFInitializedState InitState,
            ThemeService ThemeService,
            ILoggerFactory loggerFactory)
        {
            this.ModbusSettState = ModbusSettState;
            this.ModbusDataState = ModbusDataState;
            this.ConnDiagState = ConnDiagState;
            this.InitState = InitState;
            this.ThemeService = ThemeService;
            this.logger = loggerFactory.CreateLogger<HomeViewModel>();

            title = "PVA MODBUS TCP Client";

            // ViewModel grid elements
            shiftColumn = new ObservableCollection<string>();

            addressList = [ModbusSettState.StartAddress.ToString()];

            // Selected Address Convention Handling

            UpdateShiftColumn(); // Build the initial shift column
            UpdateModbusTable(); // Build the initial table based on default parameters in the Model

            ModbusSettState.PropertyChanged += this.ModbusSettChanged;
            ModbusDataState.PropertyChanged += this.ModbusDataChanged;

            // Subscribe to the tab viewmodels too? How else will you know if the Address Convention changed?
            PollTabs = new ObservableCollection<PollSettingsViewModel>
            {
                new CoilPollSettingsViewModel(
                    "Status Coils",
                    PollType.CoilStatus,
                    this.ModbusSettState,
                    this.ThemeService),

                new CoilPollSettingsViewModel(
                    "Status Inputs",
                    PollType.InputStatus,
                    this.ModbusSettState,
                    this.ThemeService),

                new RegisterPollSettingsViewModel(
                    "Holding Registers",
                    PollType.HoldingRegisters,
                    this.ModbusSettState,
                    this.ThemeService),

                new RegisterPollSettingsViewModel(
                    "Input Registers",
                    PollType.InputRegisters,
                    this.ModbusSettState,
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

        // Grid collections
        public ObservableCollection<string> ShiftColumn => shiftColumn;

        // Get is required in order for XAML to see this
        public ObservableCollection<ModbusRow> ModbusRows { get; } = new ObservableCollection<ModbusRow>();

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
                SavePollType = ModbusSettState.SelectedPollType,
                SaveNumericBase = ModbusSettState.SelectedNumericBase,
                SaveDataSize = ModbusSettState.SelectedDataSize,
                SaveEndian = ModbusSettState.SelectedEndian,
                SaveAsciiEnable = ModbusSettState.AsciiEnable,

                // Get this from the Settings tab.
                SaveAddressConv = ModbusSettState.SelectedAddressConvention,
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
            ModbusSettState.SelectedAddressConvention = lD.SaveAddressConv;

            OnPropertyChanged(nameof(ModbusSettState.SelectedAddressConvention));

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
            // Prepare a cache of the exisitng name data, in case we need to keep some of the current names around
            string[] namesCache = new string[ModbusSettState.DataLength];
            for (int i = 0; i < namesCache.Length; i++)
            {
                namesCache[i] = string.Empty; // Initialize the cache with empty strings to avoid null issues
            }

            // Retrieve the current names from every existing row of MODBUS data for the cache
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < ModbusRows.Count; i++)
                {
                    // Only save this name for the new display if we know that we'll see it in the new length.
                    // Otherwise, we'll risk exceeding the size of the length cache with a name we won't even need.
                    if (i < ModbusSettState.DataLength)
                    {
                        string? temp = ModbusRows[i].Name;
                        if (temp == null)
                        {
                            namesCache[i] = string.Empty;
                        }
                        else
                        {
                            namesCache[i] = temp;
                        }
                    }

                    ModbusRows.Clear();
                }
            });

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Add new MODBUS rows for the configured length with the names cache
                for (int i = 0; i < ModbusSettState.DataLength; i++)
                {
                    ModbusRows.Add(new ModbusRow(namesCache[i], string.Empty)); // Populate the name, data remains empty for now

                    // logger.LogInformation($"At Table Update: ModbusRow[{i}] = {namesCache[i]}, {string.Empty}");
                }

                OnPropertyChanged(nameof(ModbusRows));
            });
        }

        // Marshall these methods!
        private void UpdateAddressHeaders()
        {
            // Update the address headers that sit above the name/data columns
            Application.Current.Dispatcher.Invoke(() =>
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Loop through all 6 column pairs of MODBUS names and data
                for (int i = 0; i < ModbusRows.Count; i++)
                {
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Update ShiftColumn contents, since the values here will need to be modified as a result of the convention changing
                shiftColumn.Clear();
                for (int i = 0; i < 20; i++)
                {
                    // This will print each index as i if counting from 0, or i+1 if counting from 1
                    string content = $"+{(ModbusSettState.SelectedAddressConvention == AddressConvention.RegisterAddress ? i : i + 1)}";
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
