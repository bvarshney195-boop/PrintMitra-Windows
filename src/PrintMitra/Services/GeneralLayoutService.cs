using PrintMitra.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrintMitra.Services;

public sealed class GeneralLayoutService
{
    public FixedDocument BuildPhotoGrid(IReadOnlyList<string> paths, PaperDefinition paper, int columns, double marginMm, double gapMm, bool fill)
    {
        if (paths.Count == 0) throw new InvalidOperationException("Select at least one photograph.");
        columns = Math.Clamp(columns, 1, 5);
        var width = UnitConverter.MmToDip(paper.WidthMm);
        var height = UnitConverter.MmToDip(paper.HeightMm);
        var margin = UnitConverter.MmToDip(Math.Clamp(marginMm, 0, 30));
        var gap = UnitConverter.MmToDip(Math.Clamp(gapMm, 0, 20));
        var rows = Math.Max(1, (int)Math.Floor((height - (2 * margin) + gap) / ((width - (2 * margin) - ((columns - 1) * gap)) / columns * .75 + gap)));
        var perPage = Math.Max(1, columns * rows);
        var document = NewDocument(width, height);

        for (var offset = 0; offset < paths.Count; offset += perPage)
        {
            var page = NewPage(width, height);
            var cellWidth = (width - (2 * margin) - ((columns - 1) * gap)) / columns;
            var cellHeight = (height - (2 * margin) - ((rows - 1) * gap)) / rows;
            for (var i = 0; i < Math.Min(perPage, paths.Count - offset); i++)
            {
                var col = i % columns;
                var row = i / columns;
                AddImage(page, paths[offset + i], margin + col * (cellWidth + gap), margin + row * (cellHeight + gap), cellWidth, cellHeight, fill ? Stretch.UniformToFill : Stretch.Uniform);
            }
            AddPage(document, page);
        }
        return document;
    }

    public FixedDocument BuildDocumentPages(IReadOnlyList<string> paths, PaperDefinition paper, double marginMm, bool fit)
    {
        if (paths.Count == 0) throw new InvalidOperationException("Select at least one document image.");
        var width = UnitConverter.MmToDip(paper.WidthMm);
        var height = UnitConverter.MmToDip(paper.HeightMm);
        var margin = UnitConverter.MmToDip(Math.Clamp(marginMm, 0, 30));
        var document = NewDocument(width, height);
        foreach (var path in paths)
        {
            var page = NewPage(width, height);
            AddImage(page, path, margin, margin, width - 2 * margin, height - 2 * margin, fit ? Stretch.Uniform : Stretch.UniformToFill);
            AddPage(document, page);
        }
        return document;
    }

    public FixedDocument BuildPassportSheet(string path, PaperDefinition paper, double photoWidthMm, double photoHeightMm, int copies, bool cuttingGuides)
    {
        photoWidthMm = Math.Clamp(photoWidthMm, 15, 100);
        photoHeightMm = Math.Clamp(photoHeightMm, 15, 120);
        copies = Math.Clamp(copies, 1, 50);
        var width = UnitConverter.MmToDip(paper.WidthMm);
        var height = UnitConverter.MmToDip(paper.HeightMm);
        var photoWidth = UnitConverter.MmToDip(photoWidthMm);
        var photoHeight = UnitConverter.MmToDip(photoHeightMm);
        var margin = UnitConverter.MmToDip(12);
        var gap = UnitConverter.MmToDip(5);
        var columns = Math.Max(1, (int)Math.Floor((width - 2 * margin + gap) / (photoWidth + gap)));
        var rows = Math.Max(1, (int)Math.Floor((height - 2 * margin + gap) / (photoHeight + gap)));
        var perPage = columns * rows;
        var document = NewDocument(width, height);
        for (var offset = 0; offset < copies; offset += perPage)
        {
            var page = NewPage(width, height);
            for (var i = 0; i < Math.Min(perPage, copies - offset); i++)
            {
                var x = margin + (i % columns) * (photoWidth + gap);
                var y = margin + (i / columns) * (photoHeight + gap);
                AddImage(page, path, x, y, photoWidth, photoHeight, Stretch.UniformToFill);
                if (cuttingGuides) AddGuide(page, x, y, photoWidth, photoHeight);
            }
            AddPage(document, page);
        }
        return document;
    }

    private static FixedDocument NewDocument(double width, double height) => new() { DocumentPaginator = { PageSize = new Size(width, height) } };
    private static FixedPage NewPage(double width, double height) => new() { Width = width, Height = height, Background = Brushes.White };
    private static void AddPage(FixedDocument document, FixedPage page) { var content = new PageContent(); ((IAddChild)content).AddChild(page); document.Pages.Add(content); }

    private static void AddImage(FixedPage page, string path, double x, double y, double width, double height, Stretch stretch)
    {
        var source = new BitmapImage(); source.BeginInit(); source.CacheOption = BitmapCacheOption.OnLoad; source.UriSource = new Uri(path); source.EndInit(); source.Freeze();
        var image = new Image { Source = source, Width = width, Height = height, Stretch = stretch, ClipToBounds = true };
        FixedPage.SetLeft(image, x); FixedPage.SetTop(image, y); page.Children.Add(image);
    }

    private static void AddGuide(FixedPage page, double x, double y, double width, double height)
    {
        var guide = new Border { Width = width, Height = height, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(.6) };
        FixedPage.SetLeft(guide, x); FixedPage.SetTop(guide, y); page.Children.Add(guide);
    }
}
