using System.Reflection;

namespace PrintMitra.Services;

public sealed class ScanService
{
    public string AcquireImage(string outputPath)
    {
        var dialogType = Type.GetTypeFromProgID("WIA.CommonDialog") ?? throw new NotSupportedException("Windows Image Acquisition is not available.");
        object? dialog = null;
        object? image = null;
        try
        {
            dialog = Activator.CreateInstance(dialogType) ?? throw new InvalidOperationException("Unable to start the Windows scanner dialog.");
            image = dialogType.InvokeMember("ShowAcquireImage", BindingFlags.InvokeMethod, null, dialog, new object?[] { 1, 2, 0, "{B96B3CAB-0728-11D3-9D7B-0000F81EF32E}", true, true, false });
            if (image is null) throw new OperationCanceledException("Scanning was cancelled.");
            image.GetType().InvokeMember("SaveFile", BindingFlags.InvokeMethod, null, image, new object[] { outputPath });
            return outputPath;
        }
        finally
        {
            if (image is not null && System.Runtime.InteropServices.Marshal.IsComObject(image)) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(image);
            if (dialog is not null && System.Runtime.InteropServices.Marshal.IsComObject(dialog)) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dialog);
        }
    }
}
