using Schiism.WPF.Models;
using System.Collections.ObjectModel;

namespace Schiism.WPF.ViewModels.Items
{
    public class ModbusColumnViewModel : BindableBase
    {
        private string address;
        private double watchWidth;
        private double nameWidth;
        private double dataWidth;
        private bool? colIsUpdating;

        // MODBUS Coil/Register header address
        public string Address
        {
            get => address;

            // SetProperty uses the private variable, because you're taking assigning the field, then setting the property implicitly
            set => SetProperty(ref address, value);
        }

        // Jointed column widths
        public double WatchWidth
        {
            get => watchWidth;
            set => SetProperty(ref watchWidth, value);
        }

        public double NameWidth
        {
            get => nameWidth;
            set => SetProperty(ref nameWidth, value);
        }

        public double DataWidth
        {
            get => dataWidth;
            set => SetProperty(ref dataWidth, value);
        }

        public bool? ColIsUpdating
        {
            get => colIsUpdating;
            set => SetProperty(ref colIsUpdating, value);
        }

        // Collection of rows in each column
        public ObservableCollection<ModbusRow> Rows { get; }

        // Commands
        // WPF Public Command properties
        public DelegateCommand<double?> ResizeWatchColumnCommand { get; }

        public DelegateCommand<double?> ResizeNameColumnCommand { get; }

        public DelegateCommand<double?> ResizeDataColumnCommand { get; }

        // Constructor
        public ModbusColumnViewModel()
        {
            Address = "0";

            Rows = new ObservableCollection<ModbusRow>();

            ResizeWatchColumnCommand = new DelegateCommand<double?>(OnWatchResize);
            ResizeNameColumnCommand = new DelegateCommand<double?>(OnNameResize);
            ResizeDataColumnCommand = new DelegateCommand<double?>(OnDataResize);
        }

        public ModbusColumnViewModel(List<ModbusRow> rows)
        {
            Address = "0";

            Rows = [.. rows];

            ResizeWatchColumnCommand = new DelegateCommand<double?>(OnWatchResize);
            ResizeNameColumnCommand = new DelegateCommand<double?>(OnNameResize);
            ResizeDataColumnCommand = new DelegateCommand<double?>(OnDataResize);
        }

        private void OnWatchResize(double? delta)
        {
            if (!delta.HasValue)
            {
                return;
            }

            WatchWidth = Math.Max(
                50,
                WatchWidth + delta.Value);
        }

        private void OnNameResize(double? delta)
        {
            if (!delta.HasValue)
            {
                return;
            }

            NameWidth = Math.Max(
                50,
                NameWidth + delta.Value);
        }

        private void OnDataResize(double? delta)
        {
            if (!delta.HasValue)
            {
                return;
            }

            DataWidth = Math.Max(
                50,
                DataWidth + delta.Value);
        }
    }
}
