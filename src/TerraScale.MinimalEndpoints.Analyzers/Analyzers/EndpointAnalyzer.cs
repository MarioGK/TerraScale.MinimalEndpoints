using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TerraScale.MinimalEndpoints.Analyzers.Helpers;
using TerraScale.MinimalEndpoints.Analyzers.Models;

namespace TerraScale.MinimalEndpoints.Analyzers.Analyzers;

internal static class EndpointAnalyzer
{
    public static bool IsEndpointClass(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
            return false;

        // An endpoint class can be identified in a few ways:
        // - Implements IMinimalEndpoint (preferred)
        // - Exposes Route or HttpMethod properties (convention)
        // - Still supports the older MinimalEndpoints attribute or method-level Http* attributes

        var hasMinimalEndpointsAttribute = classDeclaration.AttributeLists
            .Any(al => al.Attributes.Any(a => a.Name.ToString().Contains("MinimalEndpoints")));

        var hasHttpMethodAttributes = classDeclaration.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.AttributeLists
                .Any(al => al.Attributes
                    .Any(a => a.Name.ToString().Contains("Http"))));

        var hasRouteProperty = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "Route" || p.Identifier.Text == "BaseRoute");

        var hasHttpMethodProperty = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "HttpMethod" || p.Identifier.Text == "Method");

        return hasMinimalEndpointsAttribute || hasHttpMethodAttributes || hasRouteProperty || hasHttpMethodProperty;
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

        var hasRouteProperty = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "Route" || p.Identifier.Text == "BaseRoute");

        var hasHttpMethodProperty = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "HttpMethod" || p.Identifier.Text == "Method");

        if (!hasMinimalEndpointsAttribute && !hasHttpMethodAttributes && !hasRouteProperty && !hasHttpMethodProperty)
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

        // Determine HTTP method and route. Preference order:
        // 1) Method-level Http* attribute (backwards compatibility)
        // 2) Class-level HttpMethod / Route properties (new convention)

        var httpMethod = string.Empty;
        var route = string.Empty;

        // First attempt to read method-level attribute (backward compatibility)
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

        // If no method-level HTTP attribute was found, try to read class-level HttpMethod property
        if (string.IsNullOrEmpty(httpMethod))
        {
            httpMethod = GetHttpMethodFromClass(methodSymbol.ContainingType) ?? string.Empty;
        }

        // If still no HTTP method is provided, this is not an endpoint we can register
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
                    "MinimalEndpoints",
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

        // If method-level route is empty, attempt to get class-level Route property
        var effectiveBaseRoute = string.IsNullOrEmpty(baseRoute) ? string.Empty : baseRoute;
        if (string.IsNullOrEmpty(route))
        {
            var rs = GetBaseRoute(methodSymbol.ContainingType);
            if (!string.IsNullOrEmpty(rs))
                effectiveBaseRoute = rs;
        }

        // For now, treat route as the effective route; if both exist, combine
        var fullRoute = CombineRoutes(effectiveBaseRoute, route);

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
        // Prefer a GroupName property on the class (IMinimalEndpoint), then fall back to the attribute
        var groupName = ExtractStringFromProperty(methodSymbol.ContainingType, new[] { "GroupName" })
                        ?? methodSymbol.ContainingType.GetAttributes()
                            .FirstOrDefault(a => a.AttributeClass?.Name == "EndpointGroupNameAttribute")?
                            .ConstructorArguments.FirstOrDefault().Value?.ToString()
                        ?? methodSymbol.ContainingType.Name;

        // Extract produces/consumes from attributes
        var produces = ExtractProducesFromAttribute(methodSymbol, "ProducesAttribute");
        var consumes = ExtractStringArrayFromAttribute(methodSymbol, "ConsumesAttribute");

        // Extract response descriptions from XML documentation
        var responseDescriptions = XmlDocumentationHelper.GetResponseDescriptions(methodSymbol);

        // Extract response descriptions from attributes and merge
        var responseAttributes = methodSymbol.GetAttributes()
            // Resolve group from several places (prefer generic base type TGroup, then
            // GroupType property, then EndpointGroupNameAttribute, finally class name).
            string? groupName = null;

            // 1) If endpoint inherits from BaseMinimalApiEndpoint<TGroup>, get TGroup name
            var baseType = methodSymbol.ContainingType.BaseType;
            if (baseType != null && baseType.IsGenericType)
            {
                var genericDef = baseType.OriginalDefinition?.Name ?? string.Empty;
                if (genericDef.Contains("BaseMinimalApiEndpoint"))
                {
                    var typeArg = baseType.TypeArguments.FirstOrDefault();
                    if (typeArg != null)
                    {
                        groupName = GetGroupNameFromGroupType(typeArg as INamedTypeSymbol);
                    }
                }
            }

            // 2) If no generic base group, try class-level GroupType property (typeof(SomeGroup))
            if (string.IsNullOrEmpty(groupName))
            {
                var typeName = ExtractTypeNameFromProperty(methodSymbol.ContainingType, new[] { "GroupType" });
                if (!string.IsNullOrEmpty(typeName))
                {
                    groupName = typeName;
                }
            }

            // 3) Fallback to attribute
            if (string.IsNullOrEmpty(groupName))
            {
                groupName = methodSymbol.ContainingType.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == "EndpointGroupNameAttribute")?
                    .ConstructorArguments.FirstOrDefault().Value?.ToString();
            }

            // 4) Final fallback to class name
            groupName ??= methodSymbol.ContainingType.Name;
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
        // First try to find a property named Route or BaseRoute on the class declaration
        var routeFromProp = ExtractStringFromProperty(classSymbol, new[] { "Route", "BaseRoute" });
        if (!string.IsNullOrEmpty(routeFromProp))
            return routeFromProp ?? string.Empty;

        // Fall back to the legacy MinimalEndpoints attribute
        var minimalEndpointsAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name.Contains("MinimalEndpoints") == true);

        if (minimalEndpointsAttribute?.ConstructorArguments.Length > 0)
        {
            return minimalEndpointsAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string? GetHttpMethodFromClass(INamedTypeSymbol classSymbol)
    {
        var methodFromProp = ExtractStringFromProperty(classSymbol, new[] { "HttpMethod", "Method" });
        if (!string.IsNullOrEmpty(methodFromProp))
            return methodFromProp?.ToUpperInvariant();

        return null;
    }

    private static string? ExtractStringFromProperty(INamedTypeSymbol classSymbol, string[] propertyNames)
    {
        // Loop each declaration syntax and inspect any property declarations
        foreach (var decl in classSymbol.DeclaringSyntaxReferences)
        {
            var node = decl.GetSyntax();
            if (node is ClassDeclarationSyntax cls)
            {
                foreach (var prop in cls.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (!propertyNames.Contains(prop.Identifier.Text))
                        continue;

                    // Expression-bodied property: => "literal"
                    if (prop.ExpressionBody?.Expression is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        return lit.Token.ValueText;
                    }

                    // Auto-property initializer: { get; } = "literal";
                    if (prop.Initializer?.Value is LiteralExpressionSyntax initLit && initLit.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        return initLit.Token.ValueText;
                    }

                    // Getter with return statement
                    if (prop.AccessorList != null)
                    {
                        var getter = prop.AccessorList.Accessors.FirstOrDefault(a => a.Keyword.IsKind(SyntaxKind.GetKeyword));
                        if (getter != null)
                        {
                            // Try to find a return statement returning a string literal
                            var returnStmt = getter.Body?.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
                            if (returnStmt?.Expression is LiteralExpressionSyntax returnLit && returnLit.IsKind(SyntaxKind.StringLiteralExpression))
                            {
                                return returnLit.Token.ValueText;
                            }
                        }
                    }
                }
            }
        }

        return null;
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
