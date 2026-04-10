using Microsoft.Win32;
using Schism.Models;
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

        // View Model properties
        private string _title = "Schism Home Screen";
        private Visibility[] _colsVis = { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };
        private Visibility _aDisplayTypeDropdown = Visibility.Hidden;
        private string[] _addressList = { "0", "20", "40", "60", "80", "100" };
        private static ObservableCollection<string> _addressConventions = ["Register Address (starting from 0)", "Register Number (starting from 1)"];
        private string _selectedAddressConvention = _addressConventions.First();
        private bool _nonBoolData = false;
        private bool _endianEnable = false;
        private bool _hexData = false;
        private Visibility _errorContents = Visibility.Collapsed;

        // View Model grid elements
        private ObservableCollection<StringWrapper> _shiftColumn = new ObservableCollection<StringWrapper>();
        private ObservableCollection<StringWrapper>[] _names = new ObservableCollection<StringWrapper>[6];
        private ObservableCollection<StringWrapper>[] _modbusGrid = new ObservableCollection<StringWrapper>[6];

        // Commands
        private DelegateCommand? _saveClick;
        private DelegateCommand? _loadClick;
        private DelegateCommand? _exitClick;
        private DelegateCommand? _connClick;
        private DelegateCommand? _settClick;
        private DelegateCommand? _insErrClick;
        private DelegateCommand? _themesClick;
        private DelegateCommand? _aboutClick;

        // Service Singletons (see App.xml)
        public ThemeService TS => ThemeService.Instance;
        public MODBUSService MS => MODBUSService.Instance;

        // Public instances of the ViewModel for control in the View
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string[] AddressList
        {
            get => _addressList;
            set => SetProperty(ref _addressList, value);
        }

        public Visibility[] ColsVis
        {
            get => _colsVis;
            set => SetProperty(ref _colsVis, value);
        }

        public Visibility ADisplayTypeDropdown
        {
            get => _aDisplayTypeDropdown;
            set => SetProperty(ref _aDisplayTypeDropdown, value);
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

        public ObservableCollection<string> AddressConventions{ get => _addressConventions; }

        public string SelectedAddressConvention
        {
            get => _selectedAddressConvention;
            set
            {
                SetProperty(ref _selectedAddressConvention, value);
                UpdateModbusTable(); // Ensure the table updates immediately when the address convention is changed, since this changes the content of the shift column!
            }
        }

        // View Model Visibility element bases from Model boolean! :D
        public Visibility ErrorContents
        {
            get
            {
                // Use IsNullOrEmpty for safety (handles null and empty)
                return string.IsNullOrEmpty(MS.ErrMess) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // Grid collections
        public ObservableCollection<StringWrapper> ShiftColumn => _shiftColumn;
        public ObservableCollection<StringWrapper>[] Names => _names;
        public ObservableCollection<StringWrapper>[] ModbusGrid => _modbusGrid;

        // ViewModel constructor
        public HomeViewModel(IDialogService dialogService)
        {
            MS.PropertyChanged += MS_PropertyChanged;
            UpdateModbusTable(); // Build the initial table based on default parameters in the Model
        }

        private void MS_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Make it so this updates with respect to relevant parameters in the Model
            if (e.PropertyName is nameof(MS.DataLength) or
                nameof(MS.DeviceId) or
                nameof(MS.SelectedEndian) or
                nameof(MS.SelectedAsciiDisplayType))
                UpdateModbusTable();

            if (e.PropertyName is nameof(MS.SelectedNumericBase) or nameof(MS.DataLength) or nameof(MS.SelectedDataSize))
            {

                // If we're attempting to poll an odd number of registers while in a numeric base that requires an even number of registers, reduce the length until it is a multiple of 2 to ensure we don't exceed the length with our data display.
                if ((MS.SelectedDataType is "Holding Registers" or "Input Registers") && (MS.SelectedDataSize == "32-Bit") && (MS.DataLength % 2 != 0))
                    MS.DataLength = (ushort)(MS.DataLength - (MS.DataLength % 2));

                // If we're attempting to poll a number of registers that isn't a multiple of 4 while in a numeric base that requires a multiple of 4, reduce the length until it is a multiple of 4 to ensure we don't exceed the length with our data display.
                else if ((MS.SelectedDataType is "Holding Registers" or "Input Registers") && (MS.SelectedDataSize == "64-Bit") && (MS.DataLength % 4 != 0))
                    MS.DataLength = (ushort)(MS.DataLength - (MS.DataLength % 4));

                if(MS.SelectedNumericBase is "Floating Point")
                {
                    // Force data size update if it is currently set to 16-Bit while attempting to use Floating Point as the Numeric Base
                    if(MS.SelectedDataSize == "16-Bit")
                        MS.SelectedDataSize = "32-Bit";

                    // Update the available data sizes for Floating Point
                    MS.DataSizes = new ObservableCollection<string>{"32-Bit", "64-Bit" };
                }
                else
                    // Update the available data sizes for non-Floating Point numeric bases
                    MS.DataSizes = new ObservableCollection<string> { "16-Bit", "32-Bit", "64-Bit" };

                if(MS.SelectedDataSize is "16-Bit")
                {
                    if(MS.SelectedNumericBase is "Floating Point")
                        // Force numeric base update if it is currently set to Floating Point while attempting to use 16-Bit as the Data Size
                        MS.SelectedNumericBase = "Decimal";

                    MS.NumericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary"};
                }
                else
                    // Update the available numeric bases for non-16-Bit data sizes
                    MS.NumericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary", "Floating Point" };

                _hexData = (MS.SelectedDataType is "Holding Registers" or "Input Registers") && (MS.SelectedNumericBase is "Hexadecimal");
                _endianEnable = (MS.SelectedDataType is "Holding Registers" or "Input Registers");
                OnPropertyChanged(nameof(HexData));
                OnPropertyChanged(nameof(EndianEnable));

                UpdateModbusTable();
            }

            if (e.PropertyName is nameof(MS.SelectedDataType))
            {
                _nonBoolData = MS.SelectedDataType is "Holding Registers" or "Input Registers";
                _hexData = (MS.SelectedDataType is "Holding Registers" or "Input Registers") && (MS.SelectedNumericBase is "Hexadecimal");
                _endianEnable = (MS.SelectedDataType is "Holding Registers" or "Input Registers");
                OnPropertyChanged(nameof(NonBoolData)); // Notify the UI that the NonBoolData value has been updated, so that it can show/hide the numeric base and endian dropdowns accordingly
                OnPropertyChanged(nameof(EndianEnable)); // Notify the UI that the EndianEnable value has been updated, so that it can show/hide the endian dropdown accordingly
                OnPropertyChanged(nameof(HexData)); // Notify the UI that the HexData value has been updated
                UpdateModbusTable();
            }

            if (e.PropertyName is nameof(MS.AsciiEnable))
            {
                _aDisplayTypeDropdown = MS.AsciiEnable ? Visibility.Visible : Visibility.Hidden; // Ensure the ADisplayType dropdown visibility is consistent with the ASCIIEnable value
                OnPropertyChanged(nameof(ADisplayTypeDropdown)); // Notify the UI that the ADisplayType dropdown visibility has been updated)
                UpdateModbusTable();
            }

            if (e.PropertyName is nameof(MS.StartAddress))
            {
                for(int i = 0; i < _addressList.Length; i++)
                {
                    int addr = MS.StartAddress + (i * 20);
                    _addressList[i] = addr.ToString();
                }
                OnPropertyChanged(nameof(AddressList)); // Notify the UI that the address list has been updated
                UpdateModbusTable();
            }

            if (e.PropertyName is nameof(MS.ModbusData)){
                UpdateModbusData(); // Only update the MODBUS data if we see an update on the data from the Model!
            }

            if (e.PropertyName is nameof(MS.ErrMess))
            {
                OnPropertyChanged(nameof(ErrorContents));
                // No MODBUS table update needed here.
            }
        }

        //private void TS_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        //{
        //    // Your View has public variables that it references to grab the right colors. Instead, refer to the TS.X instances. You can probably remove the subscription since you don't need the ViewModel to act on these changes.
        //}

        // Build the table for the main UI based on the provided MODBUS Data from the MODBUSService Model
        private void UpdateModbusTable()
        {

            MS.IsConnected = false; // Force disconnect if we're currently connected, since we're changing parameters that would affect the data display.

            // Determine which columns should be visible, based on the provided DataLength from the Model!
            for (int i = 0; i < _colsVis.Length; i++)
            {
                _colsVis[i] = MS.DataLength > (i * 20) ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged(nameof(ColsVis));
            }

            // Update shift column contents, in case the SelectedAddressConvention was updated.
            // Make it so this only updates if SelectedAddressConvention has been changed since the last call of this method!
            _shiftColumn.Clear();
            for (int i = 0; i < 20; i++)
            {
                // This will print each index as i if counting from 0, or i+1 if counting from 1
                string content = $"+{(SelectedAddressConvention == "Register Address (starting from 0)" ? i : i + 1)}";
                _shiftColumn.Add(new StringWrapper(content));
            }

            // Prepare the name data, in case we need to keep it around for the next table build
            string[] namesCache = new string[MS.DataLength];
            for (int i = 0; i < _names.Length; i++)
            {
                if (_names[i] == null)
                    _names[i] = new ObservableCollection<StringWrapper>();
                else
                {
                    for(int j = 0; j < _names[i].Count; j++)
                    {
                        // Only save this name for the new display if we know that we'll see it in the new length.
                        // Otherwise, we'll risk exceeding the size of the length cache with a name we won't even need.
                        if (((i * 20) + j) < MS.DataLength)
                        {
                            string? temp = _names[i][j].Value;
                            if (temp == null)
                                namesCache[(i * 20) + j] = "";
                            else
                                namesCache[(i * 20) + j] = temp;
                        }
                    }
                }
                _names[i].Clear();
            }

            // Loop through each column of the current ModbusData collection and clear them.
            // If any columns are null (somehow), create the collection there
            for (int i = 0; i < _modbusGrid.Length; i++)
            {
                if (_modbusGrid[i] == null)
                    _modbusGrid[i] = new ObservableCollection<StringWrapper>();

                _modbusGrid[i].Clear();
            }

            // Add rows for the configured length
            var reqCols = ((MS.DataLength - 1) / 20) + 1; // Calculate how many columns we need based on the length (integer division rounding up)
            for (int i = 0; i < reqCols; i++)
            {
                var reqRows = Math.Min((MS.DataLength - 20 * i), 20); // Calculate how many rows we need in the last column (or 20 if length is greater than 20)
                for (int j = 0; j < reqRows; j++)
                {
                    int idx = (i * 20) + j;
                    StringWrapper name = new StringWrapper(namesCache[idx]);

                    // MODBUS Data does not get pulled at this time!
                    StringWrapper data = new StringWrapper("");

                    _names[i].Add(name);
                    _modbusGrid[i].Add(data);
                }
            }

            // Notify the UI of element updates
            OnPropertyChanged(nameof(ColsVis));
            OnPropertyChanged(nameof(ShiftColumn));
            OnPropertyChanged(nameof(Names)); // Might not be needed, since this is an ObservableCollection...
            OnPropertyChanged(nameof(ModbusGrid)); // Might not be needed, since this is an ObservableCollection...
        }

        private void UpdateModbusData()
        {
            // Update columns for the configured length
            var numCols = _modbusGrid.Length;
            for (int i = 0; i < numCols; i++)
            {
                var numRows = _modbusGrid[i].Count; // Get the number of rows currently displayed in this column
                for (int j = 0; j < numRows; j++)
                {
                    int idx = (i * 20) + j;

                    // Only try to pull MODBUS data if we have a connection!
                    StringWrapper data;
                    if (MS.IsConnected)
                        // Retrieve existing item if present; otherwise create one instance
                        data = MS.ModbusData[idx] ?? new StringWrapper("");
                    else
                        data = new StringWrapper("");

                    _modbusGrid[i][j] = data;
                }
            }

            // Notify the UI of element updates
            OnPropertyChanged(nameof(ModbusGrid)); // Might not be needed, since this is an ObservableCollection...
        }


        private void Save(SaveData sD)
        {
            // Logic to save data
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Schism");
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            string json = JsonSerializer.Serialize(new
            {
                sD.SaveDeviceId,
                sD.SaveStartAddress,
                sD.SaveLength,
                sD.SaveDataType,
                sD.SaveNumericBase,
                sD.SaveEndian,
                sD.SaveAsciiEnable,
                sD.SaveAsciiDisplayType,
                sD.SaveAddressConv
            });

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

        private SaveData Load()
        {

            SaveData? lD = new SaveData();

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
                return lD;
            else
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
                SaveAsciiDisplayType = MS.SelectedAsciiDisplayType,

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
            MS.SelectedAsciiDisplayType = lD.SaveAsciiDisplayType;

            // ViewModel specific save parameter
            _selectedAddressConvention = lD.SaveAddressConv;

            _aDisplayTypeDropdown = lD.SaveAsciiEnable ? Visibility.Visible : Visibility.Hidden; // Ensure the ADisplayType dropdown visibility is consistent with the loaded ASCIIEnable value

            OnPropertyChanged(nameof(SelectedAddressConvention));
            OnPropertyChanged(nameof(ADisplayTypeDropdown));

            // Since you're updating these parameters in the Model, your subscription from the constructor will catch this!
            // Load --> Update Model --> Subscription pings --> Table is rebuilt
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
            // Looks a bit strange, but effectively works as a toggle! Press it once to connect, press it again to stop.
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

        //public DelegateCommand InsErr_Click =>
        //    _insErrClick ??= new DelegateCommand(Execute_InsErr_Click);

        //void Execute_InsErr_Click()
        //{
        //    // TODO: Implement error injection dialog
        //}

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
