using Schiism.WPF.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.WPF.Models
{
    public class BoardAvailableState : BindableBase
    {
        private string boardID;
        private string width;
        private bool failedBoard;
        private bool flippedBoard;
        private string topBarcode;
        private string bottomBarcode;
        private string partName;

        public event EventHandler? BASendTrigger;

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

        public void SetBA(string boardID, string width, bool failedBoard, bool flippedBoard, string topBarcode, string bottomBarcode, string partName)
        {
            BoardID = boardID;
            Width = width;
            FailedBoard = failedBoard;
            FlippedBoard = flippedBoard;
            TopBarcode = topBarcode;
            BottomBarcode = bottomBarcode;
            PartName = partName;
        }

        public void TriggerSend()
        {
            BASendTrigger?.Invoke(this, EventArgs.Empty);
        }
    }
}
