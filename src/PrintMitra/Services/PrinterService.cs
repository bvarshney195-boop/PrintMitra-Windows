using System.Diagnostics;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PrintMitra.Services;

public sealed class PrinterService
{
    public IReadOnlyList<string> GetInstalledPrinters()
    {
        using var server = new LocalPrintServer();
        return server.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections })
            .Select(q => q.FullName).OrderBy(x => x).ToArray();
    }

    public void OpenPrinterProperties(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName)) throw new InvalidOperationException("Select a printer first.");
        Process.Start(new ProcessStartInfo
        {
            FileName = "rundll32.exe",
            Arguments = $"printui.dll,PrintUIEntry /e /n \"{printerName.Replace("\"", "")}\"",
            UseShellExecute = true
        });
    }

    public bool PrintDocument(FixedDocument document, string? preferredPrinter)
    {
        var dialog = new PrintDialog();
        if (!string.IsNullOrWhiteSpace(preferredPrinter))
        {
            using var server = new LocalPrintServer();
            var queue = server.GetPrintQueues().FirstOrDefault(q => q.FullName.Equals(preferredPrinter, StringComparison.OrdinalIgnoreCase));
            if (queue is not null) dialog.PrintQueue = queue;
        }
        if (dialog.ShowDialog() != true) return false;
        dialog.PrintDocument(document.DocumentPaginator, "PrintMitra job");
        return true;
    }
}
