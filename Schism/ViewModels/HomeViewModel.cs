using Prism.Ioc;
using Prism.Navigation.Regions;
using Schism.Models;
using Schism.Views;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Schism.ViewModels
{
    public class HomeViewModel : BindableBase
    {

        // The helper method to raise the event
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);
        }

        // View Model properties
        private string _title = "Schism Home Screen";
        private int _numPolls = 0;
        private int _numOK = 0;
        private int _numErrors = 0;
        private int _numTX = 0;
        private int _numRX = 0;
        private int _numRequests = 0;
        private int _numResponses = 0;
        private int _deviceID = 1;
        private int _length = 5;
        private int _startAddress = 0; // Don't worry about leading zeros, Radzio just interprets a value without leading zeros as having leading zeroes (i.e. output coil range). Don't worry aboout "Global Data" either, since again Radzio doesn't bother.
        private bool _asciiEnable = false;
        private string[] _addressList = new string[6] { "0", "20", "40", "60", "80", "100" };
        private Visibility[] _colsVis = new Visibility[6] { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };
        private Visibility _aDisplayTypeDropDown = Visibility.Hidden;

        private ObservableCollection<StringWrapper> _shiftColumn = new StringWrapperList();
        private ObservableCollection<DataPoint>[] _MODBUSDataPoints = new DataPointList[6];

        // dropdowns
        private ObservableCollection<string> _dataType = new ObservableCollection<string> { "Coil Status", "Input Status", "Holding Registers", "Input Registers" };
        private ObservableCollection<string> _numericBase = new ObservableCollection<string> { "Integer", "Hexadecimal", "Binary", "32 Bit Float", "32 Bit SW. Float", "64 Bit Float", "64 Bit SW. Float" };
        private ObservableCollection<string> _endian = new ObservableCollection<string> { "Big Endian", "Little Endian", "Big Endian (Swap Words)", "Big Endian (Swap Bytes)" };
        private ObservableCollection<string> _aDisplayType = new ObservableCollection<string> { "1 Char/Reg", "2 Char/Reg", "2 Char/Reg SW." };

        private string _selectedDataType;
        private string _selectedNumericBase;
        private string _selectedEndian;
        private string _selectedADisplayType;

        // commands
        private DelegateCommand? _saveClick;
        private DelegateCommand? _loadClick;
        private DelegateCommand? _exitClick;
        private DelegateCommand? _connClick;
        private DelegateCommand? _discClick;
        private DelegateCommand? _settClick;
        private DelegateCommand? _insErrClick;
        private DelegateCommand? _themesClick;
        private DelegateCommand? _aboutClick;

        // Service Singletons (see App.xml)
        private readonly SaveAndLoadService SNL = new SaveAndLoadService();
        private readonly ThemeService TS = new ThemeService();
        private readonly MODBUSService MS = new MODBUSService();

        // Public property getters and setters
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public int NumPolls
        {
            get { return _numPolls; }
            set { SetProperty(ref _numPolls, value); }
        }

        public int NumOK
        {
            get { return _numOK; }
            set { SetProperty(ref _numOK, value); }
        }

        public int NumErrors
        {
            get { return _numErrors; }
            set { SetProperty(ref _numErrors, value); }
        }

        public int NumTX
        {
            get { return _numTX; }
            set { SetProperty(ref _numTX, value); }
        }

        public int NumRX
        {
            get { return _numRX; }
            set { SetProperty(ref _numRX, value); }
        }

        public int NumRequests
        {
            get { return _numRequests; }
            set { SetProperty(ref _numRequests, value); }
        }

        public int NumRepsonses
        {
            get { return _numResponses; }
            set { SetProperty(ref _numResponses, value); }
        }

        public int DeviceID
        {
            get { return _deviceID; }
            set
            {
                // Min and Max boundaries on Device ID, according to MODBUS documentation
                int clamped = Math.Clamp(value, 1, 247);

                if (SetProperty(ref _deviceID, clamped))
                {
                    OnPropertyChanged();
                }
            }
        }

        public Brush MainColor
        {
            get { return TS.Main; }
        }
        public Brush AccentOne
        {
            get { return TS.Accent; }
        }

        public Brush AccentTwo
        {
            get { return TS.Accent2; }
        }

        public Brush AccentThree
        {
            get { return TS.Accent3; }
        }

        public Brush TextColor
        {
            get { return TS.Text; }
        }

        private void UpdateColsVisAndNotify()
        {
            for (int i = 0; i < MODBUSDataPoints.Length; i++)
            {
                _colsVis[i] = _length > (i * 20) ? Visibility.Visible : Visibility.Collapsed;
            }

            // Notify the UI that the ColsVis contents changed
            OnPropertyChanged(nameof(ColsVis));
        }

        private int GetMaxLengthForStartAddress()
        {
            int cap = (65535 - _startAddress) + 1; // inclusive cap
            return Math.Min(120, cap);
        }

        public int StartAddress
        {
            get { return _startAddress; }
            set
            {
                // Min and Max boundaries on Starting Address, according to MODBUS documentation
                int clampedStart = Math.Clamp(value, 0, 65535);

                if (SetProperty(ref _startAddress, clampedStart))
                {
                    // When start address changes, ensure the current length does not exceed the new allowable range.
                    int maxLen = GetMaxLengthForStartAddress();
                    int clampedLength = Math.Clamp(_length, 1, maxLen);

                    if (SetProperty(ref _length, clampedLength))
                    {
                        // Notify the UI that the AddressList contents changed
                        OnPropertyChanged(nameof(Length));

                        // Rebuild the collection to reflect new length
                        BuildModbusDataPoints();

                        // Update column visibility based on the (possibly changed) length
                        UpdateColsVisAndNotify();
                    }

                    // update the textual address representation
                    for (int i = 0; i < AddressList.Length; i++)
                    {
                        AddressList[i] = (_startAddress + (i * 20)).ToString();
                    }

                    // Notify the UI that the AddressList contents changed
                    OnPropertyChanged(nameof(AddressList));

                    // Notify StartAddress changed (caller/member name handled by OnPropertyChanged call above in SetProperty,
                    // but keeping parity with original behavior)
                    OnPropertyChanged();
                }
            }
        }

        public int Length
        {
            get { return _length; }
            set
            {
                // Min and Max boundaries on Value relative to current StartAddress
                int maxLen = GetMaxLengthForStartAddress();
                int clamped = Math.Clamp(value, 1, maxLen);

                // Update the backing field first; only proceed if it actually changed
                if (SetProperty(ref _length, clamped))
                {
                    // Rebuild the collection to reflect new length (uses Length getter now)
                    BuildModbusDataPoints();

                    // Update column visibility and notify UI
                    UpdateColsVisAndNotify();

                    // Keep parity with original pattern
                    OnPropertyChanged();
                }
            }
        }

        public string[] AddressList
        {
            get { return _addressList; }
            set { SetProperty(ref _addressList, value); }
        }

        public Visibility[] ColsVis
        {
            get { return _colsVis; }
            set
            {
                SetProperty(ref _colsVis, value);
            }
        }

        public bool ASCIIEnable
        {
            get { return _asciiEnable; }
            set
            {
                if (SetProperty(ref _asciiEnable, value))
                {
                    SetProperty(ref _aDisplayTypeDropDown, _asciiEnable ? Visibility.Visible : Visibility.Hidden);
                    OnPropertyChanged(nameof(ADisplayTypeDropDown));
                }
                OnPropertyChanged();
            }
        }

        public ObservableCollection<StringWrapper> ShiftColumn
        {
            get { return _shiftColumn; }
            set { SetProperty(ref _shiftColumn, value); }
        }

        public ObservableCollection<DataPoint>[] MODBUSDataPoints
        {
            get { return _MODBUSDataPoints; }
            set { SetProperty(ref _MODBUSDataPoints, value); }
        }

        public ObservableCollection<string> DataType
        {
            get { return _dataType; }
            set { SetProperty(ref _dataType, value); }
        }

        public ObservableCollection<string> NumericBase
        {
            get { return _numericBase; }
            set { SetProperty(ref _numericBase, value); }
        }

        public ObservableCollection<string> Endian
        {
            get { return _endian; }
            set { SetProperty(ref _endian, value); }
        }

        public ObservableCollection<string> ADisplayType
        {
            get { return _aDisplayType; }
            set { SetProperty(ref _aDisplayType, value); }
        }

        public string SelectedDataType
        {
            get { return _selectedDataType; }
            set { SetProperty(ref _selectedDataType, value); }
        }

        public string SelectedNumericBase
        {
            get { return _selectedNumericBase; }
            set { SetProperty(ref _selectedNumericBase, value); }
        }

        public string SelectedEndian
        {
            get { return _selectedEndian; }
            set { SetProperty(ref _selectedEndian, value); }
        }

        public string SelectedADisplayType
        {
            get { return _selectedADisplayType; }
            set { SetProperty(ref _selectedADisplayType, value); }
        }

        public Visibility ADisplayTypeDropDown
        {
            get { return _aDisplayTypeDropDown; }
            set { SetProperty(ref _aDisplayTypeDropDown, value); }
        }

        private IDialogService _dialogService;

        // View Model constructor
        public HomeViewModel(IDialogService dialogService)
        {
            // Ensure collection is populated with a header + Length rows
            BuildModbusDataPoints();

            _dialogService = dialogService;
            //NavigateCommand = new DelegateCommand(OnNavigate);
        }

        // Rebuilds the observable collection items so the UI sees the expected rows
        private void BuildModbusDataPoints()
        {
            // Defensive: if collection is null (shouldn't be), create it
            if (ShiftColumn == null)
            {
                ShiftColumn = new StringWrapperList();
            }

            ShiftColumn.Clear();

            for (int i = 0; i < MODBUSDataPoints.Length; i++)
            {
                if (MODBUSDataPoints[i] == null)
                {
                    MODBUSDataPoints[i] = new DataPointList();
                }
                MODBUSDataPoints[i].Clear();
            }

            GenerateTable();
        }

        private void GenerateTable()
        {
            // Generate header shifts (always 20 rows with 1 header cell)
            for (int i = 0; i < 20; i++)
            {
                string content = "+" + (i).ToString();
                ShiftColumn.Add(new StringWrapper(content));
            }

            // Add rows for the configured length
            var reqCols = ((Length - 1) / 20) + 1; // Calculate how many columns we need based on the length (integer division rounding up)
            for (int i = 0; i < reqCols; i++)
            {
                var reqRows = Math.Min((Length - 20 * i), 20); // Calculate how many rows we need in the last column (or 20 if length is greater than 20)
                for (int j = 0; j < reqRows; j++)
                {
                    string alias = "";
                    // UPDATE THIS WITH ACTUAL MODBUS DATA!
                    string data = ((i + 1) * j * 20).ToString();
                    MODBUSDataPoints[i].Add(new DataPoint(alias, data));
                }
            }
        }

        // Public Command properties
        public DelegateCommand Save_Click =>
            _saveClick ??= new DelegateCommand(Execute_save_Click);

        void Execute_save_Click()
        {
            // Create a SaveData object with the current state of the ViewModel
            SaveData sD = new SaveData
            {
                SaveDeviceID = this.DeviceID,
                SaveStartAddress = this.StartAddress,
                SaveLength = this.Length,
                SaveDataType = this.SelectedDataType,
                SaveNumericBase = this.SelectedNumericBase,
                SaveEndian = this.SelectedEndian,
                SaveASCIIEnable = this.ASCIIEnable,
                SaveADisplayType = this.SelectedADisplayType
            };
            SNL.Save(sD);
        }

        public DelegateCommand Load_Click =>
            _loadClick ??= new DelegateCommand(Execute_Load_Click);

        void Execute_Load_Click()
        {
            SaveData lD = SNL.Load();

            // Update ViewModel properties with loaded data
            // NOTE: Setting the public instances of variables runs the logic in the setters implicitly! ;)
            this.DeviceID = lD.SaveDeviceID;
            this.Length = lD.SaveLength;
            this.StartAddress = lD.SaveStartAddress;
            this.SelectedDataType = lD.SaveDataType;
            this.SelectedNumericBase = lD.SaveNumericBase;
            this.SelectedEndian = lD.SaveEndian;
            this.ASCIIEnable = lD.SaveASCIIEnable;
            this.ADisplayTypeDropDown = lD.SaveASCIIEnable ? Visibility.Visible : Visibility.Hidden; // Ensure the ADisplayType dropdown visibility is consistent with the loaded ASCIIEnable value
            this.SelectedADisplayType = lD.SaveADisplayType;

            // Update UI as needed
            UpdateColsVisAndNotify();
        }

        public DelegateCommand Exit_Click =>
            _exitClick ??= new DelegateCommand(Execute_Exit_Click);

        void Execute_Exit_Click()
        {
            //  TODO: Implement application exit logic
        }

        public DelegateCommand Conn_Click =>
            _connClick ??= new DelegateCommand(Execute_Conn_Click);

        void Execute_Conn_Click()
        {
            // TODO: Implement connection logic
        }

        public DelegateCommand Disc_Click =>
            _discClick ??= new DelegateCommand(Execute_Disc_Click);

        void Execute_Disc_Click()
        {
            // TODO: Implement disconnect logic
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
