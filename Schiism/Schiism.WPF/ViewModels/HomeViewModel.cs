// <copyright file="HomeViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.ViewModels
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Windows;
    using Microsoft.Win32;
    using Schiism.Models;
    using Schiism.Services;
    using Schiism.Views;

    public class HomeViewModel : BindableBase, INotifyPropertyChanged
    {

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
        private DelegateCommand? connClick;
        private DelegateCommand? settClick;
        private DelegateCommand? themesClick;
        private DelegateCommand? aboutClick;

        // ViewModel constructor
        public HomeViewModel(IDialogService dialogService)
        {
            this.title = "PVA MODBUS TCP Client";
            this.addressList = ["0", "20", "40", "60", "80", "100"];
            this.nonBoolData = false;
            this.endianEnable = false;
            this.hexData = false;

            // Dropdown contents
            this.addressConventions = ["Register Address (starting from 0)", "Register Number (starting from 1)"];
            this.selectedAddressConvention = this.addressConventions.First();

            // Visibility control
            this.colsVis = new ObservableCollection<Visibility> { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };

            // ViewModel grid elements
            this.shiftColumn = new ObservableCollection<string>();
            this.modbusRows = new ObservableCollection<ModbusRow>[6];

            this.UpdateShiftColumn(); // Build the initial shift column
            this.UpdateModbusTable(); // Build the initial table based on default parameters in the Model

            // Logic for handling reactions to updates from the MODBUSService
            // NOTE: We don't need this for the ThemeService, because there's no logic that we need to perform in response to anything there. The View still gets access to everything in both services, even without this logic for the MS.
            if (this.MS == null)
            {
                throw new Exception("MS is null");
            }

            this.MS.PropertyChanged += this.MSPropertyChanged;
        }

        // Service Singleton instances
        public ThemeService TS => ThemeService.Instance;

        // public MODBUSService MS => MODBUSService.Instance;

        // Public instances of the ViewModel for control in the View
        public string Title
        {
            get => this.title;
            set => this.SetProperty(ref this.title, value);
        }

        public bool NonBoolData
        {
            get => this.nonBoolData;
            set => this.SetProperty(ref this.nonBoolData, value);
        }

        public bool EndianEnable
        {
            get => this.endianEnable;
            set => this.SetProperty(ref this.endianEnable, value);
        }

        public bool HexData
        {
            get => this.hexData;
            set => this.SetProperty(ref this.hexData, value);
        }

        public string[] AddressList
        {
            get => this.addressList;
            set => this.SetProperty(ref this.addressList, value);
        }

        public ObservableCollection<string> AddressConventions { get => this.addressConventions; }

        public string SelectedAddressConvention
        {
            get => this.selectedAddressConvention;
            set
            {
                this.SetProperty(ref this.selectedAddressConvention, value);

                // Update Shift Column, since we have changed how to express the information here
                this.UpdateShiftColumn();
            }
        }

        public ObservableCollection<Visibility> ColsVis
        {
            get => this.colsVis;
            set => this.SetProperty(ref this.colsVis, value);
        }

        // Grid collections
        public ObservableCollection<string> ShiftColumn => this.shiftColumn;

        public ObservableCollection<ModbusRow>[] ModbusRows => this.modbusRows;

        // Public Command properties
        public DelegateCommand SaveClick =>
            this.saveClick ??= new DelegateCommand(this.ExecuteSaveClick);

        public DelegateCommand LoadClick =>
            this.loadClick ??= new DelegateCommand(this.ExecuteLoadClick);

        public DelegateCommand ExitClick =>
            this.exitClick ??= new DelegateCommand(this.ExecuteExitClick);

        public DelegateCommand ConnClick =>
            this.connClick ??= new DelegateCommand(this.ExecuteConnClick);

        public DelegateCommand SettClick =>
            this.settClick ??= new DelegateCommand(this.ExecuteSettClick);

        public DelegateCommand ThemesClick =>
            this.themesClick ??= new DelegateCommand(this.ExecuteThemesClick);

        public DelegateCommand AboutClick =>
            this.aboutClick ??= new DelegateCommand(this.ExecuteAboutClick);

        // View Model Visibility element bases from Model boolean! :D
        public Visibility ErrorContents
        {
            // Use IsNullOrEmpty for safety (handles null and empty)
            get => string.IsNullOrEmpty(this.MS.ErrMess) ? Visibility.Collapsed : Visibility.Visible;
        }

        public void ExecuteSaveClick()
        {
            // Create a SaveData object with the current state of the ViewModel
            SaveData sD = new SaveData
            {
                SaveDeviceId = this.MS.DeviceId,
                SaveStartAddress = this.MS.StartAddress,
                SaveLength = this.MS.DataLength,
                SaveDataType = this.MS.SelectedDataType,
                SaveNumericBase = this.MS.SelectedNumericBase,
                SaveDataSize = this.MS.SelectedDataSize,
                SaveEndian = this.MS.SelectedEndian,
                SaveAsciiEnable = this.MS.AsciiEnable,

                SaveAddressConv = this.selectedAddressConvention,
            };
            this.Save(sD);
        }

        public void ExecuteLoadClick()
        {
            SaveData lD = this.Load();

            // Update ViewModel properties with loaded data
            // NOTE: Setting the public instances of variables runs the logic in the setters implicitly! ;)
            this.MS.DeviceId = lD.SaveDeviceId;
            this.MS.DataLength = lD.SaveLength;
            this.MS.StartAddress = lD.SaveStartAddress;
            this.MS.SelectedDataType = lD.SaveDataType;
            this.MS.SelectedNumericBase = lD.SaveNumericBase;
            this.MS.SelectedDataSize = lD.SaveDataSize;
            this.MS.SelectedEndian = lD.SaveEndian;
            this.MS.AsciiEnable = lD.SaveAsciiEnable;

            // ViewModel specific save parameter
            this.selectedAddressConvention = lD.SaveAddressConv;

            this.OnPropertyChanged(nameof(this.SelectedAddressConvention));

            // Since you're updating these parameters in the Service, your subscription from the constructor will catch this and update the UI automatically.
            // Load --> Update Service --> Subscription pings --> Table is rebuilt
        }

        public void ExecuteExitClick()
        {
            // Close the app!
            Application.Current.Shutdown();
        }

        public void ExecuteConnClick()
        {
            // Looks a bit strange, but effectively works as a toggle! Press it once to connect (false case), press it again to stop (true case).
            if (this.MS.ConnectEngage)
            {
                this.MS.ConnectEngage = false;
            }
            else
            {
                this.MS.Connection();
            }
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
            this.RaisePropertyChanged(propertyName);
        }

        // React to MODBUSService updates, depending on what updated
        private void MSPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Simply update the MODBUS table (shape) if we see a change here
            if (e.PropertyName is nameof(this.MS.DataLength))
            {
                // Update MODBUS table, since the shape of the names and data may have changed here
                this.UpdateModbusTable();
            }

            // Starting Address should update the table headers as well as call a table update, in case the starting address requires a length change
            if (e.PropertyName is nameof(this.MS.StartAddress))
            {
                // Update the address headers that sit above the data columns
                for (int i = 0; i < this.addressList.Length; i++)
                {
                    ushort startAdd = Convert.ToUInt16(this.MS.StartAddress);
                    int addr = startAdd + (i * 20);
                    this.addressList[i] = addr.ToString();
                }

                this.OnPropertyChanged(nameof(this.AddressList));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (You shouldn't need this here. Address is changed in View --> Address is updated in Service --> Length is updated in Service in response to the change in Address --> Change in Length gets pushed up to here and runs the above table update call)
            }

            // If either the DataLength or the SelectedDataSize change, make changes accordingly
            if (e.PropertyName is nameof(this.MS.DataLength) or nameof(this.MS.SelectedDataSize))
            {
                if (this.MS.SelectedDataType is "Holding Registers" or "Input Registers")
                {
                    // If we're attempting to poll an odd number of registers while in a numeric base that requires an even number of registers, reduce the length until it is a multiple of 2 to ensure we don't exceed the length with our data display.
                    if ((this.MS.SelectedDataSize == "32-Bit") && (this.MS.DataLength % 2 != 0) && (this.MS.DataLength > 2))
                    {
                        this.MS.DataLength = (byte)(this.MS.DataLength - (this.MS.DataLength % 2));
                    }

                    // If we're attempting to poll a number of registers that isn't a multiple of 4 while in a numeric base that requires a multiple of 4, reduce the length until it is a multiple of 4 to ensure we don't exceed the length with our data display.
                    else if ((this.MS.SelectedDataSize == "64-Bit") && (this.MS.DataLength % 4 != 0) && (this.MS.DataLength > 4))
                    {
                        this.MS.DataLength = (byte)(this.MS.DataLength - (this.MS.DataLength % 4));
                    }
                }

                if (this.MS.SelectedNumericBase is "Floating Point")
                {
                    // Force data size update if it is currently set to 16-Bit while attempting to use Floating Point as the Numeric Base
                    if (this.MS.SelectedDataSize == "16-Bit")
                    {
                        this.MS.SelectedDataSize = "32-Bit";
                    }

                    // Update the available data sizes for Floating Point (32 bit and 64 bit only)
                    this.MS.DataSizes = ["32-Bit", "64-Bit"];
                }
                else
                {
                    // Update the available data sizes for non-Floating Point numeric bases (all 3)
                    this.MS.DataSizes = ["16-Bit", "32-Bit", "64-Bit"];
                }

                if (this.MS.SelectedDataSize is "16-Bit")
                {
                    if (this.MS.SelectedNumericBase is "Floating Point")
                    {
                        // Force numeric base update if it is currently set to Floating Point while attempting to use 16-Bit as the Data Size
                        this.MS.SelectedNumericBase = "Decimal";
                    }

                    // Update the available numeric bases for 16-bit (all but Floating Point)
                    this.MS.NumericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary" };
                }
                else
                {
                    // Update the available numeric bases for non-16-Bit data sizes (all 5)
                    this.MS.NumericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary", "Floating Point" };
                }

                this.endianEnable = this.MS.SelectedDataType is "Holding Registers" or "Input Registers";
                this.OnPropertyChanged(nameof(this.EndianEnable));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (MIGHT NEED TO PUT THIS BACK IN)
            }

            if (e.PropertyName is nameof(this.MS.SelectedDataType))
            {
                // Update selection availability, according to current settings (ex. allow ASCII display only when using Hex as the numeric base)
                this.nonBoolData = this.MS.SelectedDataType is "Holding Registers" or "Input Registers";
                this.hexData = (this.MS.SelectedDataType is "Holding Registers" or "Input Registers") && (this.MS.SelectedNumericBase is "Hexadecimal");
                this.endianEnable = this.MS.SelectedDataType is "Holding Registers" or "Input Registers";

                this.OnPropertyChanged(nameof(this.NonBoolData));
                this.OnPropertyChanged(nameof(this.EndianEnable));
                this.OnPropertyChanged(nameof(this.HexData));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (MIGHT NEED TO PUT THIS BACK IN)
            }

            if (e.PropertyName is nameof(this.MS.SelectedNumericBase))
            {
                // Update selection availability, according to current settings (ex. allow ASCII display only when using Hex as the numeric base)
                this.hexData = (this.MS.SelectedDataType is "Holding Registers" or "Input Registers") && (this.MS.SelectedNumericBase is "Hexadecimal");
                this.OnPropertyChanged(nameof(this.HexData));
            }

            if (e.PropertyName is nameof(this.MS.RawModbusData))
            {
                // Update MODBUS Data in the UI
                // NOTE: since the RawModbusData updates via a loop on another thread, this method will get called constantly!
                this.UpdateModbusData();
            }

            // Simply pass along the error message contents from the catch block in the MODBUS Service
            if (e.PropertyName is nameof(this.MS.ErrMess))
            {
                this.OnPropertyChanged(nameof(this.ErrorContents));
            }
        }

        // Build the table for the main UI based on the provided MODBUS Data from the MODBUSService Model
        private void UpdateModbusTable()
        {
            // Force disconnect if we're currently connected, since we're changing parameters that would affect the amount of data that we poll
            this.MS.IsConnected = false;

            // Prepare a cache of the name data, in case we need to keep some of the current names around
            string[] namesCache = new string[this.MS.DataLength];
            for (int i = 0; i < namesCache.Length; i++)
            {
                namesCache[i] = string.Empty; // Initialize the cache with empty strings to avoid null issues
            }

            // Retrieve the current names from every existing row of MODBUS data for the cache
            for (int i = 0; i < this.modbusRows.Length; i++)
            {
                // Prevent null issues on the first run (should probably be put in the constructor tbh...
                if (this.modbusRows[i] == null)
                {
                    this.modbusRows[i] = new ObservableCollection<ModbusRow>();
                }

                for (int j = 0; j < this.modbusRows[i].Count; j++)
                {
                    int idx = (i * 20) + j; // Calculate the overall index based on column and row

                    // Only save this name for the new display if we know that we'll see it in the new length.
                    // Otherwise, we'll risk exceeding the size of the length cache with a name we won't even need.
                    if (idx < this.MS.DataLength)
                    {
                        string? temp = this.modbusRows[i][j].Name;
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

                this.modbusRows[i].Clear();
            }

            // Calculate how many columns we need based on the lengthm which may have changed (integer division)
            var reqCols = ((this.MS.DataLength - 1) / 20) + 1;

            // Add new MODBUS rows for the configured length with the names cache
            for (int i = 0; i < reqCols; i++)
            {
                // Calculate how many rows we need in each column, ensuring we don't exceed the total length
                var reqRows = Math.Min(this.MS.DataLength - (20 * i), 20);

                // Add the new names and data iteratively to the new table
                for (int j = 0; j < reqRows; j++)
                {
                    int idx = (i * 20) + j; // Calculate the overall index based on column and row
                    this.modbusRows[i].Add(new ModbusRow(namesCache[idx], string.Empty)); // Populate the name, data remains empty for now
                }
            }

            // Determine which columns should be visible, based on the provided length of the data
            this.colsVis.Clear();
            for (int i = 0; i < 6; i++)
            {
                this.colsVis.Add(this.MS.DataLength > (i * 20) ? Visibility.Visible : Visibility.Collapsed);
            }

            this.OnPropertyChanged(nameof(this.ModbusRows));
        }

        // Update only the data in the table
        private void UpdateModbusData()
        {
            // Loop through all 6 column pairs of MODBUS names and data
            for (int i = 0; i < this.modbusRows.Length; i++)
            {
                // Loop through each of the twenty rows
                for (int j = 0; j < this.modbusRows[i].Count; j++)
                {
                    int idx = (i * 20) + j; // Calculate the overall index based on current  column and row

                    // Only try to take the MODBUS data if we have a connection and if the index is within the bounds of the current length.
                    // i.e. the user can change the desired data length prior to connecting, so we don't necessarily want to try reading data here (it may not exist yet)
                    string data;
                    if (this.MS.IsConnected && idx < this.MS.DataLength)
                    {
                        // Retrieve existing item if present; otherwise create one instance
                        data = this.MS.RawModbusData[idx].ToString() ?? new string(string.Empty);
                    }
                    else
                    {
                        data = new string(string.Empty);
                    }

                    this.modbusRows[i][j].Data = data;
                }
            }

            // Notify the UI of element updates
            this.OnPropertyChanged(nameof(this.ModbusRows)); // Might not be needed, since this is an ObservableCollection...
        }

        private void UpdateShiftColumn()
        {
            // Update ShiftColumn contents, since the values here will need to be modified as a result of the convention changing
            this.shiftColumn.Clear();
            for (int i = 0; i < 20; i++)
            {
                // This will print each index as i if counting from 0, or i+1 if counting from 1
                string content = $"+{(this.selectedAddressConvention == "Register Address (starting from 0)" ? i : i + 1)}";
                this.shiftColumn.Add(new string(content));
            }

            this.OnPropertyChanged(nameof(this.ShiftColumn));
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
                sD.SaveDataType,
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

            var openFileDialog = new OpenFileDialog();

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
                var options = new JsonSerializerOptions
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
