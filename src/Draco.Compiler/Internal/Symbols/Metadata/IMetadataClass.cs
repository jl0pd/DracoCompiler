namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Represents a metadata symbol, that is considered a class in metadata.
/// </summary>
internal interface IMetadataClass : IMetadataSymbol
{
    /// <summary>
    /// The containing symbol of this class.
    /// </summary>
    public Symbol ContainingSymbol { get; }

    /// <summary>
    /// The default member attribute name, which is the name of an indexer property.
    /// </summary>
    public string? DefaultMemberAttributeName { get; }
}
