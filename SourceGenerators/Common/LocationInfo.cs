using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GodotUtilities.SourceGenerators;

internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? From(Location? location)
    {
        if (location?.SourceTree is null)
            return null;

        return new LocationInfo(
            location.SourceTree.FilePath,
            location.SourceSpan,
            location.GetLineSpan().Span);
    }

    public static int Compare(LocationInfo? a, LocationInfo? b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a is null) return -1;
        if (b is null) return 1;

        var byFile = string.CompareOrdinal(a.FilePath, b.FilePath);
        return byFile != 0 ? byFile : a.TextSpan.Start.CompareTo(b.TextSpan.Start);
    }
}

internal static class LocationInfoExtensions
{
    public static Location ToLocationOrNone(this LocationInfo? info) => info?.ToLocation() ?? Location.None;
}
