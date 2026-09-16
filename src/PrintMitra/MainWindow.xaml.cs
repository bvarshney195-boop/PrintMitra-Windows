using Microsoft.Win32;
using PrintMitra.Models;
using PrintMitra.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PrintMitra;

public partial class MainWindow : Window
{
    private readonly PrivacyWorkspace _workspace = new();
    private readonly CardImageProcessor _processor = new();
    private readonly PrinterService _printers = new();
    private readonly PrintLayoutService _layouts = new();
    private readonly CalibrationStore _calibrations = new();
    private string? _frontSource, _backSource, _frontCorrected, _backCorrected;
    private CalibrationProfile _calibration = CalibrationProfile.Default("Unselected printer");

    public MainWindow()
    {
        InitializeComponent();
        PrivacyWorkspace.CleanupStale();
        PaperCombo.ItemsSource = PaperDefinition.Common;
        PaperCombo.SelectedIndex = 0;
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
        CardPanel.Visibility = panel == "card" ? Visibility.Visible : Visibility.Collapsed;
        PreviewPanel.Visibility = panel == "preview" ? Visibility.Visible : Visibility.Collapsed;
        CalibrationPanel.Visibility = panel == "calibration" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowHome_Click(object sender, RoutedEventArgs e) => Show("home");
    private void ShowCard_Click(object sender, RoutedEventArgs e) => Show("card");
    private void ShowPreview_Click(object sender, RoutedEventArgs e) { RefreshPreview(); Show("preview"); }
    private void ShowCalibration_Click(object sender, RoutedEventArgs e) { RefreshCalibrationPreview(); Show("calibration"); }

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
        PreviewViewer.Document = _layouts.BuildIdentityCardPage(_frontCorrected ?? _frontSource, _backCorrected ?? _backSource, paper, _calibration, sets, CutGuidesCheck?.IsChecked == true);
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
}
