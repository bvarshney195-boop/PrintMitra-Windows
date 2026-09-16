namespace PrintMitra.Services;

public static class UnitConverter
{
    public const double DipsPerInch = 96.0;
    public const double MmPerInch = 25.4;
    public static double MmToDip(double mm) => mm / MmPerInch * DipsPerInch;
}
