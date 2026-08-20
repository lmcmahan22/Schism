// <copyright file="BoardAvailableViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using Schiism.Core.Configuration;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.WPF.Models;
    using System.Runtime.CompilerServices;

    public class BoardAvailableViewModel : BindableBase
    {
        // Private variable
        private string title;

        private string boardID;
        private string width;
        private bool failedBoard;
        private bool flippedBoard;
        private string topBarcode;
        private string bottomBarcode;
        private string partName;

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

        public bool FailedBoard
        {
            get => failedBoard;
            set => SetProperty(ref failedBoard, value);
        }

        public bool FlippedBoard
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
            this.BAState.SetBA(this.BoardID, this.Width, this.FailedBoard, this.FlippedBoard, this.TopBarcode, this.BottomBarcode, this.PartName);
            this.BAState.TriggerSend();
        }
    }
}
