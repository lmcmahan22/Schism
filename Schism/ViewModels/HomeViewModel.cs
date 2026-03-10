using Schism.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

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
        private int _deviceID = 0;
        private int _length = 5;
        private int _startAddress = 0;
        private bool _asciiEnable = false;
        private string _address1 = "0";
        private Visibility[] _colsVis = new Visibility[6] { Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed };

        // Make a variable for the length of each table. Either 21 or the length of the last, unfinished column, whichever is smaller. This will be used to determine how many rows to show in the UI for each column.

        // Initialize collection at declaration to avoid CS8618
        private ObservableCollection<StringWrapper> _shiftColumn = new StringWrapperList();
        private ObservableCollection<DataPoint>[] _MODBUSDataPoints = new DataPointList[6];

        // Make command fields nullable to avoid CS8618
        private DelegateCommand? _saveClick;
        private DelegateCommand? _loadClick;
        private DelegateCommand? _exitClick;
        private DelegateCommand? _cutClick;
        private DelegateCommand? _copyClick;
        private DelegateCommand? _pasteClick;
        private DelegateCommand? _connClick;
        private DelegateCommand? _discClick;
        private DelegateCommand? _settClick;
        private DelegateCommand? _scanRtClick;
        private DelegateCommand? _insErrClick;
        private DelegateCommand? _themesClick;
        private DelegateCommand? _aboutClick;

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
            set { SetProperty(ref _deviceID, value); }
        }

        public int StartAddress
        {
            get { return _startAddress; }
            set
            {
                if (SetProperty(ref _startAddress, value))
                {
                    // update the textual address representation
                    Address1 = StartAddress.ToString();

                    OnPropertyChanged();
                }
            }
        }

        public string Address1
        {
            get { return _address1; }
            set { SetProperty(ref _address1, value); }
        }

        public int Length
        {
            get { return _length; }
            set
            {

                // Min and Max boundaries on Value
                value = Math.Min(value, 120);
                value = Math.Max(value, -1);

                // Rebuild the collection to reflect new length
                BuildModbusDataPoints();
                for (int i = 0; i < MODBUSDataPoints.Length; i++)
                {
                    _colsVis[i] = value > (i * 20) ? Visibility.Visible : Visibility.Collapsed;
                }

                // Notify the UI that the ColsVis contents changed. Updating this from the array's setter won't work since the array reference doesn't change, so we need to raise this manually.
                OnPropertyChanged(nameof(ColsVis));

                // Also notify length changed (CallerMemberName will be "Length")
                OnPropertyChanged();
            }
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
            set { SetProperty(ref _asciiEnable, value); }
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

        // View Model constructor
        public HomeViewModel()
        {
            // Ensure collection is populated with a header + Length rows
            BuildModbusDataPoints();
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
                var reqRows = Math.Min((Length - 20*i), 20); // Calculate how many rows we need in the last column (or 20 if length is greater than 20)
                for (int j = 0; j < reqRows; j++)
                {
                    string alias = "";
                    // UPDATE THIS WITH ACTUAL MODBUS DATA!
                    string data = (j * 25).ToString();
                    MODBUSDataPoints[i].Add(new DataPoint(alias, data));
                }
            }
        }

        // Public Command properties
        public DelegateCommand Save_Click =>
            _saveClick ??= new DelegateCommand(Execute_save_Click);

        void Execute_save_Click()
        {
            // TODO: Implement new file logic
        }

        public DelegateCommand Load_Click =>
            _loadClick ??= new DelegateCommand(Execute_Load_Click);

        void Execute_Load_Click()
        {
            // TODO: Implement file open logic
        }

        public DelegateCommand Exit_Click =>
            _exitClick ??= new DelegateCommand(Execute_Exit_Click);

        void Execute_Exit_Click()
        {
            //  TODO: Implement application exit logic
        }

        public DelegateCommand Cut_Click =>
            _cutClick ??= new DelegateCommand(Execute_Cut_Click);

        void Execute_Cut_Click()
        {
            //  TODO: Implement cut logic
        }

        public DelegateCommand Copy_Click =>
            _copyClick ??= new DelegateCommand(Execute_Copy_Click);

        void Execute_Copy_Click()
        {
            // TODO: Implement copy logic
        }

        public DelegateCommand Paste_Click =>
            _pasteClick ??= new DelegateCommand(Execute_Paste_Click);

        void Execute_Paste_Click()
        {
            // TODO: Implement paste logic
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
            // TODO: Implement settings dialog
        }

        public DelegateCommand ScanRt_Click =>
            _scanRtClick ??= new DelegateCommand(Execute_ScanRt_Click);

        void Execute_ScanRt_Click()
        {
            // TODO: Implement scan rate dialog
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
            // TODO: Implement theme selection dialog
        }

        public DelegateCommand About_Click =>
            _aboutClick ??= new DelegateCommand(Execute_About_Click);

        void Execute_About_Click()
        {
            // TODO: Implement About dialog
        }
    }
}
