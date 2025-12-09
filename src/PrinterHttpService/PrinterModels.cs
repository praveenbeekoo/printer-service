namespace PrinterHttpService
{
    public class PrinterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsOffline { get; set; }
        public bool IsBusy { get; set; }
        public string Location { get; set; } = string.Empty;
    }

    public class PrintHexRequest
    {
        public string Printer { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty; // HEX string
    }

    public class PrintBase64Request
    {
        public string Printer { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty; // Base64 string
    }
}