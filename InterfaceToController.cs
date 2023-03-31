using System.Text.RegularExpressions;

var files = Directory.EnumerateFiles(
    "C:\\Users\\romad\\source\\repos\\poweredprojectsblazor\\P3.Domain\\Services\\Abstraction");

var interfaceRegex = new Regex(@"public interface (\w+)\s*\{([^}]*)\}", RegexOptions.Multiline);
var methodRegex = new Regex(@"\s*(\w+\<[^\>]+\>)?\s+(\w+)\(([^)]*)\)(\s*where\s+\w+\s*:\s*\w+)?");

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
    var controllerName = interfaceMatch.Groups[1].Value.Remove(0, 1).Replace("Service", "Controller");

    var methodMatches = methodRegex.Matches(interfaceMatch.Groups[2].Value);

    // Generate the ASP.NET Controller code
    var controllerCode = $@"using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using P3.Domain.Services.Abstraction;

namespace P3.WebApi.Controllers.V2;

[ApiController]
[Route(""api/v2/{new Regex("[A-Z]").Replace(controllerName.Replace("Controller", string.Empty), m => "-" + m.Value.ToLower()).Remove(0, 1)}s"")]
public class {controllerName} : ControllerBase
{{
    private readonly {interfaceName} _{className[0].ToString().ToLower() + className[1..]};

    public {controllerName}(
        {interfaceName} {className[0].ToString().ToLower() + className[1..]}
    ) => _{className[0].ToString().ToLower() + className[1..]} = {className[0].ToString().ToLower() + className[1..]};
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
                or "CancellationToken");

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

        // Generate the Controller action method code
        controllerCode += $@"
    [{(methodName.ToLower().Contains("update") ? "HttpPut" : isPost ? "HttpPost" : methodName.ToLower().Contains("delete") || methodName.ToLower().Contains("remove") ? "HttpDelete" : "HttpGet")}(""{new Regex("[A-Z]").Replace(methodName, m => "-" + m.Value.ToLower()).Remove(0, 1)}"")]
    public {(returnType.Contains("Task") ? "async " : string.Empty)}{(returnType.Contains("Task") ? "Task<IActionResult>" : "IActionResult")} {methodName}({controllerParameters}){(returnType is "void" or "Task" ? @$"
    {{
        {(returnType.Contains("Task") ? "await " : string.Empty)}_{className[0].ToString().ToLower() + className[1..]}.{methodName}({(string.IsNullOrWhiteSpace(passedParameters) ? string.Empty : passedParameters)});
        
        return Ok();
    }}" : @$" => Ok(
            {(returnType.Contains("Task") ? "await " : string.Empty)}_{className[0].ToString().ToLower() + className[1..]}.{methodName}({(string.IsNullOrWhiteSpace(passedParameters) ? string.Empty : passedParameters)})
        );")}
";
    }

    controllerCode += "}\n";

    File.WriteAllText(
        $"D:\\NewControllers\\{controllerName}.cs",
        controllerCode
    );
});
