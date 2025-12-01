using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MinimalEndpoints.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EndpointDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor AsyncRequired = new(
        "ME001",
        "Endpoint method must be async",
        "Endpoint method '{0}' must return a Task (async). All endpoint methods must be asynchronous.",
        "MinimalEndpoints",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InterfaceRequired = new(
        "ME002",
        "Endpoint class must implement IMinimalEndpoint",
        "Endpoint class '{0}' must implement the IMinimalEndpoint interface",
        "MinimalEndpoints",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor SingleEndpoint = new(
        "ME003",
        "Only one endpoint per file allowed",
        "Endpoint class '{0}' contains {1} HTTP method endpoints. Only one endpoint per file is allowed.",
        "MinimalEndpoints",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        AsyncRequired,
        InterfaceRequired,
        SingleEndpoint
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Check for MinimalEndpoints attribute or Http Methods
        var hasMinimalEndpointsAttribute = namedType.GetAttributes()
            .Any(a => a.AttributeClass?.Name.Contains("MinimalEndpoints") == true);

        var httpMethods = namedType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public &&
                       !m.IsStatic &&
                       m.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Http") == true))
            .ToList();

        var hasHttpMethodAttributes = httpMethods.Any();

        if (!hasMinimalEndpointsAttribute && !hasHttpMethodAttributes)
            return;

        // ME002: Must implement IMinimalEndpoint
        // Using string check for interface name
        if (!namedType.AllInterfaces.Any(i => i.ToDisplayString().Contains("IMinimalEndpoint")))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InterfaceRequired,
                namedType.Locations.FirstOrDefault(),
                namedType.Name));
        }

        // ME003: Single endpoint per file
        if (httpMethods.Count > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SingleEndpoint,
                namedType.Locations.FirstOrDefault(),
                namedType.Name,
                httpMethods.Count));
        }

        // ME001: Async required
        foreach (var method in httpMethods)
        {
            if (!method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains("Task"))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    AsyncRequired,
                    method.Locations.FirstOrDefault(),
                    method.Name));
            }
        }
    }
}
