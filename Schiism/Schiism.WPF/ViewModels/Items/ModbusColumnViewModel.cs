using Schiism.WPF.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
            set
            {
                if (!SetProperty(ref colIsUpdating, value))
                {
                    return;
                }

                // Ignore indeterminate state when user clicks header
                if (!value.HasValue)
                {
                    return;
                }

                // Set all rows according to this checkbox
                foreach (var row in Rows)
                {
                    row.IsUpdating = value.Value;
                }
            }
        }

        // Collection of rows in each column
        public ObservableCollection<ModbusRow> Rows { get; }

        // Commands
        // WPF Public Command properties
        public DelegateCommand<double?> ResizeWatchColumnCommand { get; }

        public DelegateCommand<double?> ResizeNameColumnCommand { get; }

        public DelegateCommand<double?> ResizeDataColumnCommand { get; }

        // Constructor
        public ModbusColumnViewModel(List<ModbusRow> rows)
        {
            Address = "0";

            Rows = [.. rows];

            ColIsUpdating = false;

            WatchWidth = 100;
            NameWidth = 100;
            DataWidth = 300;

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
                100,
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

        public void RowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModbusRow.IsUpdating))
            {
                UpdateColumnState();
            }
        }

        private void UpdateColumnState()
        {
            if (Rows.Count == 0)
            {
                ColIsUpdating = false;
                return;
            }

            bool allTrue = Rows.All(r => r.IsUpdating == true);
            bool allFalse = Rows.All(r => r.IsUpdating == false);

            if (allTrue)
            {
                colIsUpdating = true;
            }
            else if (allFalse)
            {
                colIsUpdating = false;
            }
            else
            {
                colIsUpdating = null;
            }

            RaisePropertyChanged(nameof(ColIsUpdating));
        }
    }
}
