using System;
using System.IO;
using System.Runtime.InteropServices;

































































}    }        }            ClosePrinter(hPrinter);            EndDocPrinter(hPrinter);            EndPagePrinter(hPrinter);            Marshal.FreeCoTaskMem(unmanagedBytes);        {        finally        }            }                throw new InvalidOperationException($"WritePrinter failed with error {err}");                var err = Marshal.GetLastWin32Error();            {            if (!WritePrinter(hPrinter, unmanagedBytes, bytes.Length, out var written) || written != bytes.Length)            Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);        {        try
n        var unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);        }            throw new InvalidOperationException($"StartPagePrinter failed with error {err}");            var err = Marshal.GetLastWin32Error();            ClosePrinter(hPrinter);            EndDocPrinter(hPrinter);        {
n        if (!StartPagePrinter(hPrinter))        }            throw new InvalidOperationException($"StartDocPrinter failed with error {err}");            var err = Marshal.GetLastWin32Error();            ClosePrinter(hPrinter);        {        if (!StartDocPrinter(hPrinter, 1, di))
n        var di = new DOCINFOA { pDocName = "Raw Document", pDataType = "RAW" };        }            throw new InvalidOperationException($"OpenPrinter failed with error {err}");            var err = Marshal.GetLastWin32Error();        {
n        if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))        if (bytes == null || bytes.Length == 0) throw new ArgumentException("No data to print");    {
n    public static void SendBytesToPrinter(string printerName, byte[] bytes)    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);
n    [DllImport("winspool.drv", SetLastError = true)]    private static extern bool EndPagePrinter(IntPtr hPrinter);
n    [DllImport("winspool.drv", SetLastError = true)]    private static extern bool StartPagePrinter(IntPtr hPrinter);
n    [DllImport("winspool.drv", SetLastError = true)]    private static extern bool EndDocPrinter(IntPtr hPrinter);
n    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]    private static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In] DOCINFOA di);
n    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]    private static extern bool ClosePrinter(IntPtr hPrinter);
n    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);
n    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]    }        [MarshalAs(UnmanagedType.LPStr)] public string pDataType;        [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;        [MarshalAs(UnmanagedType.LPStr)] public string pDocName;    {    private class DOCINFOA    [StructLayout(LayoutKind.Sequential)]{npublic static class RawPrinterHelper