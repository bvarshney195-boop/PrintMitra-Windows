using Microsoft.Win32;
using PrintMitra.Models;
using PrintMitra.Services;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PrintMitra;

public partial class MainWindow : Window
{
    private readonly PrivacyWorkspace _workspace = new();
    private readonly CardImageProcessor _processor = new();
    private readonly PrinterService _printers = new();
    private readonly PrintLayoutService _layouts = new();
    private readonly GeneralLayoutService _generalLayouts = new();
    private readonly ScanService _scanner = new();
    private readonly CalibrationStore _calibrations = new();
    private string? _frontSource, _backSource, _frontCorrected, _backCorrected;
    private readonly List<string> _photoFiles = new();
    private readonly List<string> _documentImageFiles = new();
    private string? _selectedPdf, _passportPath, _scanPath;
    private FixedDocument? _lastDocument;
    private CalibrationProfile _calibration = CalibrationProfile.Default("Unselected printer");

    public MainWindow()
    {
        InitializeComponent();
        PrivacyWorkspace.CleanupStale();
        PaperCombo.ItemsSource = PaperDefinition.Common;
        PaperCombo.SelectedIndex = 0;
        foreach (var combo in new[] { PhotoPaperCombo, DocumentPaperCombo, PassportPaperCombo, ScanPaperCombo })
        {
            combo.ItemsSource = PaperDefinition.Common;
            combo.SelectedIndex = 0;
        }
        LoadPrinters();
        RefreshCalibrationPreview();
        Closed += (_, _) => _workspace.Dispose();
    }

    private void LoadPrinters()
    {
        try
        {
            PrinterCombo.ItemsSource = _printers.GetInstalledPrinters();
            if (PrinterCombo.Items.Count > 0) PrinterCombo.SelectedIndex = 0;
            else AppStatus.Text = "No installed printer was found.";
        }
        catch (Exception ex) { AppStatus.Text = $"Printer discovery failed: {ex.Message}"; }
    }

    private void Show(string panel)
    {
        HomePanel.Visibility = panel == "home" ? Visibility.Visible : Visibility.Collapsed;
        PhotosPanel.Visibility = panel == "photos" ? Visibility.Visible : Visibility.Collapsed;
        DocumentsPanel.Visibility = panel == "documents" ? Visibility.Visible : Visibility.Collapsed;
        PassportPanel.Visibility = panel == "passport" ? Visibility.Visible : Visibility.Collapsed;
        ScanPanel.Visibility = panel == "scan" ? Visibility.Visible : Visibility.Collapsed;
        CardPanel.Visibility = panel == "card" ? Visibility.Visible : Visibility.Collapsed;
        PreviewPanel.Visibility = panel == "preview" ? Visibility.Visible : Visibility.Collapsed;
        CalibrationPanel.Visibility = panel == "calibration" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowHome_Click(object sender, RoutedEventArgs e) => Show("home");
    private void ShowPhotos_Click(object sender, RoutedEventArgs e) => Show("photos");
    private void ShowDocuments_Click(object sender, RoutedEventArgs e) => Show("documents");
    private void ShowPassport_Click(object sender, RoutedEventArgs e) => Show("passport");
    private void ShowScan_Click(object sender, RoutedEventArgs e) => Show("scan");
    private void ShowCard_Click(object sender, RoutedEventArgs e) => Show("card");
    private void ShowPreview_Click(object sender, RoutedEventArgs e) { if (_lastDocument is null) RefreshPreview(); else PreviewViewer.Document = _lastDocument; Show("preview"); }
    private void ShowCalibration_Click(object sender, RoutedEventArgs e) { RefreshCalibrationPreview(); Show("calibration"); }

    private void ChoosePhotos_Click(object sender, RoutedEventArgs e)
    {
        var dialog = ImageDialog(true, "Choose photographs");
        if (dialog.ShowDialog() != true) return;
        _photoFiles.Clear(); _photoFiles.AddRange(dialog.FileNames);
        PhotoFilesList.ItemsSource = null; PhotoFilesList.ItemsSource = _photoFiles.Select(Path.GetFileName).ToArray();
    }

    private void PreviewPhotos_Click(object sender, RoutedEventArgs e)
    {
        if (PhotoPaperCombo.SelectedItem is not PaperDefinition paper || _photoFiles.Count == 0) { MessageBox.Show("Choose at least one photograph."); return; }
        var columns = int.TryParse(((PhotoColumnsCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()), out var value) ? value : 2;
        var fill = PhotoCropCombo.SelectedIndex == 1;
        PresentDocument(_generalLayouts.BuildPhotoGrid(_photoFiles, paper, columns, 10, 5, fill), "Photo sheet preview", "Review the photo arrangement and paper selection.", false);
    }

    private void ChooseDocuments_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choose document pages or PDF", Filter = "Documents and images|*.pdf;*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff", Multiselect = true };
        if (dialog.ShowDialog() != true) return;
        _documentImageFiles.Clear();
        _documentImageFiles.AddRange(dialog.FileNames.Where(p => !p.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)));
        _selectedPdf = dialog.FileNames.FirstOrDefault(p => p.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
        DocumentFilesList.ItemsSource = dialog.FileNames.Select(Path.GetFileName).ToArray();
        OpenPdfButton.IsEnabled = _selectedPdf is not null;
    }

    private void OpenPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPdf is null) return;
        Process.Start(new ProcessStartInfo(_selectedPdf) { UseShellExecute = true });
        AppStatus.Text = "PDF opened in the default Windows application. Press Ctrl+P there to print.";
    }

    private void PreviewDocuments_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentPaperCombo.SelectedItem is not PaperDefinition paper || _documentImageFiles.Count == 0)
        {
            if (_selectedPdf is not null) { OpenPdf_Click(sender, e); return; }
            MessageBox.Show("Choose one or more photographed or scanned document pages."); return;
        }
        PresentDocument(_generalLayouts.BuildDocumentPages(_documentImageFiles, paper, 10, DocumentFitCheck.IsChecked == true), "Document preview", "Each selected image is placed on a separate page.", false);
    }

    private void ChoosePassport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = ImageDialog(false, "Choose portrait photograph");
        if (dialog.ShowDialog() != true) return;
        _passportPath = dialog.FileName; PassportImage.Source = LoadBitmap(dialog.FileName);
    }

    private void PassportPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (PassportPresetCombo.SelectedIndex == 0) { PassportWidthBox.Text = "35"; PassportHeightBox.Text = "45"; }
        else if (PassportPresetCombo.SelectedIndex == 1) { PassportWidthBox.Text = "50.8"; PassportHeightBox.Text = "50.8"; }
    }

    private void PreviewPassport_Click(object sender, RoutedEventArgs e)
    {
        if (_passportPath is null || PassportPaperCombo.SelectedItem is not PaperDefinition paper) { MessageBox.Show("Choose a portrait photograph."); return; }
        if (!double.TryParse(PassportWidthBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var width) || !double.TryParse(PassportHeightBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var height) || !int.TryParse(PassportCopiesBox.Text, out var copies)) { MessageBox.Show("Enter valid photo dimensions and copies."); return; }
        PresentDocument(_generalLayouts.BuildPassportSheet(_passportPath, paper, width, height, copies, PassportGuidesCheck.IsChecked == true), "Passport / ID photo preview", $"Photo size: {width:0.##} × {height:0.##} mm. Verify authority requirements before printing.", true);
    }

    private void AcquireScan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _scanPath = _scanner.AcquireImage(_workspace.NewFile(".jpg"));
            ScannedImage.Source = LoadBitmap(_scanPath); AppStatus.Text = "Scan acquired locally.";
        }
        catch (OperationCanceledException) { AppStatus.Text = "Scanning cancelled."; }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Scanner"); }
    }

    private void PreviewScan_Click(object sender, RoutedEventArgs e)
    {
        if (_scanPath is null || ScanPaperCombo.SelectedItem is not PaperDefinition paper) { MessageBox.Show("Scan a page first."); return; }
        PresentDocument(_generalLayouts.BuildDocumentPages(new[] { _scanPath }, paper, 10, ScanFitCheck.IsChecked == true), "Scanned-page preview", "Review the scanned page before printing.", false);
    }

    private void PrintAgain_Click(object sender, RoutedEventArgs e)
    {
        if (_lastDocument is null) return;
        try { if (_printers.PrintDocument(_lastDocument, PrinterCombo.SelectedItem as string)) AppStatus.Text = "Previous in-session job sent again."; }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Reprint failed"); }
    }

    private void ChooseFront_Click(object sender, RoutedEventArgs e) => ChooseImage(true);
    private void ChooseBack_Click(object sender, RoutedEventArgs e) => ChooseImage(false);

    private void ChooseImage(bool front)
    {
        var dialog = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff", Multiselect = false };
        if (dialog.ShowDialog() != true) return;
        if (front) { _frontSource = dialog.FileName; FrontImage.Source = LoadBitmap(dialog.FileName); }
        else { _backSource = dialog.FileName; BackImage.Source = LoadBitmap(dialog.FileName); }
        CorrectionStatus.Text = "Image selected. Ready for offline correction.";
    }

    private void CorrectAndPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_frontSource is null && _backSource is null) { MessageBox.Show("Choose at least one card photograph.", "PrintMitra"); return; }
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            if (_frontSource is not null) _frontCorrected = _processor.CorrectCard(_frontSource, _workspace.NewFile(".png"));
            if (_backSource is not null) _backCorrected = _processor.CorrectCard(_backSource, _workspace.NewFile(".png"));
            CorrectionStatus.Text = "Perspective and clarity corrected.";
            RefreshPreview(); Show("preview");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Unable to correct image", MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { Mouse.OverrideCursor = null; }
    }

    private async void PrinterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PrinterCombo.SelectedItem is string printer) _calibration = await _calibrations.GetAsync(printer);
        RefreshPreview();
    }

    private void PrinterProperties_Click(object sender, RoutedEventArgs e)
    {
        try { _printers.OpenPrinterProperties(PrinterCombo.SelectedItem as string ?? ""); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Printer Properties"); }
    }

    private void PaperCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshPreview();
    private void SetsBox_TextChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) RefreshPreview(); }
    private void PreviewOption_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) RefreshPreview(); }

    private void RefreshPreview()
    {
        if (PreviewViewer is null || PaperCombo.SelectedItem is not PaperDefinition paper) return;
        var sets = int.TryParse(SetsBox?.Text, out var count) ? Math.Clamp(count, 1, 20) : 1;
        var document = _layouts.BuildIdentityCardPage(_frontCorrected ?? _frontSource, _backCorrected ?? _backSource, paper, _calibration, sets, CutGuidesCheck?.IsChecked == true);
        PreviewViewer.Document = document;
        _lastDocument = document;
        ReprintHomeButton.IsEnabled = true;
        PreviewTitle.Text = "Aadhaar / PAN preview";
        PreviewSubtitle.Text = "Identity-card output is 85.60 × 53.98 mm. Keep driver scaling at Actual Size / 100%.";
        ExactSizeWarning.Visibility = Visibility.Visible;
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewViewer.Document is not System.Windows.Documents.FixedDocument document) return;
        if (Math.Abs(_calibration.ScaleX - 1) < .0001 && Math.Abs(_calibration.ScaleY - 1) < .0001)
        {
            var answer = MessageBox.Show("This printer has not been calibrated. Continue with default sizing?", "Exact-size warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }
        try { if (_printers.PrintDocument(document, PrinterCombo.SelectedItem as string)) AppStatus.Text = "Print job sent."; }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Printing failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void RefreshCalibrationPreview()
    {
        if (CalibrationViewer is not null) CalibrationViewer.Document = _layouts.BuildCalibrationPage(PaperDefinition.Common[0]);
    }

    private void PrintCalibration_Click(object sender, RoutedEventArgs e)
    {
        try { _printers.PrintDocument(_layouts.BuildCalibrationPage(PaperDefinition.Common[0]), PrinterCombo.SelectedItem as string); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Calibration print failed"); }
    }

    private async void SaveCalibration_Click(object sender, RoutedEventArgs e)
    {
        if (PrinterCombo.SelectedItem is not string printer) { MessageBox.Show("Select a printer first."); return; }
        if (!double.TryParse(MeasuredXBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var measuredX) || measuredX <= 0 ||
            !double.TryParse(MeasuredYBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var measuredY) || measuredY <= 0)
        { MessageBox.Show("Enter valid measured values."); return; }
        _calibration = new CalibrationProfile(printer, 100 / measuredX, 50 / measuredY, DateTimeOffset.UtcNow);
        await _calibrations.SaveAsync(_calibration);
        CalibrationStatus.Text = $"Saved for {printer}: X {_calibration.ScaleX:F4}, Y {_calibration.ScaleY:F4}";
        RefreshPreview();
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        var mode = (ModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        AppStatus.Text = $"{mode} mode · Offline";
    }

    private static BitmapImage LoadBitmap(string path)
    {
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.UriSource = new Uri(path); image.EndInit(); image.Freeze(); return image;
    }

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = e.Text.Any(c => !char.IsDigit(c));

    private static OpenFileDialog ImageDialog(bool multiple, string title) => new() { Title = title, Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff", Multiselect = multiple };

    private void PresentDocument(FixedDocument document, string title, string subtitle, bool exactSize)
    {
        PreviewViewer.Document = document;
        _lastDocument = document;
        ReprintHomeButton.IsEnabled = true;
        PreviewTitle.Text = title;
        PreviewSubtitle.Text = subtitle;
        ExactSizeWarning.Visibility = exactSize ? Visibility.Visible : Visibility.Collapsed;
        Show("preview");
    }
}
