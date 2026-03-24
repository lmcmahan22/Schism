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
using System.Windows.Media;

namespace Schism.ViewModels
{
    public class HomeViewModel : BindableBase, INotifyPropertyChanged
    {

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        // View Model properties (should these be static???)
        private string _title = "Schism Home Screen";
        private Visibility[] _colsVis = { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };
        private Visibility _aDisplayTypeDropDown = Visibility.Hidden;
        private string[] _addressList = { "0", "20", "40", "60", "80", "100" };
        private static ObservableCollection<string> _addressConvention = new ObservableCollection<string> { "Register Address (starting from 0)", "Register Number (starting from 1)" };
        private string _selectedAddressConvention = _addressConvention.First();
        private bool _nonBoolData = false;

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
        private readonly ThemeService _TS = ThemeService.Instance; // ThemeService is a singleton, so we access the instance directly
        private readonly MODBUSService _MS = MODBUSService.Instance; // MODBUSService is a singleton, so we access the instance directly

        // Public instances of the Model for control in the View
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

        public Visibility ADisplayTypeDropDown
        {
            get { return _aDisplayTypeDropDown; }
            set { SetProperty(ref _aDisplayTypeDropDown, value); }
        }

        public bool NonBoolData
        {
            get => _nonBoolData;
            set => SetProperty(ref _nonBoolData, value);
        }

        public ObservableCollection<string> AddressConvention{ get => _addressConvention; }

        public string SelectedAddressConvention
        {
            get => _selectedAddressConvention;
            set => SetProperty(ref _selectedAddressConvention, value);
        }

        // Grid elements
        public ObservableCollection<StringWrapper> ShiftColumn { get => _shiftColumn; }
        public ObservableCollection<StringWrapper>[] Names { get => _names; }
        public ObservableCollection<StringWrapper>[] ModbusGrid { get => _modbusGrid; }

        // ModbusService Model instances
        public int NumPolls
        {
            get => _MS.NumPolls;
            set => _MS.NumPolls = value;
        }

        public int NumOK
        {
            get => _MS.NumOK;
            set => _MS.NumOK = value;
        }

        public int NumErrors
        {
            get => _MS.NumErrors;
            set => _MS.NumErrors = value;
        }

        public int NumTX
        {
            get => _MS.NumTX;
            set => _MS.NumTX = value;
        }

        public int NumRX
        {
            get => _MS.NumRX;
            set => _MS.NumRX = value;
        }

        public int NumRequests
        {
            get => _MS.NumRequests;
            set => _MS.NumRequests = value;
        }

        public int NumResponses
        {
            get => _MS.NumResponses;
            set => _MS.NumResponses = value;
        }

        public byte DeviceID
        {
            get => _MS.DeviceID;
            set => _MS.DeviceID = value;
        }

        public ushort Length
        {
            get => _MS.Length;
            set => _MS.Length = value;
        }

        public ushort StartAddress
        {
            get => _MS.StartAddress;
            set => _MS.StartAddress = value;
        }

        public bool AsciiEnable
        {
            get => _MS.AsciiEnable;
            set => _MS.AsciiEnable = value;
        }

        public bool IsConnected
        {
            get => _MS.IsConnected;
            set => _MS.IsConnected = value;
        }

        private ObservableCollection<StringWrapper> _modbusData { get => _MS.ModbusData; }

        public ObservableCollection<string> DataType { get => _MS.DataType; }
        public ObservableCollection<string> NumericBase { get => _MS.NumericBase; }
        public ObservableCollection<string> Endian { get => _MS.Endian; }
        public ObservableCollection<string> ADisplayType { get => _MS.ADisplayType; }

        public string SelectedDataType
        {
            get => _MS.SelectedDataType;
            set => _MS.SelectedDataType = value;
        }

        public string SelectedNumericBase
        {
            get => _MS.SelectedNumericBase;
            set => _MS.SelectedNumericBase = value;
        }

        public string SelectedEndian
        {
            get => _MS.SelectedEndian;
            set => _MS.SelectedEndian = value;
        }

        public string SelectedADisplayType
        {
            get => _MS.SelectedADisplayType;
            set => _MS.SelectedADisplayType = value;
        }

        // ThemeService Model instances
        public Brush MainColor { get => _TS.Main; }
        public Brush AccentOneColor { get => _TS.Accent1; }
        public Brush AccentTwoColor { get => _TS.Accent2; }
        public Brush AccentThreeColor { get => _TS.Accent3; }
        public Brush TextColor { get => _TS.Text; }

        // ViewModel constructor
        public HomeViewModel(IDialogService dialogService)
        {
            _TS.PropertyChanged += Themes_PropertyChanged; // Subscribe to the PropertyChanged event of the ThemeService singleton to react to theme changes

            _MS.PropertyChanged += MODBUS_PropertyChanged; // Subscribe to the PropertyChanged event of the ThemeService singleton to react to connection status change
        }

        // Model Singleton Subscriptions!
        // This is how our data gets carried from the Model through to the View
        private void Themes_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_TS.Main))
            {
                OnPropertyChanged(nameof(MainColor));
            }
            if (e.PropertyName == nameof(_TS.Accent1))
            {
                OnPropertyChanged(nameof(AccentOneColor));
            }
            if (e.PropertyName == nameof(_TS.Accent2))
            {
                OnPropertyChanged(nameof(AccentTwoColor));
            }
            if (e.PropertyName == nameof(_TS.Accent3))
            {
                OnPropertyChanged(nameof(AccentThreeColor));
            }
            if (e.PropertyName == nameof(_TS.Text))
            {
                OnPropertyChanged(nameof(TextColor));
            }
        }

        private void MODBUS_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_MS.IsConnected))
            {
                OnPropertyChanged(nameof(IsConnected));
            }

            // Some of these may have custom updating logic.
            // This is where we need to act on the Model's updates, so it makes sense to put this functionality here.
            if(e.PropertyName == nameof(_MS.StartAddress))
            {
                // update the textual address representation
                for (int i = 0; i < _addressList.Length; i++)
                {
                    _addressList[i] = (_MS.StartAddress + (i * 20)).ToString();
                }

                // Notify the UI that the AddressList contents changed
                OnPropertyChanged(nameof(_addressList));
            }

            if(e.PropertyName == nameof(_MS.AsciiEnable){
                SetProperty(ref _aDisplayTypeDropDown, _MS.AsciiEnable ? Visibility.Visible : Visibility.Hidden);
                OnPropertyChanged(nameof(ADisplayTypeDropDown));
            }

            if(e.PropertyName == nameof(_MS.SelectedADisplayType)){
                // set NonBoolData to true in order to disable UI elements when we don't need them!
                _nonBoolData = _MS.SelectedDataType is "Holding Registers" or "Input Registers";
            }

            // Rebuild the table in here?
            BuildModbusTable();
        }

        // Private methods for concise functionality
        private void UpdateColsVisAndNotify()
        {
            // Determine which columns should be visible, based on the provided Length from the Model!
            for (int i = 0; i < ColsVis.Length; i++)
                _colsVis[i] = Length > (i * 20) ? Visibility.Visible : Visibility.Collapsed;

            // Notify the UI that the ColsVis contents changed
            OnPropertyChanged(nameof(ColsVis));
        }

        // Build the table for the main UI based on the provided MODBUS Data from the MODBUSService Model
        private void BuildModbusTable()
        {
            // Defensive: if collection is null (shouldn't be), create it
            if (_shiftColumn == null)
                _shiftColumn = new ObservableCollection<StringWrapper>();

            _shiftColumn.Clear();

            // Generate header shifts (always 20 rows with 1 header cell)
            for (int i = 0; i < 20; i++)
            {
                // This will print each index as i if counting from 0, or i+1 if counting from 1
                string content = $"+{(SelectedAddressConvention == "Register Address (starting from 0)" ? i : i + 1)}";
                ShiftColumn.Add(new StringWrapper(content));
            }

            string[] namesCache = new string[Length];
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
                        if (((i * 20) + j) < Length)
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

            // Loop through each column of the current ModbusData collection.
            // If any are null (somehow), create the collection there
            for (int i = 0; i < _modbusGrid.Length; i++)
            {
                if (_modbusGrid[i] == null)
                    _modbusGrid[i] = new ObservableCollection<StringWrapper>();

                _modbusGrid[i].Clear();
            }

            // Add rows for the configured length
            var reqCols = ((_MS.Length - 1) / 20) + 1; // Calculate how many columns we need based on the length (integer division rounding up)
            for (int i = 0; i < reqCols; i++)
            {
                var reqRows = Math.Min((_MS.Length - 20 * i), 20); // Calculate how many rows we need in the last column (or 20 if length is greater than 20)
                for (int j = 0; j < reqRows; j++)
                {
                    string name = namesCache[(i * 20) + j];
                    StringWrapper data = _modbusData.ElementAt((i * 20) + j);
                    _names[i].Add(new StringWrapper(name));
                    _modbusGrid[i].Add(data);
                }
            }
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
                sD.SaveDeviceID,
                sD.SaveStartAddress,
                sD.SaveLength,
                sD.SaveDataType,
                sD.SaveNumericBase,
                sD.SaveEndian,
                sD.SaveASCIIEnable,
                sD.SaveADisplayType,
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
                SaveDeviceID = DeviceID,
                SaveStartAddress = StartAddress,
                SaveLength = Length,
                SaveDataType = SelectedDataType,
                SaveNumericBase = SelectedNumericBase,
                SaveEndian = SelectedEndian,
                SaveASCIIEnable = AsciiEnable,
                SaveADisplayType = SelectedADisplayType,
                SaveAddressConv = SelectedAddressConvention
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
            DeviceID = lD.SaveDeviceID;
            Length = lD.SaveLength;
            StartAddress = lD.SaveStartAddress;
            SelectedDataType = lD.SaveDataType;
            SelectedNumericBase = lD.SaveNumericBase;
            SelectedEndian = lD.SaveEndian;
            AsciiEnable = lD.SaveASCIIEnable;
            ADisplayTypeDropDown = lD.SaveASCIIEnable ? Visibility.Visible : Visibility.Hidden; // Ensure the ADisplayType dropdown visibility is consistent with the loaded ASCIIEnable value
            SelectedADisplayType = lD.SaveADisplayType;
            SelectedAddressConvention = lD.SaveAddressConv;

            // Update UI as needed
            UpdateColsVisAndNotify();
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
            // TODO: Implement connection logic
            if(IsConnected == false)
            {
                _MS.Connection();
            }
            else
            {
                // setting this to false will trigger the disconnect on the parallel thread's while loop!
                IsConnected = false;
            }
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

        public DelegateCommand InsErr_Click =>
            _insErrClick ??= new DelegateCommand(Execute_InsErr_Click);

        void Execute_InsErr_Click()
        {
            // TODO: Implement error injection dialog
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
