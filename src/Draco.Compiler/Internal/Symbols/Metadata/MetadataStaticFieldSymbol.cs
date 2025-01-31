using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Static fields read from metadata.
/// </summary>
internal sealed class MetadataStaticFieldSymbol : GlobalSymbol, IMetadataSymbol
{
    public override TypeSymbol Type => InterlockedUtils.InitializeNull(ref this.type, this.BuildType);
    private TypeSymbol? type;

    public override bool IsMutable => !(this.fieldDefinition.Attributes.HasFlag(FieldAttributes.Literal) || this.fieldDefinition.Attributes.HasFlag(FieldAttributes.InitOnly));

    public override string Name => this.MetadataReader.GetString(this.fieldDefinition.Name);

    public override Api.Semantics.Visibility Visibility
    {
        get
        {
            // If this is an interface member, default to public
            if (this.ContainingSymbol is TypeSymbol { IsInterface: true })
            {
                return Api.Semantics.Visibility.Public;
            }

            // Otherwise read flag from metadata
            return this.fieldDefinition.Attributes.HasFlag(FieldAttributes.Public)
                ? Api.Semantics.Visibility.Public
                : Api.Semantics.Visibility.Internal;
        }
    }

    public override SymbolDocumentation Documentation => InterlockedUtils.InitializeNull(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => InterlockedUtils.InitializeNull(ref this.rawDocumentation, this.BuildRawDocumentation);
    private string? rawDocumentation;

    public override Symbol? ContainingSymbol { get; }

    /// <summary>
    /// The metadata assembly of this metadata symbol.
    /// </summary>
    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    /// <summary>
    /// The metadata reader that was used to read up this metadata symbol.
    /// </summary>
    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    /// <summary>
    /// True, if this is a literal that cannot be referenced as a field, but needs to be inlined as a value.
    /// This is the case for enum members.
    /// </summary>
    public bool IsLiteral => this.fieldDefinition.Attributes.HasFlag(FieldAttributes.Literal);

    /// <summary>
    /// The default value of this field.
    /// </summary>
    public object? DefaultValue => InterlockedUtils.InitializeMaybeNull(ref this.defaultValue, this.BuildDefaultValue);
    private object? defaultValue;

    private readonly FieldDefinition fieldDefinition;

    public MetadataStaticFieldSymbol(Symbol containingSymbol, FieldDefinition fieldDefinition)
    {
        if (!fieldDefinition.Attributes.HasFlag(FieldAttributes.Static))
        {
            throw new System.ArgumentException("globals must be constructed from static fields");
        }

        this.ContainingSymbol = containingSymbol;
        this.fieldDefinition = fieldDefinition;
    }

    private TypeSymbol BuildType() =>
        this.fieldDefinition.DecodeSignature(this.Assembly.Compilation.TypeProvider, this);

    private object? BuildDefaultValue()
    {
        var constantHandle = this.fieldDefinition.GetDefaultValue();
        if (constantHandle.IsNil) return null;

        var constant = this.MetadataReader.GetConstant(constantHandle);
        return MetadataSymbol.DecodeConstant(constant, this.MetadataReader);
    }

    private SymbolDocumentation BuildDocumentation() =>
        XmlDocumentationExtractor.Extract(this);

    private string BuildRawDocumentation() =>
        MetadataSymbol.GetDocumentation(this);
}
