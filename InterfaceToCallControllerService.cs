using System.Text.RegularExpressions;

var files = Directory.EnumerateFiles(
    "C:\\Users\\romad\\source\\repos\\poweredprojectsblazor\\P3.Domain\\Services\\Abstraction");

var interfaceRegex = new Regex(@"public interface (\w+)\s*\{([^}]*)\}", RegexOptions.Multiline);
var methodRegex = new Regex(@"\s*(\S+)?\s+(\w+)\(([^)]*)\)(\s*where\s+\w+\s*:\s*\w+)?");

files.ToList().ForEach(file =>
{
    var interfaceCode = File.ReadAllText(file);

    var interfaceMatch = interfaceRegex.Match(interfaceCode);

    if (string.IsNullOrWhiteSpace(interfaceMatch.Groups[1].Value))
    {
        return;
    }

    var interfaceName = interfaceMatch.Groups[1].Value;
    var className = interfaceName.Remove(0, 1);
    var serviceName = interfaceMatch.Groups[1].Value.Remove(0, 1);

    var methodMatches = methodRegex.Matches(interfaceMatch.Groups[2].Value);

    var firstPartOfRoute
        = $"\"api/v2/{new Regex("[A-Z]").Replace(serviceName.Replace("Service", string.Empty), m => "-" + m.Value.ToLower()).Remove(0, 1)}s\"";

    // Generate the ASP.NET Controller code
    var serviceCode = $@"using System.Text;
using Newtonsoft.Json;
{string.Join('\n', interfaceCode.Split('\n').Where(l => l.Contains("using") && l.Contains("P3.")).Select(l => l.Trim()).ToList())}
using P3.UI.Connector.Services.Abstraction;

namespace P3.UI.Connector.Services.Realization;

internal class {serviceName} : I{serviceName}
{{
    private readonly HttpClient _httpClient;

    public {serviceName}(
        HttpClient httpClient
    ) => _httpClient = httpClient;
";

    foreach (Match methodMatch in methodMatches)
    {
        var returnType = methodMatch.Groups[1].Value;
        var methodName = methodMatch.Groups[2].Value;
        var parameters = methodMatch.Groups[3].Value;

        var passedParameters
            = "\n                "
            + string.Join(",\n                ",
                parameters.Split(',').Select(p => string.IsNullOrWhiteSpace(p) ? string.Empty : p.Trim().Split(' ')[1])
                    .ToList())
            + "\n          ";

        var isGet = parameters
            .Split(',')
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => string.IsNullOrWhiteSpace(p) ? string.Empty : p.Trim().Split(' ')[0])
            .ToList()
            .All(p => p is "string"
                or "int"
                or "long"
                or "bool"
                or "Guid"
                or "DateTime"
                or "decimal"
                or "double"
                or "float"
                or "byte"
                or "short"
                or "char"
                or "uint"
                or "ulong"
                or "ushort"
                or "sbyte"
                or "string?"
                or "int?"
                or "long?"
                or "bool?"
                or "Guid?"
                or "DateTime?"
                or "decimal?"
                or "double?"
                or "float?"
                or "byte?"
                or "short?"
                or "char?"
                or "uint?"
                or "ulong?"
                or "ushort?"
                or "sbyte?"
                or "CancelationToken");

        var controllerParameters = "\n        "
            + string.Join(",\n        ",
                parameters.Split(',').Select(p => string.IsNullOrWhiteSpace(p)
                        ? string.Empty
                        : p.Trim().Split(' ')[0] is "string"
                            or "int"
                            or "long"
                            or "bool"
                            or "Guid"
                            or "DateTime"
                            or "decimal"
                            or "double"
                            or "float"
                            or "byte"
                            or "short"
                            or "char"
                            or "uint"
                            or "ulong"
                            or "ushort"
                            or "sbyte"
                            or "string?"
                            or "int?"
                            or "long?"
                            or "bool?"
                            or "Guid?"
                            or "DateTime?"
                            or "decimal?"
                            or "double?"
                            or "float?"
                            or "byte?"
                            or "short?"
                            or "char?"
                            or "uint?"
                            or "ulong?"
                            or "ushort?"
                            or "sbyte?"
                            ? $"[FromQuery] {p.Trim()}"
                            : p.Contains("CancellationToken")
                                ? p.Trim()
                                : $"[FromBody] {p.Trim()}"
                    )
                    .ToList())
            + "\n    ";

        var isPost = controllerParameters.Contains("[FromBody]");

        var httpMethod
            = $"{(methodName.ToLower().Contains("update") ? "HttpPut" : isPost ? "HttpPost" : methodName.ToLower().Contains("delete") || methodName.ToLower().Contains("remove") ? "HttpDelete" : "HttpGet")}";

        var secondPartOfRoute
            = $"\"{new Regex("[A-Z]").Replace(methodName, m => "-" + m.Value.ToLower()).Remove(0, 1)}\"";

        var postParameter = controllerParameters.Contains("[FromBody]")
            ? '('
            + string.Join(", ",
                controllerParameters.Split('\n').Where(p => p.Contains("[FromBody]"))
                    .Select(p => string.Join(' ',
                        (p.Contains("=") ? p.Remove(p.IndexOf("=", StringComparison.Ordinal)) : p)
                        .Replace("[FromBody] ", string.Empty).Trim()
                        .Split(' ').Select(s => s.Trim())
                        .Select(s => s[0].ToString().ToUpper() + s[1..]))))
            + ')'
            + " body = ("
            + string.Join(", ",
                controllerParameters.Split('\n').Where(p => p.Contains("[FromBody]"))
                    .Select(p => p.Replace("[FromBody] ", string.Empty).Trim().Split(' ')[1]))
            + ')'
            : null;

        if (controllerParameters.Split("[FromBody]").Length == 2)
        {
            postParameter
                = $"var body = {controllerParameters.Split('\n').Where(p => p.Contains("[FromBody]")).Select(p => p.Replace("[FromBody] ", string.Empty).Trim().Split(' ')[1]).First()}";
        }

        postParameter = postParameter?.Replace(",,", ",");

        // Generate the Controller action method code
        serviceCode += $@"
    public {(returnType.Contains("Task") ? "async " : string.Empty)}{returnType} {methodName}({controllerParameters.Replace("[FromQuery] ", string.Empty).Replace("[FromBody] ", string.Empty)})
    {{
        {(postParameter is not null ? (postParameter.Last() == ',' ? postParameter.Remove(postParameter.Length - 1) : postParameter) + ";\n\n        " : string.Empty)}var response = await _httpClient
            .{httpMethod.Replace("Http", string.Empty)}Async(
                {(firstPartOfRoute + '/' + secondPartOfRoute).Replace("-async", string.Empty).Replace("\"/\"", "/")}{(postParameter is not null ? ",\n                new StringContent(\n                    JsonConvert.SerializeObject(\n                        body\n                    )\n                )" : string.Empty)}
            );

        response.EnsureSuccessStatusCode();{(returnType is not "Task" && returnType is not "void" ? @$"

        var stringResult = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = JsonConvert.DeserializeObject<{(returnType.Contains("Task<") ? returnType.Remove(returnType.LastIndexOf(">", StringComparison.Ordinal)).Replace("Task<", string.Empty) : returnType)}>(stringResult);

        return result;" : string.Empty)}
    }}
";
    }

    serviceCode += "}\n";

    File.WriteAllText(
        $"D:\\NewServices\\{serviceName}.cs",
        serviceCode
    );
});
