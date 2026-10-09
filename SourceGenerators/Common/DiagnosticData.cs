using System.Linq;
using Microsoft.CodeAnalysis;

namespace GodotUtilities.SourceGenerators;

internal sealed record DiagnosticData(
    DiagnosticDescriptor Descriptor,
    LocationInfo? SourceLocation,
    EquatableArray<string> Args)
{
    public static DiagnosticData Create(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] args)
        => new(descriptor, location, new EquatableArray<string>(args));

    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(
            Descriptor,
            SourceLocation.ToLocationOrNone(),
            Args.Select(a => (object)a).ToArray());
}
