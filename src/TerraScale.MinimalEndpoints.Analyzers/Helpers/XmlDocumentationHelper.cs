using System.Xml;
using Microsoft.CodeAnalysis;

namespace TerraScale.MinimalEndpoints.Analyzers.Helpers;

internal static class XmlDocumentationHelper
{
    public static string? GetXmlDocumentationComment(ISymbol symbol, string tagName)
    {
        var xmlComment = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(xmlComment))
            return null;

        var doc = new XmlDocument();
        doc.LoadXml($"<root>{xmlComment}</root>");

        var node = doc.SelectSingleNode($"//{tagName}");
        return node?.InnerText?.Trim();
    }

    public static List<string> GetXmlDocumentationTags(ISymbol symbol)
    {
        var tags = new List<string>();
        var xmlComment = symbol.GetDocumentationCommentXml();

        if (string.IsNullOrEmpty(xmlComment))
            return tags;

        var doc = new XmlDocument();
        doc.LoadXml($"<root>{xmlComment}</root>");

        var tagNodes = doc.SelectNodes("//tag");
        foreach (XmlNode? tagNode in tagNodes)
        {
            if (tagNode?.InnerText != null)
            {
                tags.Add(tagNode.InnerText.Trim());
            }
        }

        return tags;
    }

    public static Dictionary<int, string> GetResponseDescriptions(ISymbol symbol)
    {
        var responses = new Dictionary<int, string>();
        var xmlComment = symbol.GetDocumentationCommentXml();

        if (string.IsNullOrEmpty(xmlComment))
            return responses;

        var doc = new XmlDocument();
        doc.LoadXml($"<root>{xmlComment}</root>");

        var responseNodes = doc.SelectNodes("//response");
        foreach (XmlNode? responseNode in responseNodes)
        {
            if (responseNode?.Attributes != null)
            {
                var codeAttr = responseNode.Attributes["code"];
                if (codeAttr != null && int.TryParse(codeAttr.Value, out var code))
                {
                    responses[code] = responseNode.InnerText?.Trim() ?? string.Empty;
                }
            }
        }

        return responses;
    }

    public static Dictionary<string, string> GetParameterDescriptions(ISymbol symbol)
    {
        var descriptions = new Dictionary<string, string>();
        var xmlComment = symbol.GetDocumentationCommentXml();

        if (string.IsNullOrEmpty(xmlComment))
            return descriptions;

        var doc = new XmlDocument();
        doc.LoadXml($"<root>{xmlComment}</root>");

        var paramNodes = doc.SelectNodes("//param");
        foreach (XmlNode? paramNode in paramNodes)
        {
            if (paramNode?.Attributes != null)
            {
                var nameAttr = paramNode.Attributes["name"];
                if (nameAttr != null && !string.IsNullOrEmpty(nameAttr.Value))
                {
                    descriptions[nameAttr.Value] = paramNode.InnerText?.Trim() ?? string.Empty;
                }
            }
        }

        return descriptions;
    }
}
