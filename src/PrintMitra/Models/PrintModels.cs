namespace PrintMitra.Models;

public enum CardKind { Aadhaar, Pan }
public enum UserMode { Easy, Operator }

public sealed record PaperDefinition(string Name, double WidthMm, double HeightMm)
{
    public static readonly IReadOnlyList<PaperDefinition> Common = new[]
    {
        new PaperDefinition("A4", 210, 297),
        new PaperDefinition("A5", 148, 210),
        new PaperDefinition("B5", 176, 250),
        new PaperDefinition("Letter", 215.9, 279.4),
        new PaperDefinition("Legal", 215.9, 355.6),
        new PaperDefinition("Photo 4 × 6 in", 101.6, 152.4),
        new PaperDefinition("Photo 5 × 7 in", 127, 177.8),
        new PaperDefinition("Photo 8 × 10 in", 203.2, 254)
    };
}

public sealed record CalibrationProfile(string PrinterName, double ScaleX, double ScaleY, DateTimeOffset UpdatedAt)
{
    public static CalibrationProfile Default(string printer) => new(printer, 1, 1, DateTimeOffset.UtcNow);
}
