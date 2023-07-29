using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Umbeon.StateContainers.SourceGeneration
{

    [Generator]
    public class StateContainersSourceGenerator : ISourceGenerator
    {
        private const string STATE_CONTAINER_FIELD_ATTRIBUTE_METADATA_NAME = "Umbeon.StateContainers.StateContainerBase+StateContainerFieldAttribute";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass.
            context.RegisterForSyntaxNotifications(() => new StateContainerFieldAttributeSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is StateContainerFieldAttributeSyntaxReceiver receiver))
            {
                return;
            }

            // Find attribute symbol.
            INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName(STATE_CONTAINER_FIELD_ATTRIBUTE_METADATA_NAME);

            // Group fields by class.
            var classFieldGroups = receiver.StateContainerFieldAttributeAnnotatedFields
                .GroupBy<IFieldSymbol, INamedTypeSymbol>(x => x.ContainingType, SymbolEqualityComparer.Default);

            // Generate and add sources for every class.
            foreach (var classFieldGroup in classFieldGroups)
            {
                string classSource = GenerateClassSourcePartial(classFieldGroup.Key, classFieldGroup.ToList(), attributeSymbol);
                context.AddSource($"{classFieldGroup.Key.Name}.g.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private string GenerateClassSourcePartial(INamedTypeSymbol classSymbol, List<IFieldSymbol> fieldSymbols, ISymbol attributeSymbol)
        {
            string classNamespace = classSymbol.ContainingNamespace.ToDisplayString();

            // Begin building source file.
            var source = new StringBuilder();

            // Define namespace.
            source.AppendLine($"namespace {classNamespace} {{");

            // Define class.
            source.AppendLine($"\tpublic partial class {classSymbol.Name}");

            // Open block - Class.
            source.AppendLine("\t{");

            // Generate and add code for each field property.
            foreach (IFieldSymbol fieldSymbol in fieldSymbols)
            {
                var propertyCode = GeneratePropertyCode(fieldSymbol, attributeSymbol);
                source.Append(propertyCode);
            }

            // Close block - Class.
            source.AppendLine(string.Empty);
            source.AppendLine("\t}");

            // Close block - Namespace
            source.AppendLine("}");

            return source.ToString();
        }

        private string GeneratePropertyCode(IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // Get the name and type of the field.
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            string propertyName = fieldName.Trim('_');

            if (propertyName.Length == 0)
            {
                propertyName = "_Prop";
            }

            propertyName = char.ToUpper(propertyName[0]) + propertyName.Substring(1);

            var source = new StringBuilder();

            // Define property.
            source.Append(
$@"
        public {fieldType} {propertyName} 
        {{
            get 
            {{
                return this.{fieldName};
            }}
            set
            {{
                this.{fieldName} = value;
                this.NotifyValueChanged(nameof(this.{propertyName}));
            }}
        }}
");
            return source.ToString();
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class StateContainerFieldAttributeSyntaxReceiver : ISyntaxContextReceiver
        {
            private const string STATE_CONTAINER_FIELD_ATTRIBUTE_CLASS = "Umbeon.StateContainers.StateContainerBase.StateContainerFieldAttribute";

            public List<IFieldSymbol> StateContainerFieldAttributeAnnotatedFields { get; } = new List<IFieldSymbol>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                // Ignore non-field nodes.
                if (!(context.Node is FieldDeclarationSyntax fieldNode))
                {
                    return;
                }

                // Check if the field is annotated with any attribute.
                if (!fieldNode.AttributeLists.Any())
                {
                    return;
                }

                // Get StateContainerFieldAttribute annotated symbol declared by the field
                var fieldSymbols = fieldNode.Declaration.Variables
                    .Select(x => context.SemanticModel.GetDeclaredSymbol(x))
                    .Where(x => x != null
                                && x.GetAttributes()
                                    .Any(y => y.AttributeClass.ToDisplayString() == STATE_CONTAINER_FIELD_ATTRIBUTE_CLASS))
                    .OfType<IFieldSymbol>()
                    .Cast<IFieldSymbol>()
                    .ToList();

                this.StateContainerFieldAttributeAnnotatedFields.AddRange(fieldSymbols);
            }
        }
    }
}