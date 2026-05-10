using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: CcAnalyzer <src-root> [output.txt]");
    return 1;
}

var rootDir = Path.GetFullPath(args[0]);
var outPath = args.Length > 1 ? Path.GetFullPath(args[1]) : null;

var files = Directory.EnumerateFiles(rootDir, "*.cs", SearchOption.AllDirectories)
    .Where(p => p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false
                && p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
    .ToList();

var rows = new List<(string File, string Type, string Member, int Cc)>();
foreach (var path in files)
{
    var text = File.ReadAllText(path);
    var tree = CSharpSyntaxTree.ParseText(text, path: path);
    var modelRoot = tree.GetRoot();
    foreach (var node in modelRoot.DescendantNodes())
    {
        switch (node)
        {
            case MethodDeclarationSyntax m:
                rows.Add((Rel(path), ContainingTypeName(m), m.Identifier.Text + m.ParameterList.ToString(), Cyclomatic(m)));
                break;
            case ConstructorDeclarationSyntax c:
                rows.Add((Rel(path), ContainingTypeName(c), c.Identifier.Text + c.ParameterList.ToString(), Cyclomatic(c)));
                break;
            case AccessorDeclarationSyntax { Body: not null } a:
                rows.Add((Rel(path), ContainingAccessor(a), a.Keyword.Text + " accessor", Cyclomatic(a)));
                break;
        }
    }
}

var sb = new StringBuilder();
sb.AppendLine("file,type,member,cyclomatic_complexity");
foreach (var (file, type, member, cc) in rows.OrderByDescending(r => r.Cc).ThenBy(r => r.File).ThenBy(r => r.Member))
{
    var safeMember = member.Replace('\r', ' ').Replace('\n', ' ').Replace('"', '\'');
    sb.AppendLine($"\"{file}\",\"{type}\",\"{safeMember}\",{cc}");
}

var report = sb.ToString();
if (outPath is not null)
    File.WriteAllText(outPath, report);
Console.Write(report);

Console.Error.WriteLine($"Analyzed {files.Count} files, {rows.Count} methods/accessors.");
return 0;

string Rel(string full) => Path.GetRelativePath(rootDir, full).Replace('\\', '/');

static string ContainingTypeName(SyntaxNode methodLike) =>
    (methodLike.Parent as ClassDeclarationSyntax)?.Identifier.Text
    ?? (methodLike.Parent as StructDeclarationSyntax)?.Identifier.Text
    ?? (methodLike.Parent as RecordDeclarationSyntax)?.Identifier.Text
    ?? "?";

static string ContainingAccessor(AccessorDeclarationSyntax a)
{
    var p = a.Parent?.Parent;
    return p switch
    {
        PropertyDeclarationSyntax prop => prop.Identifier.Text,
        IndexerDeclarationSyntax idx => "this[]",
        _ => "?"
    };
}

static int Cyclomatic(SyntaxNode bodyContainer)
{
    var body = bodyContainer switch
    {
        MethodDeclarationSyntax m => (SyntaxNode?)m.Body ?? m.ExpressionBody,
        ConstructorDeclarationSyntax ctor => ctor.Body ?? (SyntaxNode?)ctor.ExpressionBody,
        AccessorDeclarationSyntax a => a.Body ?? (SyntaxNode?)a.ExpressionBody,
        _ => null
    };
    if (body is null)
        return 1;

    var complexity = 1;
    foreach (var node in body.DescendantNodes(n => true))
    {
        switch (node)
        {
            case IfStatementSyntax:
            case ForStatementSyntax:
            case ForEachStatementSyntax:
            case WhileStatementSyntax:
            case DoStatementSyntax:
            case CaseSwitchLabelSyntax:
            case CatchClauseSyntax:
            case ConditionalExpressionSyntax:
                complexity++;
                break;
            case BinaryExpressionSyntax be
                when be.IsKind(SyntaxKind.LogicalAndExpression) || be.IsKind(SyntaxKind.LogicalOrExpression):
                complexity++;
                break;
        }
    }

    return complexity;
}
