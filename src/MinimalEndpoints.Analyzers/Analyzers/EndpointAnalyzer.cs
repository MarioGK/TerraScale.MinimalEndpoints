using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MinimalEndpoints.Helpers;
using MinimalEndpoints.Models;

namespace MinimalEndpoints.Analyzers;

internal static class EndpointAnalyzer
{
    public static bool IsEndpointClass(SyntaxNode syntaxNode)
    {
        return syntaxNode is ClassDeclarationSyntax classDeclaration &&
               (classDeclaration.AttributeLists.Any(static al =>
                   al.Attributes.Any(static a =>
                       a.Name.ToString().Contains("MinimalEndpoints"))) ||
                classDeclaration.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Any(static m => m.AttributeLists
                        .Any(static al => al.Attributes
                            .Any(static a => a.Name.ToString().Contains("Http")))));
    }

    public static ClassDeclarationSyntax? GetEndpointClass(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Check if class has MinimalEndpoints attribute or methods with HTTP attributes
        var hasMinimalEndpointsAttribute = classDeclaration.AttributeLists
            .Any(al => al.Attributes.Any(a => a.Name.ToString().Contains("MinimalEndpoints")));

        var hasHttpMethodAttributes = classDeclaration.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.AttributeLists
                .Any(al => al.Attributes
                    .Any(a => a.Name.ToString().Contains("Http"))));

        if (!hasMinimalEndpointsAttribute && !hasHttpMethodAttributes)
            return null;

        return classDeclaration;
    }

    public static EndpointMethod? AnalyzeEndpointMethod(IMethodSymbol methodSymbol, ClassDeclarationSyntax classSyntax, SemanticModel semanticModel, string baseRoute, List<Diagnostic> diagnostics)
    {
        // Find the method syntax
        var methodSyntax = classSyntax.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(semanticModel.GetDeclaredSymbol(m), methodSymbol));

        if (methodSyntax == null)
            return null;

        // Check if method has HTTP method attributes
        var httpMethod = string.Empty;
        var route = string.Empty;

        foreach (var attribute in methodSymbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.Name;
            if (attributeName != null)
            {
                if (attributeName.Contains("HttpGet"))
                {
                    httpMethod = "GET";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
                else if (attributeName.Contains("HttpPost"))
                {
                    httpMethod = "POST";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
                else if (attributeName.Contains("HttpPut"))
                {
                    httpMethod = "PUT";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
                else if (attributeName.Contains("HttpDelete"))
                {
                    httpMethod = "DELETE";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
                else if (attributeName.Contains("HttpPatch"))
                {
                    httpMethod = "PATCH";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
            }
        }

        // Skip methods without HTTP attributes
        if (string.IsNullOrEmpty(httpMethod))
            return null;

        // Check if method is async - all endpoints must be async
        if (!methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains("Task"))
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "ME001",
                    "Endpoint method must be async",
                    "Endpoint method '{0}' must return a Task (async). All endpoint methods must be asynchronous.",
                    "TerraScale.MinimalEndpoints",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                methodSyntax.GetLocation(),
                methodSymbol.Name);
            diagnostics.Add(diagnostic);
            return null;
        }

        // Get inner return type
        var returnTypeInner = "void";
        if (methodSymbol.ReturnType is INamedTypeSymbol namedReturnType &&
            namedReturnType.IsGenericType &&
            namedReturnType.Name == "Task")
        {
             var typeArg = namedReturnType.TypeArguments.FirstOrDefault();
             if (typeArg != null)
             {
                 returnTypeInner = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                 // Strip nullable annotation from reference types for typeof() compatibility
                 if (typeArg.IsReferenceType && returnTypeInner.EndsWith("?"))
                 {
                     returnTypeInner = returnTypeInner.Substring(0, returnTypeInner.Length - 1);
                 }
             }
        }

        // Combine base route with method route
        var fullRoute = CombineRoutes(baseRoute, route);

        // Analyze parameters
        var parameters = new List<EndpointParameter>();
        foreach (var param in methodSymbol.Parameters)
        {
            var endpointParam = new EndpointParameter
            {
                Name = param.Name,
                Type = param.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object",
                IsFromServices = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromServices") == true),
                IsFromBody = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromBody") == true),
                IsFromRoute = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromRoute") == true),
                IsFromQuery = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromQuery") == true)
            };
            parameters.Add(endpointParam);
        }

        // Analyze authorization attributes
        var hasAuthorize = methodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Authorize") == true);
        var hasAllowAnonymous = methodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("AllowAnonymous") == true);
        var authorizeAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name.Contains("Authorize") == true);

        string? policy = null;
        string? roles = null;
        string? authenticationSchemes = null;

        if (authorizeAttribute != null)
        {
            policy = authorizeAttribute.NamedArguments
                .FirstOrDefault(a => a.Key == "Policy").Value.Value?.ToString();
            roles = authorizeAttribute.NamedArguments
                .FirstOrDefault(a => a.Key == "Roles").Value.Value?.ToString();
            authenticationSchemes = authorizeAttribute.NamedArguments
                .FirstOrDefault(a => a.Key == "AuthenticationSchemes").Value.Value?.ToString();
        }

        // Extract XML documentation comments
        var summary = XmlDocumentationHelper.GetXmlDocumentationComment(methodSymbol, "summary");
        var description = XmlDocumentationHelper.GetXmlDocumentationComment(methodSymbol, "remarks");
        var tags = XmlDocumentationHelper.GetXmlDocumentationTags(methodSymbol);
        var isDeprecated = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name.Contains("Obsolete") == true);

        // Get group name from IMinimalEndpoint implementation
        var groupName = methodSymbol.ContainingType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "EndpointGroupNameAttribute")?
            .ConstructorArguments.FirstOrDefault().Value?.ToString() ?? methodSymbol.ContainingType.Name;

        // Extract produces/consumes from attributes
        var produces = ExtractProducesFromAttribute(methodSymbol, "ProducesAttribute");
        var consumes = ExtractStringArrayFromAttribute(methodSymbol, "ConsumesAttribute");

        // Extract response descriptions from XML documentation
        var responseDescriptions = XmlDocumentationHelper.GetResponseDescriptions(methodSymbol);

        // Extract response descriptions from attributes and merge
        var responseAttributes = methodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ResponseDescriptionAttribute");

        foreach (var attr in responseAttributes)
        {
            if (attr.ConstructorArguments.Length == 2)
            {
                if (attr.ConstructorArguments[0].Value is int statusCode)
                {
                    var desc = attr.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                    responseDescriptions[statusCode] = desc;
                }
            }
        }

        // Extract parameter descriptions from XML documentation
        var parameterDescriptions = XmlDocumentationHelper.GetParameterDescriptions(methodSymbol);

        // Check for Configure method
        var hasConfigureMethod = methodSymbol.ContainingType.GetMembers("Configure")
            .OfType<IMethodSymbol>()
            .Any(m => m.IsStatic &&
                      m.DeclaredAccessibility == Accessibility.Public &&
                      m.Parameters.Length == 1 &&
                      m.Parameters[0].Type.Name == "RouteHandlerBuilder");

        // Extract endpoint filters
        var endpointFilters = new List<string>();
        endpointFilters.AddRange(GetEndpointFilters(methodSymbol.ContainingType));
        endpointFilters.AddRange(GetEndpointFilters(methodSymbol));

        // Auto-detect multipart/form-data
        var hasFormFile = parameters.Any(p => p.Type.Contains("IFormFile"));
        if (hasFormFile && !consumes.Any())
        {
            consumes.Add("multipart/form-data");
        }

        return new EndpointMethod
        {
            ClassNamespace = methodSymbol.ContainingType.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty,
            ClassName = methodSymbol.ContainingType.Name,
            MethodName = methodSymbol.Name,
            HttpMethod = httpMethod,
            Route = fullRoute,
            Parameters = parameters,
            ReturnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ReturnTypeInner = returnTypeInner,
            HasAuthorize = hasAuthorize,
            HasAllowAnonymous = hasAllowAnonymous,
            Policy = policy,
            Roles = roles,
            AuthenticationSchemes = authenticationSchemes,
            Summary = summary,
            Description = description,
            Tags = tags,
            IsDeprecated = isDeprecated,
            GroupName = groupName,
            Produces = produces,
            Consumes = consumes,
            ResponseDescriptions = responseDescriptions,
            ParameterDescriptions = parameterDescriptions,
            HasConfigureMethod = hasConfigureMethod,
            EndpointFilters = endpointFilters
        };
    }

    private static List<string> GetEndpointFilters(ISymbol symbol)
    {
        var filters = new List<string>();
        var attributes = symbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "EndpointFilterAttribute");

        foreach (var attr in attributes)
        {
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol)
            {
                filters.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }
        return filters;
    }

    public static string GetBaseRoute(INamedTypeSymbol classSymbol)
    {
        var minimalEndpointsAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name.Contains("MinimalEndpoints") == true);

        if (minimalEndpointsAttribute?.ConstructorArguments.Length > 0)
        {
            return minimalEndpointsAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string GetRouteFromAttribute(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length > 0)
        {
            return attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        }
        return string.Empty;
    }

    private static List<ProducesInfo> ExtractProducesFromAttribute(IMethodSymbol methodSymbol, string attributeName)
    {
        var result = new List<ProducesInfo>();
        var attributes = methodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == attributeName);

        foreach (var attribute in attributes)
        {
            var info = new ProducesInfo { StatusCode = 200 };

            // Check constructor args for content types
            if (attribute.ConstructorArguments.Length > 0)
            {
                var args = attribute.ConstructorArguments[0];
                if (args.Kind == TypedConstantKind.Array)
                {
                    info.ContentTypes.AddRange(args.Values.Select(v => v.Value?.ToString() ?? string.Empty));
                }
                else if (args.Value != null)
                {
                    info.ContentTypes.Add(args.Value.ToString());
                }
            }

            // Check named args for StatusCode
            var statusCodeArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "StatusCode");
            if (statusCodeArg.Key != null && statusCodeArg.Value.Value is int code)
            {
                info.StatusCode = code;
            }

            result.Add(info);
        }

        return result;
    }

    private static List<string> ExtractStringArrayFromAttribute(IMethodSymbol methodSymbol, string attributeName)
    {
        var result = new List<string>();
        var attribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

        if (attribute != null)
        {
            if (attribute.ConstructorArguments.Length > 0)
            {
                var args = attribute.ConstructorArguments[0];
                if (args.Kind == TypedConstantKind.Array)
                {
                    result.AddRange(args.Values.Select(v => v.Value?.ToString() ?? string.Empty));
                }
                else if (args.Value != null)
                {
                    result.Add(args.Value.ToString());
                }
            }
        }

        return result;
    }

    private static string CombineRoutes(string baseRoute, string methodRoute)
    {
        if (string.IsNullOrEmpty(baseRoute))
            return methodRoute;

        if (string.IsNullOrEmpty(methodRoute))
            return baseRoute;

        // If method route is absolute (starts with /), ignore base route
        if (methodRoute.StartsWith("/"))
            return methodRoute;

        baseRoute = baseRoute.Trim('/');
        methodRoute = methodRoute.Trim('/');

        return $"{baseRoute}/{methodRoute}";
    }
}
