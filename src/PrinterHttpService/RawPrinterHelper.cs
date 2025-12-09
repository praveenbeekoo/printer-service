using System.Runtime.InteropServices;

namespace PrinterHttpService
{
    public static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName = string.Empty;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile = string.Empty;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType = "RAW";
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA",
            SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, byte[] data, int buf, out int pcWritten);

        public static void SendBytesToPrinter(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
                throw new Exception("Could not open printer: " + printerName);

            try
            {
                var di = new DOCINFOA
                {
                    pDocName = "ESC/POS Raw Print Job",
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, di))
                    throw new Exception("StartDocPrinter failed.");

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        throw new Exception("StartPagePrinter failed.");

                    try
                    {
                        if (!WritePrinter(hPrinter, bytes, bytes.Length, out int _))
                            throw new Exception("WritePrinter failed.");
                    }
                    finally
                    {
                        EndPagePrinter(hPrinter);
                    }
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}