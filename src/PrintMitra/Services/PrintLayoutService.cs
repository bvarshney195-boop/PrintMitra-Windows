using PrintMitra.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrintMitra.Services;

public sealed class PrintLayoutService
{
    public const double CardWidthMm = 85.60;
    public const double CardHeightMm = 53.98;

    public FixedDocument BuildIdentityCardPage(
        string? frontPath, string? backPath, PaperDefinition paper, CalibrationProfile calibration,
        int sets = 1, bool cuttingGuides = true)
    {
        var pageWidth = UnitConverter.MmToDip(paper.WidthMm);
        var pageHeight = UnitConverter.MmToDip(paper.HeightMm);
        var page = new FixedPage { Width = pageWidth, Height = pageHeight, Background = Brushes.White };

        var cardWidth = UnitConverter.MmToDip(CardWidthMm) * calibration.ScaleX;
        var cardHeight = UnitConverter.MmToDip(CardHeightMm) * calibration.ScaleY;
        var gap = UnitConverter.MmToDip(8);
        var margin = UnitConverter.MmToDip(15);
        var x = margin;
        var y = margin;

        for (var set = 0; set < Math.Max(1, sets); set++)
        {
            if (frontPath is not null)
            {
                AddImage(page, frontPath, x, y, cardWidth, cardHeight);
                if (cuttingGuides) AddGuide(page, x, y, cardWidth, cardHeight);
            }
            if (backPath is not null)
            {
                var backX = x + cardWidth + gap;
                if (backX + cardWidth > pageWidth - margin) { backX = margin; y += cardHeight + gap; }
                AddImage(page, backPath, backX, y, cardWidth, cardHeight);
                if (cuttingGuides) AddGuide(page, backX, y, cardWidth, cardHeight);
            }
            y += cardHeight + gap;
            if (y + cardHeight > pageHeight - margin) break;
        }

        var content = new PageContent();
        ((IAddChild)content).AddChild(page);
        var document = new FixedDocument();
        document.DocumentPaginator.PageSize = new Size(pageWidth, pageHeight);
        document.Pages.Add(content);
        return document;
    }

    public FixedDocument BuildCalibrationPage(PaperDefinition paper)
    {
        var width = UnitConverter.MmToDip(paper.WidthMm);
        var height = UnitConverter.MmToDip(paper.HeightMm);
        var page = new FixedPage { Width = width, Height = height, Background = Brushes.White };
        var line = new System.Windows.Shapes.Line
        {
            X1 = 0, X2 = UnitConverter.MmToDip(100), Y1 = 0, Y2 = 0,
            Stroke = Brushes.Black, StrokeThickness = 1
        };
        FixedPage.SetLeft(line, UnitConverter.MmToDip(30)); FixedPage.SetTop(line, UnitConverter.MmToDip(70)); page.Children.Add(line);
        var box = new Border
        {
            Width = UnitConverter.MmToDip(50), Height = UnitConverter.MmToDip(50),
            BorderBrush = Brushes.Black, BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = "50 × 50 mm", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        FixedPage.SetLeft(box, UnitConverter.MmToDip(55)); FixedPage.SetTop(box, UnitConverter.MmToDip(110)); page.Children.Add(box);
        var content = new PageContent(); ((IAddChild)content).AddChild(page);
        var document = new FixedDocument(); document.DocumentPaginator.PageSize = new Size(width, height); document.Pages.Add(content);
        return document;
    }

    private static void AddImage(FixedPage page, string path, double x, double y, double width, double height)
    {
        var source = new BitmapImage();
        source.BeginInit(); source.CacheOption = BitmapCacheOption.OnLoad; source.UriSource = new Uri(path); source.EndInit(); source.Freeze();
        var image = new Image { Source = source, Width = width, Height = height, Stretch = Stretch.Fill };
        FixedPage.SetLeft(image, x); FixedPage.SetTop(image, y); page.Children.Add(image);
    }

    private static void AddGuide(FixedPage page, double x, double y, double width, double height)
    {
        var guide = new Border { Width = width, Height = height, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(.6) };
        FixedPage.SetLeft(guide, x); FixedPage.SetTop(guide, y); page.Children.Add(guide);
    }
}
