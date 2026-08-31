namespace Schiism.Core.IPC.DTOs
{
    using System.Text.Json.Serialization;
    using Schiism.Core.Configuration.Enums;

    public record BoardAvailableDTO
    {
        [JsonConstructor]
        public BoardAvailableDTO(string boardId, string width, byte failedBoard, byte flippedBoard, bool receiptDir, string topBarcode, string bottomBarcode, string partName)
        {
            BoardId = boardId;
            Width = width;
            FailedBoard = failedBoard;
            FlippedBoard = flippedBoard;
            ReceiptDir = receiptDir;
            TopBarcode = topBarcode;
            BottomBarcode = bottomBarcode;
            PartName = partName;
        }

        public string BoardId { get; init; }

        public string Width { get; init; }

        public byte FailedBoard { get; init; }

        public byte FlippedBoard { get; init; }

        public bool ReceiptDir { get; init; }

        public string TopBarcode { get; init; }

        public string BottomBarcode { get; init; }

        public string PartName { get; init; }
    }
}
