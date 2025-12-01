using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TerraScale.MinimalEndpoints.Analyzers.Analyzers;

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

        // Only analyze concrete classes (skip interfaces, enums, delegates, attributes base types etc.)
        if (namedType.TypeKind != TypeKind.Class)
            return;

        // Skip attribute classes themselves (e.g., MinimalEndpointsAttribute)
        if (namedType.BaseType?.ToDisplayString().Contains("System.Attribute") == true)
            return;

        // Detect whether this named type looks like an endpoint class.
        // It can be marked with the legacy MinimalEndpoints attribute, contain
        // method-level Http* attributes, or expose Route/HttpMethod properties.
        var hasMinimalEndpointsAttribute = namedType.GetAttributes()
            .Any(a => a.AttributeClass?.Name.Contains("MinimalEndpoints") == true);

        var hasRouteOrMethodProperty = namedType.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.Name == "Route" || p.Name == "BaseRoute" || p.Name == "HttpMethod" || p.Name == "Method");

        var httpMethods = namedType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public &&
                       !m.IsStatic &&
                       m.MethodKind == MethodKind.Ordinary)
            .ToList();

        var hasHttpMethodAttributes = httpMethods.Any(m => m.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Http") == true));

        if (!hasMinimalEndpointsAttribute && !hasHttpMethodAttributes && !hasRouteOrMethodProperty)
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
        // If the class uses method-level Http attributes, count those methods; otherwise
        // count ordinary public instance methods and enforce single endpoint per class.
        int countToCheck = httpMethods.Count;
        if (hasHttpMethodAttributes)
        {
            countToCheck = httpMethods.Count(m => m.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Http") == true));
        }

        if (countToCheck > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SingleEndpoint,
                namedType.Locations.FirstOrDefault(),
                namedType.Name,
                countToCheck));
        }

        // ME001: Async required
        // Determine which methods are subject to async check: either those with Http attributes
        // or, if no method-level attributes are present, all ordinary public instance methods
        IEnumerable<IMethodSymbol> methodsToCheck;
        if (hasHttpMethodAttributes)
        {
            methodsToCheck = httpMethods.Where(m => m.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Http") == true));
        }
        else
        {
            methodsToCheck = httpMethods;
        }

        foreach (var method in methodsToCheck)
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
