namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents a builtin type.
/// </summary>
internal sealed class BuiltinType : Type
{
    /// <summary>
    /// The underlying system type.
    /// </summary>
    public System.Type UnderylingType { get; }

    /// <summary>
    /// The name that should be shown.
    /// </summary>
    public string Name { get; }

    public BuiltinType(System.Type underylingType, string name)
    {
        this.UnderylingType = underylingType;
        this.Name = name;
    }

    public override string ToString() => this.Name;
}
