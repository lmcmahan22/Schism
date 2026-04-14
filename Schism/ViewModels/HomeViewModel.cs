using Microsoft.Win32;
using Schism.Models;
using Schism.Services;
using Schism.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace Schism.ViewModels
{
    public class HomeViewModel : BindableBase, INotifyPropertyChanged
    {

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        // Private variables
        private string _title;
        private bool _nonBoolData;
        private bool _endianEnable;
        private bool _hexData;
        private string[] _addressList;

        // Dropdown contents
        private ObservableCollection<string> _addressConventions;
        private string _selectedAddressConvention;

        // Visibility control
        private ObservableCollection<Visibility> _colsVis;

        // ViewModel grid elements
        private ObservableCollection<string> _shiftColumn;
        private ObservableCollection<ModbusRow>[] _modbusRows;

        // ViewModel Commands
        private DelegateCommand? _saveClick;
        private DelegateCommand? _loadClick;
        private DelegateCommand? _exitClick;
        private DelegateCommand? _connClick;
        private DelegateCommand? _settClick;
        private DelegateCommand? _themesClick;
        private DelegateCommand? _aboutClick;

        // Service Singleton instances
        public ThemeService TS => ThemeService.Instance;
        public MODBUSService MS => MODBUSService.Instance;

        // Public instances of the ViewModel for control in the View
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool NonBoolData
        {
            get => _nonBoolData;
            set => SetProperty(ref _nonBoolData, value);
        }

        public bool EndianEnable
        {
            get => _endianEnable;
            set => SetProperty(ref _endianEnable, value);
        }

        public bool HexData
        {
            get => _hexData;
            set => SetProperty(ref _hexData, value);
        }

        public string[] AddressList
        {
            get => _addressList;
            set => SetProperty(ref _addressList, value);
        }

        public ObservableCollection<string> AddressConventions { get => _addressConventions; }

        public string SelectedAddressConvention
        {
            get => _selectedAddressConvention;
            set
            {
                SetProperty(ref _selectedAddressConvention, value);

                // Update ShiftColumn contents, since the values here will need to be modified as a result of the convention changing
                _shiftColumn.Clear();
                for (int i = 0; i < 20; i++)
                {
                    // This will print each index as i if counting from 0, or i+1 if counting from 1
                    string content = $"+{(_selectedAddressConvention == "Register Address (starting from 0)" ? i : i + 1)}";
                    _shiftColumn.Add(new string(content));
                }

                OnPropertyChanged(nameof(ShiftColumn));
            }
        }

        public ObservableCollection<Visibility> ColsVis
        {
            get => _colsVis;
            set => SetProperty(ref _colsVis, value);
        }

        // Grid collections
        public ObservableCollection<string> ShiftColumn => _shiftColumn;
        public ObservableCollection<ModbusRow>[] ModbusRows => _modbusRows;

        // View Model Visibility element bases from Model boolean! :D
        public Visibility ErrorContents
        {
            // Use IsNullOrEmpty for safety (handles null and empty)
            get => string.IsNullOrEmpty(MS.ErrMess) ? Visibility.Collapsed : Visibility.Visible;
        }

        // ViewModel constructor
        public HomeViewModel(IDialogService dialogService)
        {
            _title = "Schism Home Screen";
            _addressList = [ "0", "20", "40", "60", "80", "100" ];
            _nonBoolData = false;
            _endianEnable = false;
            _hexData = false;

            // Dropdown contents
            _addressConventions = ["Register Address (starting from 0)", "Register Number (starting from 1)"];
            _selectedAddressConvention = _addressConventions.First();

            // Visibility control
            _colsVis = new ObservableCollection<Visibility> { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };

            // ViewModel grid elements
            _shiftColumn = new ObservableCollection<string>();
            _modbusRows = new ObservableCollection<ModbusRow>[6];

            // Logic for handling reactions to updates from the MODBUSService
            // NOTE: We don't need this for the ThemeService, because there's no logic that we need to perform in response to anything there. The View still gets access to everything in both services, even without this logic for the MS.
            MS.PropertyChanged += MS_PropertyChanged;
            UpdateModbusTable(); // Build the initial table based on default parameters in the Model
        }

        // React to MODBUSService updates, depending on what updated
        private void MS_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {

            // Simply update the MODBUS table (shape) if we see a change here
            if (e.PropertyName is nameof(MS.DataLength))

                // Update MODBUS table, since the shape of the names and data may have changed here
                UpdateModbusTable();

            // Starting Address should update the table headers as well as call a table update, in case the starting address requires a length change
            if (e.PropertyName is nameof(MS.StartAddress))
            {
                // Update the address headers that sit above the data columns
                for (int i = 0; i < _addressList.Length; i++)
                {
                    int addr = MS.StartAddress + (i * 20);
                    _addressList[i] = addr.ToString();
                }

                OnPropertyChanged(nameof(AddressList));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (You shouldn't need this here. Address is changed in View --> Address is updated in Service --> Length is updated in Service in response to the change in Address --> Change in Length gets pushed up to here and runs the above table update call)
            }

            // If either the DataLength or the SelectedDataSize change, make changes accordingly
            if (e.PropertyName is nameof(MS.DataLength) or nameof(MS.SelectedDataSize))
            {

                if (MS.SelectedDataType is "Holding Registers" or "Input Registers")
                {
                    // If we're attempting to poll an odd number of registers while in a numeric base that requires an even number of registers, reduce the length until it is a multiple of 2 to ensure we don't exceed the length with our data display.
                    if ((MS.SelectedDataSize == "32-Bit") && (MS.DataLength % 2 != 0))
                        MS.DataLength = (ushort)(MS.DataLength - (MS.DataLength % 2));

                    // If we're attempting to poll a number of registers that isn't a multiple of 4 while in a numeric base that requires a multiple of 4, reduce the length until it is a multiple of 4 to ensure we don't exceed the length with our data display.
                    else if ((MS.SelectedDataSize == "64-Bit") && (MS.DataLength % 4 != 0))
                        MS.DataLength = (ushort)(MS.DataLength - (MS.DataLength % 4));
                }

                if(MS.SelectedNumericBase is "Floating Point")
                {
                    // Force data size update if it is currently set to 16-Bit while attempting to use Floating Point as the Numeric Base
                    if(MS.SelectedDataSize == "16-Bit")
                        MS.SelectedDataSize = "32-Bit";

                    // Update the available data sizes for Floating Point (32 bit and 64 bit only)
                    MS.DataSizes = new ObservableCollection<string>{"32-Bit", "64-Bit"};
                }
                else
                    // Update the available data sizes for non-Floating Point numeric bases (all 3)
                    MS.DataSizes = new ObservableCollection<string> { "16-Bit", "32-Bit", "64-Bit" };

                if(MS.SelectedDataSize is "16-Bit")
                {
                    if(MS.SelectedNumericBase is "Floating Point")
                        // Force numeric base update if it is currently set to Floating Point while attempting to use 16-Bit as the Data Size
                        MS.SelectedNumericBase = "Decimal";

                    // Update the available numeric bases for 16-bit (all but Floating Point)
                    MS.NumericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary"};
                }
                else
                    // Update the available numeric bases for non-16-Bit data sizes (all 5)
                    MS.NumericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary", "Floating Point" };

                // Update selection availability, according to current settings (ex. allow ASCII display only when using Hex as the numeric base)
                _hexData = (MS.SelectedDataType is "Holding Registers" or "Input Registers") && (MS.SelectedNumericBase is "Hexadecimal");
                _endianEnable = (MS.SelectedDataType is "Holding Registers" or "Input Registers");

                OnPropertyChanged(nameof(HexData));
                OnPropertyChanged(nameof(EndianEnable));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (MIGHT NEED TO PUT THIS BACK IN)
            }

            if (e.PropertyName is nameof(MS.SelectedDataType))
            {
                // Update selection availability, according to current settings (ex. allow ASCII display only when using Hex as the numeric base)
                _nonBoolData = MS.SelectedDataType is "Holding Registers" or "Input Registers";
                _hexData = (MS.SelectedDataType is "Holding Registers" or "Input Registers") && (MS.SelectedNumericBase is "Hexadecimal");
                _endianEnable = (MS.SelectedDataType is "Holding Registers" or "Input Registers");

                OnPropertyChanged(nameof(NonBoolData));
                OnPropertyChanged(nameof(EndianEnable));
                OnPropertyChanged(nameof(HexData));

                // Update MODBUS table, since the shape of the names and data may have changed here
                // UpdateModbusTable(); (MIGHT NEED TO PUT THIS BACK IN)
            }

            if (e.PropertyName is nameof(MS.RawModbusData))
            {
                // Update MODBUS Data in the UI
                // NOTE: since the RawModbusData updates via a loop on another thread, this method will get called constantly!
                UpdateModbusData();
            }

            // Simply pass along the error message contents from the catch block in the MODBUS Service
            if (e.PropertyName is nameof(MS.ErrMess))
                OnPropertyChanged(nameof(ErrorContents));
        }

        // Build the table for the main UI based on the provided MODBUS Data from the MODBUSService Model
        private void UpdateModbusTable()
        {
            // Force disconnect if we're currently connected, since we're changing parameters that would affect the amount of data that we poll
            MS.IsConnected = false;

            // Prepare a cache of the name data, in case we need to keep some of the current names around
            string[] namesCache = new string[MS.DataLength];
            for (int i = 0; i < namesCache.Length; i++)
                namesCache[i] = ""; // Initialize the cache with empty strings to avoid null issues

            // Retrieve the current names from every existing row of MODBUS data for the cache
            for (int i = 0; i < _modbusRows.Length; i++)
            {
                // Prevent null issues on the first run (should probably be put in the constructor tbh...
                if(_modbusRows[i] == null)
                    _modbusRows[i] = new ObservableCollection<ModbusRow>();

                for (int j = 0; j < _modbusRows[i].Count; j++)
                {
                    int idx = (i * 20) + j; // Calculate the overall index based on column and row

                    // Only save this name for the new display if we know that we'll see it in the new length.
                    // Otherwise, we'll risk exceeding the size of the length cache with a name we won't even need.
                    if (idx < MS.DataLength)
                    {
                        string? temp = _modbusRows[i][j].Name;
                        if (temp == null)
                            namesCache[idx] = "";
                        else
                            namesCache[idx] = temp;
                    }
                }
                _modbusRows[i].Clear();
            }

            // Calculate how many columns we need based on the lengthm which may have changed (integer division)
            var reqCols = ((MS.DataLength - 1) / 20) + 1;

            // Add new MODBUS rows for the configured length with the names cache
            for (int i = 0; i < reqCols; i++)
            {
                // Calculate how many rows we need in each column, ensuring we don't exceed the total length
                var reqRows = Math.Min((MS.DataLength - 20 * i), 20);

                // Add the new names and data iteratively to the new table
                for (int j = 0; j < reqRows; j++) {
                    int idx = (i * 20) + j; // Calculate the overall index based on column and row
                    _modbusRows[i].Add(new ModbusRow(namesCache[idx], "")); // Populate the name, data remains empty for now
                }
            }

            // Determine which columns should be visible, based on the provided length of the data
            _colsVis.Clear();
            for (int i = 0; i < 6; i++)
                _colsVis.Add(MS.DataLength > (i * 20) ? Visibility.Visible : Visibility.Collapsed);

            OnPropertyChanged(nameof(ModbusRows));
        }

        // Update only the data in the table
        private void UpdateModbusData()
        {
            // Loop through all 6 column pairs of MODBUS names and data
            for (int i = 0; i < _modbusRows.Length; i++)
            {
                // Loop through each of the twenty rows
                for (int j = 0; j < _modbusRows[i].Count; j++)
                {
                    int idx = (i * 20) + j; // Calculate the overall index based on current  column and row

                    // Only try to take the MODBUS data if we have a connection and if the index is within the bounds of the current length.
                    // i.e. the user can change the desired data length prior to connecting, so we don't necessarily want to try reading data here (it may not exist yet)
                    string data;
                    if (MS.IsConnected && idx < MS.DataLength)
                        // Retrieve existing item if present; otherwise create one instance
                        data = MS.RawModbusData[idx].ToString() ?? new string("");
                    else
                        data = new string("");
                    _modbusRows[i][j].Data = data;
                }
            }

            // Notify the UI of element updates
            OnPropertyChanged(nameof(ModbusRows)); // Might not be needed, since this is an ObservableCollection...
        }


        // Logic to save data
        private void Save(SaveData sD)
        {
            // Specify the saving directory upon pop up. If it doesn't exist, create it!
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Schism");
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
                sD.SaveEndian,
                sD.SaveAsciiEnable,
                sD.SaveAddressConv
            });

            // Display a File Explorer window for the user to save the json file
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "userData"; // Default file name
            saveFileDialog.DefaultExt = ".sav"; // Default file extension
                                                // Filter files by extension. The format is "Description|Pattern"
            saveFileDialog.Filter = "Schism Save File (.sav)|*.sav|All files (*.*)|*.*";
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
            openFileDialog.Filter = "Schism Save File (.sav)|*.sav|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Initial Directory

            // Show open file dialog box
            bool? result = openFileDialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                var options = new JsonSerializerOptions
                {
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
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
                // return loaded data
                return lD;
            else
                // return empty data (i.e. nothing is loaded for the user)
                return new SaveData();
        }

        // Public Command properties
        public DelegateCommand Save_Click =>
            _saveClick ??= new DelegateCommand(Execute_save_Click);

        void Execute_save_Click()
        {
            // Create a SaveData object with the current state of the ViewModel
            SaveData sD = new SaveData
            {
                SaveDeviceId = MS.DeviceId,
                SaveStartAddress = MS.StartAddress,
                SaveLength = MS.DataLength,
                SaveDataType = MS.SelectedDataType,
                SaveNumericBase = MS.SelectedNumericBase,
                SaveDataSize = MS.SelectedDataSize,
                SaveEndian = MS.SelectedEndian,
                SaveAsciiEnable = MS.AsciiEnable,

                SaveAddressConv = _selectedAddressConvention
            };
            Save(sD);
        }

        public DelegateCommand Load_Click =>
            _loadClick ??= new DelegateCommand(Execute_Load_Click);

        void Execute_Load_Click()
        {
            SaveData lD = Load();

            // Update ViewModel properties with loaded data
            // NOTE: Setting the public instances of variables runs the logic in the setters implicitly! ;)
            MS.DeviceId = lD.SaveDeviceId;
            MS.DataLength = lD.SaveLength;
            MS.StartAddress = lD.SaveStartAddress;
            MS.SelectedDataType = lD.SaveDataType;
            MS.SelectedNumericBase = lD.SaveNumericBase;
            MS.SelectedDataSize = lD.SaveDataSize;
            MS.SelectedEndian = lD.SaveEndian;
            MS.AsciiEnable = lD.SaveAsciiEnable;

            // ViewModel specific save parameter
            _selectedAddressConvention = lD.SaveAddressConv;

            OnPropertyChanged(nameof(SelectedAddressConvention));

            // Since you're updating these parameters in the Service, your subscription from the constructor will catch this and update the UI automatically.
            // Load --> Update Service --> Subscription pings --> Table is rebuilt
        }

        public DelegateCommand Exit_Click =>
            _exitClick ??= new DelegateCommand(Execute_Exit_Click);

        void Execute_Exit_Click()
        {
            // Close the app!
            Application.Current.Shutdown();
        }

        public DelegateCommand Conn_Click =>
            _connClick ??= new DelegateCommand(Execute_Conn_Click);

        void Execute_Conn_Click()
        {
            // Looks a bit strange, but effectively works as a toggle! Press it once to connect (false case), press it again to stop (true case).
            if (MS.ConnectEngage)
                MS.ConnectEngage = false;
            else
                MS.Connection();
        }

        public DelegateCommand Sett_Click =>
            _settClick ??= new DelegateCommand(Execute_Sett_Click);

        void Execute_Sett_Click()
        {
            // Create the Connection Settings window
            Window connSettings = new Window
            {
                // Open the window
                Content = new ConnSettings(),
                SizeToContent = SizeToContent.WidthAndHeight,
                Topmost = true
            };
            connSettings.ShowDialog();
        }

        public DelegateCommand Themes_Click =>
            _themesClick ??= new DelegateCommand(Execute_Themes_Click);

        void Execute_Themes_Click()
        {
            // Create the About window
            Window themes = new Window
            {
                // Open the window
                Content = new Themes(),
                SizeToContent = SizeToContent.WidthAndHeight,
                Topmost = true
            };
            themes.ShowDialog();
        }

        public DelegateCommand About_Click =>
            _aboutClick ??= new DelegateCommand(Execute_About_Click);

        void Execute_About_Click()
        {
            // Create the About window
            Window about = new Window
            {
                // Open the window
                Content = new About(),
                SizeToContent = SizeToContent.WidthAndHeight,
                Topmost = true
            };
            about.ShowDialog();
        }
    }
}
