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
    public class HomeViewModel : BindableBase
    {

        // The helper method to raise the event
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);
        }

        // View Model properties
        private string _title = "Schism Home Screen";
        private Visibility[] _colsVis = new Visibility[6] { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };
        private Visibility _aDisplayTypeDropDown = Visibility.Hidden;
        private string[] _addressList = new string[6] { "0", "20", "40", "60", "80", "100" };
        private ObservableCollection<StringWrapper> _shiftColumn = new ObservableCollection<StringWrapper>();
        private ObservableCollection<StringWrapper>[] _names = new ObservableCollection<StringWrapper>[6];

        // Commands
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
        private readonly ThemeService _TS = ThemeService.Instance; // ThemeService is a singleton, so we access the instance directly
        private readonly MODBUSService _MS = MODBUSService.Instance; // MODBUSService is a singleton, so we access the instance directly

        // Public instances of the Model for control in the View
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public int NumPolls
        {
            get { return _MS.NumPolls; }
            set
            {
                if (_MS.NumPolls != value)
                {
                    _MS.NumPolls = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumOK
        {
            get { return _MS.NumOK; }
            set
            {
                if (_MS.NumOK != value)
                {
                    _MS.NumOK = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumErrors
        {
            get { return _MS.NumErrors; }
            set
            {
                if (_MS.NumErrors != value)
                {
                    _MS.NumErrors = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumTX
        {
            get { return _MS.NumTX; }
            set
            {
                if (_MS.NumTX != value)
                {
                    _MS.NumTX = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumRX
        {
            get { return _MS.NumRX; }
            set
            {
                if (_MS.NumRX != value)
                {
                    _MS.NumRX = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumRequests
        {
            get { return _MS.NumRequests; }
            set
            {
                if (_MS.NumRequests != value)
                {
                    _MS.NumRequests = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumResponses
        {
            get { return _MS.NumResponses; }
            set
            {
                if (_MS.NumResponses != value)
                {
                    _MS.NumResponses = value;
                    OnPropertyChanged();
                }
            }
        }

        public byte DeviceID
        {
            get { return _MS.DeviceID; }
            set
            {
                // Min and Max boundaries on Device ID, according to MODBUS documentation
                byte clamped = Math.Clamp(value, (byte)1, (byte)247);
                if (_MS.DeviceID != clamped)
                {
                    _MS.DeviceID = clamped;
                    OnPropertyChanged();
                }
            }
        }

        public ushort Length
        {
            get { return _MS.Length; }
            set
            {
                // Min and Max boundaries on Value relative to current StartAddress
                ushort maxLen = GetMaxLengthForStartAddress();
                ushort clampedLength = Math.Clamp(value, (ushort)1, maxLen);

                if (_MS.Length != clampedLength)
                {
                    _MS.Length = clampedLength;
                    // Rebuild the collection to reflect new length (uses Length getter now)
                    BuildModbusData();
                    // Update column visibility and notify UI
                    UpdateColsVisAndNotify();

                    OnPropertyChanged();
                }
            }
        }

        public ushort StartAddress
        {
            get { return _MS.StartAddress; }
            set
            {
                // Min and Max boundaries on Starting Address, according to MODBUS documentation
                ushort clampedStart = Math.Clamp(value, (ushort)0, (ushort)65535);

                if (_MS.StartAddress != clampedStart)
                {
                    _MS.StartAddress = clampedStart;
                    // When start address changes, ensure the current length does not exceed the new allowable range.
                    ushort maxLen = GetMaxLengthForStartAddress();
                    ushort clampedLength = Math.Clamp(_MS.Length, (ushort)1, maxLen);

                    if (_MS.Length != clampedLength)
                    {
                        _MS.Length = clampedLength;
                        // Notify the UI that the AddressList contents changed
                        OnPropertyChanged(nameof(Length));

                        // Rebuild the collection to reflect new length
                        BuildModbusData();

                        // Update column visibility based on the (possibly changed) length
                        UpdateColsVisAndNotify();
                    }

                    // update the textual address representation
                    for (int i = 0; i < AddressList.Length; i++)
                    {
                        AddressList[i] = (_MS.StartAddress + (i * 20)).ToString();
                    }

                    // Notify the UI that the AddressList contents changed
                    OnPropertyChanged(nameof(AddressList));

                    // Notify StartAddress changed (caller/member name handled by OnPropertyChanged call above in SetProperty,
                    // but keeping parity with original behavior)
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
            get { return _MS.AsciiEnable; }
            set
            {
                if (_MS.AsciiEnable != value)
                {
                    _MS.AsciiEnable = value;
                    SetProperty(ref _aDisplayTypeDropDown, _MS.AsciiEnable ? Visibility.Visible : Visibility.Hidden);
                    OnPropertyChanged(nameof(ADisplayTypeDropDown));
                }
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get { return _MS.IsConnected; }
            set
            {
                if (_MS.IsConnected != value)
                {
                    _MS.IsConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<StringWrapper> ShiftColumn
        {
            get { return _shiftColumn; }
            set { SetProperty(ref _shiftColumn, value); }
        }

        public ObservableCollection<StringWrapper>[] Names
        {
            get { return _names; }
            set { SetProperty(ref _names, value); }
        }

        public ObservableCollection<StringWrapper>[] Results
        {
            get { return _MS.Results; }
            set
            {
                if (_MS.Results != value)
                {
                    _MS.Results = value;
                    OnPropertyChanged(nameof(Results));
                }
            }
        }

        public ObservableCollection<string> DataType
        {
            get { return _MS.DataType; }
        }

        public ObservableCollection<string> NumericBase
        {
            get { return _MS.NumericBase; }
        }

        public ObservableCollection<string> Endian
        {
            get { return _MS.Endian; }
        }

        public ObservableCollection<string> ADisplayType
        {
            get { return _MS.ADisplayType; }
        }

        public string SelectedDataType
        {
            get { return _MS.SelectedDataType; }
            set
            {
                if (_MS.SelectedDataType != value)
                {
                    _MS.SelectedDataType = value; OnPropertyChanged();
                }
            }
        }

        public string SelectedNumericBase
        {
            get { return _MS.SelectedNumericBase; }
            set
            {
                if (_MS.SelectedNumericBase != value)
                {
                    _MS.SelectedNumericBase = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedEndian
        {
            get { return _MS.SelectedEndian; }
            set
            {
                if (_MS.SelectedEndian != value)
                {
                    _MS.SelectedEndian = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedADisplayType
        {
            get { return _MS.SelectedADisplayType; }
            set
            {
                if (_MS.SelectedADisplayType != value)
                {
                    _MS.SelectedADisplayType = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility ADisplayTypeDropDown
        {
            get { return _aDisplayTypeDropDown; }
            set { SetProperty(ref _aDisplayTypeDropDown, value); }
        }

        public Brush MainColor
        {
            get { return _TS.Main; }
        }
        public Brush AccentOneColor
        {
            get { return _TS.Accent1; }
        }

        public Brush AccentTwoColor
        {
            get { return _TS.Accent2; }
        }

        public Brush AccentThreeColor
        {
            get { return _TS.Accent3; }
        }

        public Brush TextColor
        {
            get { return _TS.Text; }
        }

        // ViewModel constructor
        public HomeViewModel(IDialogService dialogService)
        {
            _TS.PropertyChanged += Themes_PropertyChanged; // Subscribe to the PropertyChanged event of the ThemeService singleton to react to theme changes

            // Ensure collection is populated with a header + Length rows
            BuildModbusData();
        }

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

        private void UpdateColsVisAndNotify()
        {
            for (int i = 0; i < ColsVis.Length; i++)
            {
                _colsVis[i] = Length > (i * 20) ? Visibility.Visible : Visibility.Collapsed;
            }

            // Notify the UI that the ColsVis contents changed
            OnPropertyChanged(nameof(ColsVis));
        }

        private ushort GetMaxLengthForStartAddress()
        {
            ushort cap = (ushort)(65535 - _MS.StartAddress); // inclusive cap
            return Math.Min((ushort)120, cap);
        }

        // Rebuilds the observable collection items so the UI sees the expected rows
        private void BuildModbusData()
        {
            // Defensive: if collection is null (shouldn't be), create it
            if (ShiftColumn == null)
            {
                ShiftColumn = new ObservableCollection<StringWrapper>();
            }

            ShiftColumn.Clear();

            string[] namesCache = new string[Length];
            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i] == null)
                {
                    Names[i] = new ObservableCollection<StringWrapper>();
                }
                else
                {
                    for(int j = 0; j < Names[i].Count; j++)
                    {
                        // Only save this name for the new display if we know that we'll see it in the new length.
                        // Otherwise, we'll risk exceeding the size of the length cache with a name we won't even need.
                        if (((i * 20) + j) < Length)
                        {
                            string? temp = Names[i][j].Value;
                            if (temp == null)
                            {
                                namesCache[(i * 20) + j] = "";
                            }
                            else
                            {
                                namesCache[(i * 20) + j] = temp;
                            }
                        }
                    }
                }
                Names[i].Clear();
            }

            for (int i = 0; i < Results.Length; i++)
            {
                if (Results[i] == null)
                {
                    Results[i] = new ObservableCollection<StringWrapper>();
                }
                Results[i].Clear();
            }

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
                    string name = namesCache[(i * 20) + j];
                    string data = "";
                    Names[i].Add(new StringWrapper(name));
                    Results[i].Add(new StringWrapper(data));
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
                sD.SaveADisplayType
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
                SaveDeviceID = this.DeviceID,
                SaveStartAddress = this.StartAddress,
                SaveLength = this.Length,
                SaveDataType = this.SelectedDataType,
                SaveNumericBase = this.SelectedNumericBase,
                SaveEndian = this.SelectedEndian,
                SaveASCIIEnable = this.ASCIIEnable,
                SaveADisplayType = this.SelectedADisplayType
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
            _MS.Connection();
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
