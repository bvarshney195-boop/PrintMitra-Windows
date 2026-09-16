using OpenCvSharp;

namespace PrintMitra.Services;

public sealed class CardImageProcessor
{
    private const int OutputWidth = 1011; // 85.60 mm at 300 DPI
    private const int OutputHeight = 638; // 53.98 mm at 300 DPI

    public string CorrectCard(string inputPath, string outputPath)
    {
        using var source = Cv2.ImRead(inputPath, ImreadModes.Color);
        if (source.Empty()) throw new InvalidDataException("The selected image could not be opened.");

        using var resized = ResizeForDetection(source);
        using var gray = new Mat();
        using var blurred = new Mat();
        using var edges = new Mat();
        Cv2.CvtColor(resized, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);
        Cv2.Canny(blurred, edges, 60, 180);
        Cv2.FindContours(edges, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        var quad = contours
            .OrderByDescending(Cv2.ContourArea)
            .Select(c => Cv2.ApproxPolyDP(c, 0.02 * Cv2.ArcLength(c, true), true))
            .FirstOrDefault(p => p.Length == 4 && Cv2.ContourArea(p) > resized.Width * resized.Height * 0.12);

        Mat corrected;
        if (quad is null)
        {
            corrected = CenterCropToCardRatio(source);
        }
        else
        {
            var scaleX = (double)source.Width / resized.Width;
            var scaleY = (double)source.Height / resized.Height;
            var sourcePoints = OrderCorners(quad.Select(p => new Point2f((float)(p.X * scaleX), (float)(p.Y * scaleY))).ToArray());
            var targetPoints = new[]
            {
                new Point2f(0, 0), new Point2f(OutputWidth - 1, 0),
                new Point2f(OutputWidth - 1, OutputHeight - 1), new Point2f(0, OutputHeight - 1)
            };
            using var transform = Cv2.GetPerspectiveTransform(sourcePoints, targetPoints);
            corrected = new Mat();
            Cv2.WarpPerspective(source, corrected, transform, new Size(OutputWidth, OutputHeight), InterpolationFlags.Lanczos4);
        }

        using (corrected)
        using (var lab = new Mat())
        {
            Cv2.CvtColor(corrected, lab, ColorConversionCodes.BGR2Lab);
            var channels = Cv2.Split(lab);
            try
            {
                using var clahe = Cv2.CreateCLAHE(2.0, new Size(8, 8));
                clahe.Apply(channels[0], channels[0]);
                Cv2.Merge(channels, lab);
                Cv2.CvtColor(lab, corrected, ColorConversionCodes.Lab2BGR);
                Cv2.ImWrite(outputPath, corrected, new[] { new ImageEncodingParam(ImwriteFlags.PngCompression, 3) });
            }
            finally
            {
                foreach (var channel in channels) channel.Dispose();
            }
        }
        return outputPath;
    }

    private static Mat ResizeForDetection(Mat source)
    {
        const int max = 1400;
        if (Math.Max(source.Width, source.Height) <= max) return source.Clone();
        var scale = (double)max / Math.Max(source.Width, source.Height);
        var result = new Mat();
        Cv2.Resize(source, result, new Size((int)Math.Round(source.Width * scale), (int)Math.Round(source.Height * scale)));
        return result;
    }

    private static Mat CenterCropToCardRatio(Mat source)
    {
        var ratio = (double)OutputWidth / OutputHeight;
        var width = source.Width;
        var height = (int)(width / ratio);
        if (height > source.Height) { height = source.Height; width = (int)(height * ratio); }
        var rect = new Rect((source.Width - width) / 2, (source.Height - height) / 2, width, height);
        using var cropped = new Mat(source, rect);
        var output = new Mat();
        Cv2.Resize(cropped, output, new Size(OutputWidth, OutputHeight), 0, 0, InterpolationFlags.Lanczos4);
        return output;
    }

    private static Point2f[] OrderCorners(Point2f[] points)
    {
        var topLeft = points.OrderBy(p => p.X + p.Y).First();
        var bottomRight = points.OrderByDescending(p => p.X + p.Y).First();
        var topRight = points.OrderByDescending(p => p.X - p.Y).First();
        var bottomLeft = points.OrderBy(p => p.X - p.Y).First();
        return new[] { topLeft, topRight, bottomRight, bottomLeft };
    }
}
