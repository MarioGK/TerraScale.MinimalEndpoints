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

        var hasBaseType = classDeclaration.BaseList?.Types
            .Any(t => t.Type.ToString().Contains("BaseMinimalApiEndpoint") || 
                      t.Type.ToString().Contains("IMinimalEndpoint")) ?? false;

        return hasMinimalEndpointsAttribute || hasHttpMethodAttributes || hasRouteProperty || hasHttpMethodProperty || hasBaseType;
    }

    public static ClassDeclarationSyntax? GetEndpointClass(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

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

        var hasBaseType = classDeclaration.BaseList?.Types
            .Any(t => t.Type.ToString().Contains("BaseMinimalApiEndpoint") || 
                      t.Type.ToString().Contains("IMinimalEndpoint")) ?? false;

        if (!hasMinimalEndpointsAttribute && !hasHttpMethodAttributes && !hasRouteProperty && !hasHttpMethodProperty && !hasBaseType)
            return null;

        return classDeclaration;
    }

    public static EndpointMethod? AnalyzeEndpointMethod(IMethodSymbol methodSymbol, ClassDeclarationSyntax classSyntax, SemanticModel semanticModel, string? baseRoute, List<Diagnostic> diagnostics)
    {
        var methodSyntax = classSyntax.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(semanticModel.GetDeclaredSymbol(m), methodSymbol));

        if (methodSyntax == null)
            return null;

        string? httpMethod = null;
        var route = string.Empty;

        foreach (var attribute in methodSymbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.Name;
            if (attributeName != null)
            {
                if (attributeName.Contains("HttpGet"))
                {
                    httpMethod = "Get";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
                else if (attributeName.Contains("HttpPost"))
                {
                    httpMethod = "Post";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
                else if (attributeName.Contains("HttpPut"))
                {
                    httpMethod = "Put";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
                else if (attributeName.Contains("HttpDelete"))
                {
                    httpMethod = "Delete";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
                else if (attributeName.Contains("HttpPatch"))
                {
                    httpMethod = "Patch";
                    route = GetRouteFromAttribute(attribute);
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(httpMethod))
        {
            httpMethod = GetHttpMethodFromClass(methodSymbol.ContainingType);
        }

        if (string.IsNullOrEmpty(httpMethod))
            return null;

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

        var returnTypeInner = "void";
        if (methodSymbol.ReturnType is INamedTypeSymbol namedReturnType &&
            namedReturnType.IsGenericType &&
            namedReturnType.Name == "Task")
        {
             var typeArg = namedReturnType.TypeArguments.FirstOrDefault();
             if (typeArg != null)
             {
                 returnTypeInner = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                 if (typeArg.IsReferenceType && returnTypeInner.EndsWith("?"))
                 {
                     returnTypeInner = returnTypeInner.Substring(0, returnTypeInner.Length - 1);
                 }
             }
        }

        string effectiveBaseRoute = string.IsNullOrEmpty(baseRoute) ? string.Empty : baseRoute!;
        var hasExplicitRoute = !string.IsNullOrEmpty(route);

        if (string.IsNullOrEmpty(route))
        {
            var rs = GetBaseRoute(methodSymbol.ContainingType);
            if (rs != null)
            {
                effectiveBaseRoute = rs;
                hasExplicitRoute = true;
            }
        }

        var fullRoute = CombineRoutes(effectiveBaseRoute, route);

        var parameters = new List<EndpointParameter>();
        foreach (var param in methodSymbol.Parameters)
        {
            var typeName = param.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object";
            if (param.NullableAnnotation == NullableAnnotation.Annotated && !typeName.EndsWith("?"))
            {
                typeName += "?";
            }

            var endpointParam = new EndpointParameter
            {
                Name = param.Name,
                Type = typeName,
                IsFromServices = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromServices") == true),
                IsFromBody = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromBody") == true),
                IsFromRoute = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromRoute") == true),
                IsFromQuery = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromQuery") == true),
                IsFromHeader = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromHeader") == true),
                FromHeaderName = param.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name.Contains("FromHeader") == true)?.NamedArguments.FirstOrDefault(a => a.Key == "Name").Value.Value?.ToString(),
                IsFromForm = param.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("FromForm") == true)
            };
            parameters.Add(endpointParam);
        }

        // Fix: Check class level attributes for Authorize/AllowAnonymous
        var hasAuthorize = methodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Authorize") == true)
            || methodSymbol.ContainingType.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Authorize") == true);

        var hasAllowAnonymous = methodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("AllowAnonymous") == true)
            || methodSymbol.ContainingType.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("AllowAnonymous") == true);

        // Prefer method level, then class level
        var authorizeAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name.Contains("Authorize") == true)
            ?? methodSymbol.ContainingType.GetAttributes()
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

        var summary = XmlDocumentationHelper.GetXmlDocumentationComment(methodSymbol, "summary");
        var description = XmlDocumentationHelper.GetXmlDocumentationComment(methodSymbol, "remarks");
        var tags = XmlDocumentationHelper.GetXmlDocumentationTags(methodSymbol);
        var isDeprecated = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name.Contains("Obsolete") == true);

        string? groupName = null;

        string? groupTypeFullName = null;

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
                    groupTypeFullName = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
        }

        if (string.IsNullOrEmpty(groupName))
        {
            var typeName = ExtractTypeNameFromProperty(methodSymbol.ContainingType, new[] { "GroupType" });
            if (!string.IsNullOrEmpty(typeName))
                groupName = typeName;
        }

        if (string.IsNullOrEmpty(groupName))
        {
            groupName = methodSymbol.ContainingType.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "EndpointGroupNameAttribute")?
                .ConstructorArguments.FirstOrDefault().Value?.ToString();
        }

        groupName ??= methodSymbol.ContainingType.Name;

        var produces = ExtractProducesFromAttribute(methodSymbol, "ProducesAttribute");
        var consumes = ExtractStringArrayFromAttribute(methodSymbol, "ConsumesAttribute");

        var responseDescriptions = XmlDocumentationHelper.GetResponseDescriptions(methodSymbol);

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

        var parameterDescriptions = XmlDocumentationHelper.GetParameterDescriptions(methodSymbol);

        var hasConfigureMethod = methodSymbol.ContainingType.GetMembers("Configure")
            .OfType<IMethodSymbol>()
            .Any(m => m.IsStatic &&
                      m.DeclaredAccessibility == Accessibility.Public &&
                      m.Parameters.Length == 1 &&
                      m.Parameters[0].Type.Name == "RouteHandlerBuilder");

        var endpointFilters = new List<string>();
        endpointFilters.AddRange(GetEndpointFilters(methodSymbol.ContainingType));
        endpointFilters.AddRange(GetEndpointFilters(methodSymbol));

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
            HttpMethod = httpMethod ?? string.Empty,
            Route = fullRoute,
            HasExplicitRoute = hasExplicitRoute,
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
            GroupTypeFullName = groupTypeFullName,
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

    public static string? GetBaseRoute(INamedTypeSymbol classSymbol)
    {
        var routeFromProp = ExtractStringFromProperty(classSymbol, new[] { "Route", "BaseRoute" });
        if (routeFromProp != null)
            return routeFromProp;

        var minimalEndpointsAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name.Contains("MinimalEndpoints") == true);

        if (minimalEndpointsAttribute?.ConstructorArguments.Length > 0)
        {
            return minimalEndpointsAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        }

        return null;
    }

    private static string? GetHttpMethodFromClass(INamedTypeSymbol classSymbol)
    {
        foreach (var decl in classSymbol.DeclaringSyntaxReferences)
        {
            var node = decl.GetSyntax();
            if (node is ClassDeclarationSyntax cls)
            {
                foreach (var prop in cls.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (prop.Identifier.Text != "HttpMethod" && prop.Identifier.Text != "Method")
                        continue;

                    if (prop.ExpressionBody?.Expression is MemberAccessExpressionSyntax member)
                    {
                        var name = member.Name.Identifier.Text;
                        var mapped = MapToHttpMethodName(name);
                        if (!string.IsNullOrEmpty(mapped))
                            return mapped;
                    }

                    if (prop.ExpressionBody?.Expression is IdentifierNameSyntax ident)
                    {
                        var mapped = MapToHttpMethodName(ident.Identifier.Text);
                        if (!string.IsNullOrEmpty(mapped))
                            return mapped;
                    }

                    if (prop.ExpressionBody?.Expression is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var val = lit.Token.ValueText.Trim();
                        var mapped = MapToHttpMethodName(val);
                        if (!string.IsNullOrEmpty(mapped))
                            return mapped;
                    }

                    if (prop.Initializer?.Value is MemberAccessExpressionSyntax initMember)
                    {
                        var nm = initMember.Name.Identifier.Text;
                        var mapped = MapToHttpMethodName(nm);
                        if (!string.IsNullOrEmpty(mapped))
                            return mapped;
                    }

                    if (prop.Initializer?.Value is LiteralExpressionSyntax initLit && initLit.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var val = initLit.Token.ValueText.Trim();
                        var mapped = MapToHttpMethodName(val);
                        if (!string.IsNullOrEmpty(mapped))
                            return mapped;
                    }

                    if (prop.AccessorList != null)
                    {
                        var getter = prop.AccessorList.Accessors.FirstOrDefault(a => a.Keyword.IsKind(SyntaxKind.GetKeyword));
                        if (getter != null)
                        {
                            var returnStmt = getter.Body?.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
                            if (returnStmt?.Expression is MemberAccessExpressionSyntax returnMember)
                            {
                                var nm2 = returnMember.Name.Identifier.Text;
                                var mapped = MapToHttpMethodName(nm2);
                                if (!string.IsNullOrEmpty(mapped))
                                    return mapped;
                            }

                            if (returnStmt?.Expression is LiteralExpressionSyntax returnLit && returnLit.IsKind(SyntaxKind.StringLiteralExpression))
                            {
                                var val2 = returnLit.Token.ValueText.Trim();
                                var mapped = MapToHttpMethodName(val2);
                                if (!string.IsNullOrEmpty(mapped))
                                    return mapped;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    private static string? ExtractStringFromProperty(INamedTypeSymbol classSymbol, string[] propertyNames)
    {
        foreach (var decl in classSymbol.DeclaringSyntaxReferences)
        {
            var node = decl.GetSyntax();
            if (node is ClassDeclarationSyntax cls)
            {
                foreach (var prop in cls.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (!propertyNames.Contains(prop.Identifier.Text))
                        continue;

                    if (prop.ExpressionBody?.Expression is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        return lit.Token.ValueText;
                    }

                    if (prop.Initializer?.Value is LiteralExpressionSyntax initLit && initLit.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        return initLit.Token.ValueText;
                    }

                    if (prop.AccessorList != null)
                    {
                        var getter = prop.AccessorList.Accessors.FirstOrDefault(a => a.Keyword.IsKind(SyntaxKind.GetKeyword));
                        if (getter != null)
                        {
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

    private static string? ExtractTypeNameFromProperty(INamedTypeSymbol classSymbol, string[] propertyNames)
    {
        foreach (var decl in classSymbol.DeclaringSyntaxReferences)
        {
            var node = decl.GetSyntax();
            if (node is ClassDeclarationSyntax cls)
            {
                foreach (var prop in cls.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (!propertyNames.Contains(prop.Identifier.Text))
                        continue;

                    if (prop.ExpressionBody?.Expression is TypeOfExpressionSyntax typeOfExpr)
                    {
                        if (typeOfExpr.Type is IdentifierNameSyntax id)
                            return id.Identifier.Text;
                    }

                    if (prop.Initializer?.Value is TypeOfExpressionSyntax initTypeOf)
                    {
                        if (initTypeOf.Type is IdentifierNameSyntax id2)
                            return id2.Identifier.Text;
                    }

                    if (prop.AccessorList != null)
                    {
                        var getter = prop.AccessorList.Accessors.FirstOrDefault(a => a.Keyword.IsKind(SyntaxKind.GetKeyword));
                        if (getter != null)
                        {
                            var returnStmt = getter.Body?.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
                            if (returnStmt?.Expression is TypeOfExpressionSyntax retTypeOf)
                            {
                                if (retTypeOf.Type is IdentifierNameSyntax id3)
                                    return id3.Identifier.Text;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    private static string? GetGroupNameFromGroupType(INamedTypeSymbol? groupType)
    {
        if (groupType == null)
            return null;

        foreach (var decl in groupType.DeclaringSyntaxReferences)
        {
            var node = decl.GetSyntax();
            if (node is ClassDeclarationSyntax cls)
            {
                foreach (var prop in cls.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (prop.Identifier.Text != "Name")
                        continue;

                    if (prop.ExpressionBody?.Expression is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
                        return lit.Token.ValueText;

                    if (prop.Initializer?.Value is LiteralExpressionSyntax initLit && initLit.IsKind(SyntaxKind.StringLiteralExpression))
                        return initLit.Token.ValueText;

                    if (prop.AccessorList != null)
                    {
                        var getter = prop.AccessorList.Accessors.FirstOrDefault(a => a.Keyword.IsKind(SyntaxKind.GetKeyword));
                        if (getter != null)
                        {
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

        return groupType.Name;
    }

    private static string? MapToHttpMethodName(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        switch (token.Trim().ToLowerInvariant())
        {
            case "get":
            case "httpget":
                return "Get";
            case "post":
            case "httppost":
                return "Post";
            case "put":
            case "httpput":
                return "Put";
            case "delete":
            case "httpdelete":
                return "Delete";
            case "patch":
            case "httppatch":
                return "Patch";
            case "head":
                return "Head";
            case "options":
                return "Options";
            case "trace":
                return "Trace";
            case "connect":
                return "Connect";
            default:
                return null;
        }
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

            if (attribute.ConstructorArguments.Length > 0)
            {
                var arg0 = attribute.ConstructorArguments[0];
                if (arg0.Kind == TypedConstantKind.Type && arg0.Value is INamedTypeSymbol typeSymbol)
                {
                    info.ResponseType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
                else if (arg0.Kind == TypedConstantKind.Array)
                {
                    info.ContentTypes.AddRange(arg0.Values.Select(v => v.Value?.ToString() ?? string.Empty));
                }
                else if (arg0.Value != null)
                {
                    info.ContentTypes.Add(arg0.Value.ToString());
                }
            }

            var statusCodeArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "StatusCode");
            if (statusCodeArg.Key != null && statusCodeArg.Value.Value is int code)
            {
                info.StatusCode = code;
            }

            var typeArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "Type");
            if (typeArg.Key != null && typeArg.Value.Value is INamedTypeSymbol namedType)
            {
                info.ResponseType = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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

        if (methodRoute.StartsWith("/"))
            return methodRoute;

        baseRoute = baseRoute.Trim('/');
        methodRoute = methodRoute.Trim('/');

        return $"{baseRoute}/{methodRoute}";
    }
}
