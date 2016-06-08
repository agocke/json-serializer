using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace JsonSerializer
{
    [SourceGenerator("C#")]
    public class JsonSerializer : SourceGenerator
    {
        public override void Execute(SourceGeneratorContext context)
        {
            var visitor = new SerializationMethodsGenerator(context);
            visitor.Visit(context.Compilation.SourceModule);
        }

        private class SerializationMethodsGenerator : SymbolVisitor
        {
            private readonly SourceGeneratorContext _context;
            private readonly Compilation _comp;
            private readonly INamedTypeSymbol serializableInterface;

            public SerializationMethodsGenerator(SourceGeneratorContext context)
            {
                _context = context;
                _comp = context.Compilation;
                serializableInterface = _comp.GetTypeByMetadataName("JsonSerializerUtil.IJsonSerializable`1");
            }

            public override void VisitNamedType(INamedTypeSymbol type)
            {
                foreach (var t in type.GetTypeMembers())
                {
                    VisitNamedType(t);
                }

                ProcessType(type);
            }

            public override void VisitModule(IModuleSymbol symbol)
            {
                VisitNamespace(symbol.GlobalNamespace);
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (var n in symbol.GetNamespaceMembers())
                {
                    VisitNamespace(n);
                }

                foreach (var t in symbol.GetTypeMembers())
                {
                    VisitNamedType(t);
                }
            }

            private void ProcessType(INamedTypeSymbol type)
            {
                bool fromSerializable = false;
                ITypeSymbol typeArg = null;
                foreach (var iface in type.AllInterfaces)
                {
                    if (iface.ConstructedFrom == serializableInterface)
                    {
                        fromSerializable = true;
                        typeArg = iface.TypeArguments.Single();
                        break;
                    }
                }

                if (fromSerializable)
                {
                    var parseOptions = CSharpParseOptions.Default
                        .WithFeatures(_comp.SyntaxTrees.First().Options.Features);

                    var source = SyntaxFactory.ParseSyntaxTree($@"
{GetUsings()}

namespace {GetAllNamespaces(type)}
{{
    {type.DeclaredAccessibility.ToString().ToLower()} partial class {type.Name}
    {{
        public void Serialize(TextWriter writer)
        {{
            {GetSerializeMethod(type)}
        }}
    }}
}}
", encoding: Encoding.UTF8 , options: parseOptions);
                    var normalized = source.GetRoot().NormalizeWhitespace();
                    source = SyntaxFactory.SyntaxTree(normalized, parseOptions, encoding: Encoding.UTF8);
                    _context.AddCompilationUnit($"{type.Name}.JsonSerializable", source);
                }
            }

            private static string GetSerializeMethod(INamedTypeSymbol type)
            {
                var publicProps = type.GetMembers()
                    .Where(m => m.Kind == SymbolKind.Property
                           && m.DeclaredAccessibility == Accessibility.Public);

                var sb = new StringBuilder();

                sb.AppendLine(GetPrologue());
                sb.AppendLine();

                foreach (IPropertySymbol prop in publicProps)
                {
                    SerializeProperty(prop, sb);
                }

                sb.AppendLine();
                sb.AppendLine(GetEpilogue());

                return sb.ToString();
            }

            private static string GetPrologue() => @"writer.WriteLine(""{"");";
            private static string GetEpilogue() => @"writer.Write('}');";

            private static void SerializeProperty(IPropertySymbol prop, StringBuilder sb)
            {
                var propName = prop.Name;
                const string indent = "    ";

                var propValue = prop.Type.SpecialType == SpecialType.System_String
                    ? $" '\"' + {propName}.ToString() + '\"'"
                    : propName + ".ToString()";

                sb.AppendLine($"// {propName}");
                sb.AppendLine($"writer.Write(\"{indent}\");");
                sb.AppendLine($"writer.Write('\"' + nameof({propName}) + '\"');");
                // Hack: just rely on tostring to know what to do
                sb.AppendLine($"writer.WriteLine(\": \" + {propValue} + \",\");");
            }

            private static string GetAllNamespaces(INamedTypeSymbol type)
            {
                var sb = new StringBuilder();
                for (var ns = type.ContainingNamespace;
                     !ns.IsGlobalNamespace;
                     ns = ns.ContainingNamespace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Insert(0, '.');
                    }
                    sb.Insert(0, ns.ToString());
                }

                return sb.ToString();
            }

            private static string GetUsings() => @"using System.IO;";
        }
    }
}
