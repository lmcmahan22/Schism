// <copyright file="BoardAvailableViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using Schiism.Core.Configuration;
    using Schiism.Core.Configuration.Enums;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.WPF.Models;
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;

    public class BoardAvailableViewModel : BindableBase
    {
        // Private variable
        private string title;

        private string boardID;
        private string width = "--";
        private FailType failedBoard;
        private FlipType flippedBoard;
        private string topBarcode = "--";
        private string bottomBarcode = "--";
        private string partName = "--";
        private bool receiptDir = false;

        private DelegateCommand? sendClick;

        public DelegateCommand SendClick =>
    sendClick ??= new DelegateCommand(ExecuteSendClick);

        public string BoardID
        {
            get => boardID;
            set => SetProperty(ref boardID, value);
        }

        public string BoardId
        {
            get => boardID;
            set => SetProperty(ref boardID, value);
        }

        public string Width
        {
            get => width;
            set => SetProperty(ref width, value);
        }

        public FailType FailedBoard
        {
            get => failedBoard;
            set => SetProperty(ref failedBoard, value);
        }

        public FlipType FlippedBoard
        {
            get => flippedBoard;
            set => SetProperty(ref flippedBoard, value);
        }

        public string TopBarcode
        {
            get => topBarcode;
            set => SetProperty(ref topBarcode, value);
        }

        public string BottomBarcode
        {
            get => bottomBarcode;
            set => SetProperty(ref bottomBarcode, value);
        }

        public string PartName
        {
            get => partName;
            set => SetProperty(ref partName, value);
        }

        public bool ReceiptDir
        {
            get => this.receiptDir;
            set => SetProperty(ref this.receiptDir, value);
        }

        public ObservableCollection<EnumOption<FailType>> FailedOptions { get; } =
        [
            new() { Value = FailType.Unknown, Display = "Unknown" },
            new() { Value = FailType.Good, Display = "Good" },
            new() { Value = FailType.Failed, Display = "Failed" },
        ];

        public ObservableCollection<EnumOption<FlipType>> FlippedOptions { get; } =
        [
            new() { Value = FlipType.Unknown, Display = "Unknown" },
            new() { Value = FlipType.NotFlipped, Display = "Not Flipped" },
            new() { Value = FlipType.Flipped, Display = "Flipped" },
        ];

        public BoardAvailableState BAState { get; }

        // Constructor
        public BoardAvailableViewModel(BoardAvailableState bAState)
        {
            BAState = bAState;
            title = "Board Available Config";
        }

        // Public instance with getter/setter
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        private void ExecuteSendClick()
        {
            this.BAState.SetBA(this.BoardID, this.Width, this.FailedBoard, this.FlippedBoard, this.ReceiptDir, this.TopBarcode, this.BottomBarcode, this.PartName);
            this.BAState.TriggerSend();
        }
    }
}
