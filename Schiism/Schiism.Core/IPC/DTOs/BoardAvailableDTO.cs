namespace Schiism.Core.IPC.DTOs
{
    using System.Text.Json.Serialization;
    using Schiism.Core.Configuration.Enums;

    public record BoardAvailableDTO
    {
        [JsonConstructor]
        public BoardAvailableDTO(string boardId, string width, bool failedBoard, bool flippedBoard, string topBarcode, string bottomBarcode, string partName)
        {
            BoardId = boardId;
            Width = width;
            FailedBoard = failedBoard;
            FlippedBoard = flippedBoard;
            TopBarcode = topBarcode;
            BottomBarcode = bottomBarcode;
            PartName = partName;
        }

        public string BoardId { get; init; }

        public string Width { get; init; }

        public bool FailedBoard { get; init; }

        public bool FlippedBoard { get; init; }

        public string TopBarcode { get; init; }

        public string BottomBarcode { get; init; }

        public string PartName { get; init; }
    }
}
