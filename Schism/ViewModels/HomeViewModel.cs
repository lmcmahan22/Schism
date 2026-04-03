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
        private Visibility _aDisplayTypeDropDown = Visibility.Hidden;
        private string[] _addressList = { "0", "20", "40", "60", "80", "100" };
        private static ObservableCollection<string> _addressConventions = ["Register Address (starting from 0)", "Register Number (starting from 1)"];
        private string _selectedAddressConvention = _addressConventions.First();
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
        public ThemeService TS => ThemeService.Instance; // ThemeService is a singleton, so we access the instance directly
        public MODBUSService MS => MODBUSService.Instance; // MODBUSService is a singleton, so we access the instance directly

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

        public Visibility ADisplayTypeDropDown
        {
            get => _aDisplayTypeDropDown;
            set => SetProperty(ref _aDisplayTypeDropDown, value);
        }

        public bool NonBoolData
        {
            get => _nonBoolData;
            set => SetProperty(ref _nonBoolData, value);
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
                nameof(MS.ModbusData) or 
                nameof(MS.DeviceId) or 
                nameof(MS.IsConnected) or 
                nameof(MS.SelectedNumericBase) or 
                nameof(MS.SelectedEndian) or 
                nameof(MS.SelectedAsciiDisplayType))
                    UpdateModbusTable();

            if (e.PropertyName is nameof(MS.SelectedDataType))
            {
                NonBoolData = MS.SelectedDataType is "Holding Registers" or "Input Registers";
                UpdateModbusTable();
            }

            if (e.PropertyName is nameof(MS.AsciiEnable))
            {
                ADisplayTypeDropDown = MS.AsciiEnable ? Visibility.Visible : Visibility.Hidden; // Ensure the ADisplayType dropdown visibility is consistent with the ASCIIEnable value
                UpdateModbusTable();
            }

            if (e.PropertyName is nameof(MS.StartAddress))
            {
                for(int i = 0; i < AddressList.Length; i++)
                {
                    int addr = MS.StartAddress + (i * 20);
                    AddressList[i] = addr.ToString();
                }
                OnPropertyChanged(nameof(AddressList)); // Notify the UI that the address list has been updated
                UpdateModbusTable();
            }
        }

        //private void TS_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        //{
        //    // Your View has public variables that it references to grab the right colors. Instead, refer to the TS.X instances. You can probably remove the subscription since you don't need the ViewModel to act on these changes.
        //}

        // Build the table for the main UI based on the provided MODBUS Data from the MODBUSService Model
        private void UpdateModbusTable()
        {
            // Determine which columns should be visible, based on the provided DataLength from the Model!
            for (int i = 0; i < ColsVis.Length; i++)
                ColsVis[i] = MS.DataLength > (i * 20) ? Visibility.Visible : Visibility.Collapsed;

            // Update shift column contents, in case the SelectedAddressConvention was updated.
            // Make it so this only updates if SelectedAddressConvention has been changed since the last call of this method!
            ShiftColumn.Clear();
            for (int i = 0; i < 20; i++)
            {
                // This will print each index as i if counting from 0, or i+1 if counting from 1
                string content = $"+{(SelectedAddressConvention == "Register Address (starting from 0)" ? i : i + 1)}";
                ShiftColumn.Add(new StringWrapper(content));
            }

            // Prepare the name data, in case we need to keep it around for the next table build
            string[] namesCache = new string[MS.DataLength];
            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i] == null)
                    Names[i] = new ObservableCollection<StringWrapper>();
                else
                {
                    for(int j = 0; j < Names[i].Count; j++)
                    {
                        // Only save this name for the new display if we know that we'll see it in the new length.
                        // Otherwise, we'll risk exceeding the size of the length cache with a name we won't even need.
                        if (((i * 20) + j) < MS.DataLength)
                        {
                            string? temp = Names[i][j].Value;
                            if (temp == null)
                                namesCache[(i * 20) + j] = "";
                            else
                                namesCache[(i * 20) + j] = temp;
                        }
                    }
                }
                Names[i].Clear();
            }

            // Loop through each column of the current ModbusData collection and clear them.
            // If any columns are null (somehow), create the collection there
            for (int i = 0; i < ModbusGrid.Length; i++)
            {
                if (ModbusGrid[i] == null)
                    ModbusGrid[i] = new ObservableCollection<StringWrapper>();

                ModbusGrid[i].Clear();
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

                    // Only try to pull MODBUS data if we have a connection!
                    StringWrapper data;
                    if (MS.IsConnected)
                        // Retrieve existing item if present; otherwise create one instance
                        data = MS.ModbusData[idx] ?? new StringWrapper("");
                    else
                        data = new StringWrapper("");

                    Names[i].Add(name);
                    ModbusGrid[i].Add(data);
                }
            }

            // Notify the UI of element updates
            OnPropertyChanged(nameof(ColsVis));
            OnPropertyChanged(nameof(ShiftColumn));
            OnPropertyChanged(nameof(Names)); // Might not be needed, since this is an ObservableCollection...
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
                SaveEndian = MS.SelectedEndian,
                SaveAsciiEnable = MS.AsciiEnable,
                SaveAsciiDisplayType = MS.SelectedAsciiDisplayType,

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
            MS.DeviceId = lD.SaveDeviceId;
            MS.DataLength = lD.SaveLength;
            MS.StartAddress = lD.SaveStartAddress;
            MS.SelectedDataType = lD.SaveDataType;
            MS.SelectedNumericBase = lD.SaveNumericBase;
            MS.SelectedEndian = lD.SaveEndian;
            MS.AsciiEnable = lD.SaveAsciiEnable;
            MS.SelectedAsciiDisplayType = lD.SaveAsciiDisplayType;

            // ViewModel specific save parameter
            SelectedAddressConvention = lD.SaveAddressConv;

            ADisplayTypeDropDown = lD.SaveAsciiEnable ? Visibility.Visible : Visibility.Hidden; // Ensure the ADisplayType dropdown visibility is consistent with the loaded ASCIIEnable value
            
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
            // TODO: Implement connection logic
            if(MS.IsConnected == false)
            {
                MS.Connection();
            }
            else
            {
                // setting this to false will trigger the disconnect on the parallel thread's while loop!
                MS.IsConnected = false;
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
