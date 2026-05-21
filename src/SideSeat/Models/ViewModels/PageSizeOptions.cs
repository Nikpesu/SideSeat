namespace SideSeat.Models.ViewModels;

public static class PageSizeOptions
{
    public static int Normalize(int pageSize, int defaultValue = 25)
    {
        return pageSize switch
        {
            0 => 0,
            10 or 25 or 50 or 100 => pageSize,
            _ => defaultValue
        };
    }
}