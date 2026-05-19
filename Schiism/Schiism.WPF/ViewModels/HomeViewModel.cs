// <copyright file="HomeViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using Microsoft.Win32;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using Schiism.Core.Models.IPC.DTOs.Streams;
    using Schiism.WPF.Models;
    using Schiism.WPF.Services;
    using Schiism.WPF.Views;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Windows;

    public class HomeViewModel : BindableBase, INotifyPropertyChanged
    {
        private readonly IWPFConfigState modbusSettState;
        private readonly IStreamDataState<ModbusData> modbusDataState;
        private readonly IStreamDataState<ConnectionDiagnostics> connDiagState;

        // Private variables
        private string title;
        private bool nonBoolData;
        private bool endianEnable;
        private bool hexData;
        private string[] addressList;

        // Dropdown contents
        private ObservableCollection<string> addressConventions;
        private string selectedAddressConvention;

        // Visibility control
        private ObservableCollection<Visibility> colsVis;

        // ViewModel grid elements
        private ObservableCollection<string> shiftColumn;
        private ObservableCollection<ModbusRow>[] modbusRows;

        // ViewModel Commands
        private DelegateCommand? saveClick;
        private DelegateCommand? loadClick;
        private DelegateCommand? exitClick;
        // private DelegateCommand? connClick;
        private DelegateCommand? settClick;
        private DelegateCommand? themesClick;
        private DelegateCommand? aboutClick;

        // ViewModel constructor
        public HomeViewModel(
            IDialogService dialogService,
            IWPFConfigState modbusSettState,
            IStreamDataState<ModbusData> modbusDataState,
            IStreamDataState<ConnectionDiagnostics> connDiagState)
        {
            this.modbusSettState = modbusSettState;
            this.modbusDataState = modbusDataState;
            this.connDiagState = connDiagState;

            title = "PVA MODBUS TCP Client";
            addressList = ["0", "20", "40", "60", "80", "100"];
            nonBoolData = false;
            endianEnable = false;
            hexData = false;

            // Dropdown contents
            addressConventions = ["Register Address (starting from 0)", "Register Number (starting from 1)"];
            selectedAddressConvention = addressConventions.First();

            // Visibility control
            colsVis = new ObservableCollection<Visibility> { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };

            // ViewModel grid elements
            shiftColumn = new ObservableCollection<string>();
            modbusRows = new ObservableCollection<ModbusRow>[6];

            UpdateShiftColumn(); // Build the initial shift column
            UpdateModbusTable(); // Build the initial table based on default parameters in the Model

            modbusSettState.PropertyChanged += this.ModbusSettChanged;
            modbusDataState.PropertyChanged += this.ModbusDataChanged;

            // Nothing needed for this, data just gets passed up
            // connDiagState.PropertyChanged += this.ConnDiagChanged;
        }

        // Enum control
        public ObservableCollection<EnumOption<PollType>> PollTypes { get; } =
        [
            new() { Value = PollType.CoilStatus,        Display = "Coil Status" },
            new() { Value = PollType.InputStatus,       Display = "Input Status" },
            new() { Value = PollType.HoldingRegisters,  Display = "Holding Registers" },
            new() { Value = PollType.InputRegisters,    Display = "Input Registers" }
        ];

        public ObservableCollection<EnumOption<DataSize>> DataSizes { get; } =
        [
            new() { Value = DataSize.Bit16,             Display = "16 Bit" },
            new() { Value = DataSize.Bit32,             Display = "32 Bit" },
            new() { Value = DataSize.Bit64,             Display = "64 Bit" },
        ];

        public ObservableCollection<EnumOption<Endian>> Endians { get; } =
        [
            new () {Value = Endian.BigEndian,           Display = "Big Endian"},
            new () {Value = Endian.LittleEndian,        Display = "Little Endian"},
            new () {Value = Endian.BigEndianSW,         Display = "Big Endian Byte Swap"},
            new () {Value = Endian.LittleEndianSW,      Display = "Little Endian Byte Swap"},
        ];

        public ObservableCollection<EnumOption<NumericBase>> NumericBases { get; } =
        [
            new () {Value = NumericBase.Decimal,        Display = "Decimal"},
            new () {Value = NumericBase.Integer,        Display = "Integer"},
            new () {Value = NumericBase.Hexadecimal,    Display = "Hexadecimal"},
            new () {Value = NumericBase.Binary,         Display = "Binary"},
            new () {Value = NumericBase.Float,          Display = "Float"},
        ];

        // Service Singleton instances
        public ThemeController TS => ThemeController.Instance;

        // Public instances of the ViewModel for control in the View
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public bool NonBoolData
        {
            get => nonBoolData;
            set => SetProperty(ref nonBoolData, value);
        }

        public bool EndianEnable
        {
            get => endianEnable;
            set => SetProperty(ref endianEnable, value);
        }

        public bool HexData
        {
            get => hexData;
            set => SetProperty(ref hexData, value);
        }

        public string[] AddressList
        {
            get => addressList;
            set => SetProperty(ref addressList, value);
        }

        public ObservableCollection<string> AddressConventions { get => addressConventions; }

        public string SelectedAddressConvention
        {
            get => selectedAddressConvention;
            set
            {
                SetProperty(ref selectedAddressConvention, value);

                // Update Shift Column, since we have changed how to express the information here
                UpdateShiftColumn();
            }
        }

        public ObservableCollection<Visibility> ColsVis
        {
            get => colsVis;
            set => SetProperty(ref colsVis, value);
        }

        // Grid collections
        public ObservableCollection<string> ShiftColumn => shiftColumn;

        public ObservableCollection<ModbusRow>[] ModbusRows => modbusRows;

        // WPF Public Command properties
        public DelegateCommand SaveClick =>
            saveClick ??= new DelegateCommand(ExecuteSaveClick);

        public DelegateCommand LoadClick =>
            loadClick ??= new DelegateCommand(ExecuteLoadClick);

        public DelegateCommand ExitClick =>
            exitClick ??= new DelegateCommand(ExecuteExitClick);

        //public DelegateCommand ConnClick =>
        //    connClick ??= new DelegateCommand(ExecuteConnClick);

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
                SaveDeviceId = modbusSettState.DeviceId,
                SaveStartAddress = modbusSettState.StartAddress,
                SaveLength = modbusSettState.DataLength,
                SavePollType = modbusSettState.SelectedPollType,
                SaveNumericBase = modbusSettState.SelectedNumericBase,
                SaveDataSize = modbusSettState.SelectedDataSize,
                SaveEndian = modbusSettState.SelectedEndian,
                SaveAsciiEnable = modbusSettState.AsciiEnable,

                SaveAddressConv = selectedAddressConvention,
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

            modbusSettState.Update(loadData);

            // ViewModel specific save parameter
            selectedAddressConvention = lD.SaveAddressConv;

            OnPropertyChanged(nameof(SelectedAddressConvention));

            // Since you're updating these parameters in the Service, your subscription from the constructor will catch this and update the UI automatically.
            // Load --> Update Service --> Subscription pings --> Table is rebuilt
        }

        public void ExecuteExitClick()
        {
            // Close the app!
            Application.Current.Shutdown();
        }

        //public void ExecuteConnClick()
        //{
        //    // Looks a bit strange, but effectively works as a toggle! Press it once to connect (false case), press it again to stop (true case).
        //    if (this.MS.ConnectEngage)
        //    {
        //        this.MS.ConnectEngage = false;
        //    }
        //    else
        //    {
        //        this.MS.Connection();
        //    }
        //}

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
            // Simply update the MODBUS table (shape) if we see a change here
            if (e.PropertyName is nameof(modbusSettState.DataLength))
            {
                // Update MODBUS table, since the shape of the names and data may have changed here
                UpdateModbusTable();
            }

            // Starting Address should update the table headers as well as call a table update, in case the starting address requires a length change
            if (e.PropertyName is nameof(modbusSettState.StartAddress))
            {
                // Update the address headers that sit above the data columns
                for (int i = 0; i < addressList.Length; i++)
                {
                    ushort startAdd = Convert.ToUInt16(modbusSettState.StartAddress);
                    int addr = startAdd + i * 20;
                    addressList[i] = addr.ToString();
                }

                OnPropertyChanged(nameof(AddressList));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (You shouldn't need this here. Address is changed in View --> Address is updated in Service --> Length is updated in Service in response to the change in Address --> Change in Length gets pushed up to here and runs the above table update call)
            }

            // If either the DataLength or the SelectedDataSize change, make changes accordingly
            if (e.PropertyName is nameof(modbusSettState.DataLength) or nameof(modbusSettState.SelectedDataSize))
            {
                if (modbusSettState.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters)
                {
                    // If we're attempting to poll an odd number of registers while in a numeric base that requires an even number of registers, reduce the length until it is a multiple of 2 to ensure we don't exceed the length with our data display.
                    if (modbusSettState.SelectedDataSize == DataSize.Bit32 && modbusSettState.DataLength % 2 != 0 && modbusSettState.DataLength > 2)
                    {
                        modbusSettState.DataLength = (byte)(modbusSettState.DataLength - modbusSettState.DataLength % 2);
                    }

                    // If we're attempting to poll a number of registers that isn't a multiple of 4 while in a numeric base that requires a multiple of 4, reduce the length until it is a multiple of 4 to ensure we don't exceed the length with our data display.
                    else if (modbusSettState.SelectedDataSize == DataSize.Bit64 && modbusSettState.DataLength % 4 != 0 && modbusSettState.DataLength > 4)
                    {
                        modbusSettState.DataLength = (byte)(modbusSettState.DataLength - modbusSettState.DataLength % 4);
                    }
                }

                if (modbusSettState.SelectedNumericBase is NumericBase.Float)
                {
                    // Force data size update if it is currently set to 16-Bit while attempting to use Floating Point as the Numeric Base
                    if (modbusSettState.SelectedDataSize == DataSize.Bit16)
                    {
                        modbusSettState.SelectedDataSize = DataSize.Bit32;
                    }

                    // Update the available data sizes for Floating Point (32 bit and 64 bit only)
                    this.DataSizes.Clear();
                    DataSizes.Add(new() {Value = DataSize.Bit32, Display = "32 Bit", });
                    DataSizes.Add(new() {Value = DataSize.Bit64, Display = "64 Bit", });
                }
                else
                {
                    // Update the available data sizes for non-Floating Point numeric bases (all 3)
                    this.DataSizes.Clear();
                    DataSizes.Add(new() { Value = DataSize.Bit16, Display = "16 Bit", });
                    DataSizes.Add(new() { Value = DataSize.Bit32, Display = "32 Bit", });
                    DataSizes.Add(new() { Value = DataSize.Bit64, Display = "64 Bit", });
                }

                if (modbusSettState.SelectedDataSize is DataSize.Bit16)
                {
                    if (modbusSettState.SelectedNumericBase is NumericBase.Float)
                    {
                        // Force numeric base update if it is currently set to Floating Point while attempting to use 16-Bit as the Data Size
                        modbusSettState.SelectedNumericBase = NumericBase.Decimal;
                    }

                    // Update the available numeric bases for 16-bit (all but Floating Point)
                    this.NumericBases.Clear();
                    NumericBases.Add(new() { Value = NumericBase.Decimal, Display = "Decimal", });
                    NumericBases.Add(new() { Value = NumericBase.Integer, Display = "Integer", });
                    NumericBases.Add(new() { Value = NumericBase.Hexadecimal, Display = "Hexadecimal", });
                    NumericBases.Add(new() { Value = NumericBase.Binary, Display = "Binary", });
                }
                else
                {
                    // Update the available numeric bases for non-16-Bit data sizes (all 5)
                    this.NumericBases.Clear();
                    NumericBases.Add(new() { Value = NumericBase.Decimal, Display = "Decimal", });
                    NumericBases.Add(new() { Value = NumericBase.Integer, Display = "Integer", });
                    NumericBases.Add(new() { Value = NumericBase.Hexadecimal, Display = "Hexadecimal", });
                    NumericBases.Add(new() { Value = NumericBase.Binary, Display = "Binary", });
                    NumericBases.Add(new() { Value = NumericBase.Float, Display = "Float", });
                }

                endianEnable = modbusSettState.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters;
                OnPropertyChanged(nameof(this.EndianEnable));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (MIGHT NEED TO PUT THIS BACK IN)
            }

            if (e.PropertyName is nameof(modbusSettState.SelectedPollType))
            {
                // Update selection availability, according to current settings (ex. allow ASCII display only when using Hex as the numeric base)
                nonBoolData = modbusSettState.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters;
                hexData = modbusSettState.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters && modbusSettState.SelectedNumericBase is NumericBase.Hexadecimal;
                endianEnable = modbusSettState.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters;

                OnPropertyChanged(nameof(NonBoolData));
                OnPropertyChanged(nameof(EndianEnable));
                OnPropertyChanged(nameof(HexData));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (MIGHT NEED TO PUT THIS BACK IN)
            }

            if (e.PropertyName is nameof(modbusSettState.SelectedNumericBase))
            {
                // Update selection availability, according to current settings (ex. allow ASCII display only when using Hex as the numeric base)
                hexData = modbusSettState.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters && modbusSettState.SelectedNumericBase is NumericBase.Hexadecimal;
                OnPropertyChanged(nameof(HexData));
            }
        }

        private void ModbusDataChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(modbusDataState.Contents.Data))
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
            string[] namesCache = new string[modbusSettState.DataLength];
            for (int i = 0; i < namesCache.Length; i++)
            {
                namesCache[i] = string.Empty; // Initialize the cache with empty strings to avoid null issues
            }

            // Retrieve the current names from every existing row of MODBUS data for the cache
            for (int i = 0; i < modbusRows.Length; i++)
            {
                // Prevent null issues on the first run (should probably be put in the constructor tbh...
                if (modbusRows[i] == null)
                {
                    modbusRows[i] = new ObservableCollection<ModbusRow>();
                }

                for (int j = 0; j < modbusRows[i].Count; j++)
                {
                    int idx = i * 20 + j; // Calculate the overall index based on column and row

                    // Only save this name for the new display if we know that we'll see it in the new length.
                    // Otherwise, we'll risk exceeding the size of the length cache with a name we won't even need.
                    if (idx < modbusSettState.DataLength)
                    {
                        string? temp = modbusRows[i][j].Name;
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

                modbusRows[i].Clear();
            }

            // Calculate how many columns we need based on the lengthm which may have changed (integer division)
            byte reqCols = (byte)(((modbusSettState.DataLength - 1) / 20) + 1);

            // Add new MODBUS rows for the configured length with the names cache
            for (int i = 0; i < reqCols; i++)
            {
                // Calculate how many rows we need in each column, ensuring we don't exceed the total length
                byte reqRows = (byte)Math.Min((modbusSettState.DataLength - 20) * i, 20);

                // Add the new names and data iteratively to the new table
                for (int j = 0; j < reqRows; j++)
                {
                    int idx = i * 20 + j; // Calculate the overall index based on column and row
                    modbusRows[i].Add(new ModbusRow(namesCache[idx], string.Empty)); // Populate the name, data remains empty for now
                }
            }

            // Determine which columns should be visible, based on the provided length of the data
            colsVis.Clear();
            for (int i = 0; i < 6; i++)
            {
                colsVis.Add(modbusSettState.DataLength > i * 20 ? Visibility.Visible : Visibility.Collapsed);
            }

            OnPropertyChanged(nameof(ModbusRows));
        }

        // Update only the data in the table
        private void UpdateModbusData()
        {
            // Loop through all 6 column pairs of MODBUS names and data
            for (int i = 0; i < modbusRows.Length; i++)
            {
                // Loop through each of the twenty rows
                for (int j = 0; j < modbusRows[i].Count; j++)
                {
                    int idx = i * 20 + j; // Calculate the overall index based on current  column and row

                    // Only try to take the MODBUS data if we have a connection and if the index is within the bounds of the current length.
                    // i.e. the user can change the desired data length prior to connecting, so we don't necessarily want to try reading data here (it may not exist yet)
                    string data;
                    if (connDiagState.Contents.IsConnected && idx < modbusSettState.DataLength)
                    {
                        // Retrieve existing item if present; otherwise create one instance
                        data = modbusDataState.Contents.Data[idx].ToString() ?? new string(string.Empty);
                    }
                    else
                    {
                        data = new string(string.Empty);
                    }

                    modbusRows[i][j].Data = data;
                }
            }

            // Notify the UI of element updates
            OnPropertyChanged(nameof(ModbusRows)); // Might not be needed, since this is an ObservableCollection...
        }

        private void UpdateShiftColumn()
        {
            // Update ShiftColumn contents, since the values here will need to be modified as a result of the convention changing
            shiftColumn.Clear();
            for (int i = 0; i < 20; i++)
            {
                // This will print each index as i if counting from 0, or i+1 if counting from 1
                string content = $"+{(selectedAddressConvention == "Register Address (starting from 0)" ? i : i + 1)}";
                shiftColumn.Add(new string(content));
            }

            OnPropertyChanged(nameof(ShiftColumn));
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
