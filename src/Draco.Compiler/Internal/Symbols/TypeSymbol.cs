using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a type definition.
/// </summary>
internal abstract partial class TypeSymbol : Symbol, IMemberSymbol
{
    /// <summary>
    /// True, if this is a type variable, false otherwise.
    /// </summary>
    public virtual bool IsTypeVariable => false;

    /// <summary>
    /// True, if this is a ground type, meaning there are no type variables or all type variables have been substituted.
    /// </summary>
    public virtual bool IsGroundType => !this.IsGenericInstance || this.GenericArguments.All(t => t.IsGroundType);

    /// <summary>
    /// True, if this type is a value-type.
    /// </summary>
    public virtual bool IsValueType => false;

    /// <summary>
    /// True, if this type is an interface.
    /// </summary>
    public virtual bool IsInterface => false;

    /// <summary>
    /// True, if this type is abstract.
    /// </summary>
    public virtual bool IsAbstract => false;

    /// <summary>
    /// True, if this type is sealed.
    /// </summary>
    public virtual bool IsSealed => false;

    /// <summary>
    /// The substituted type of this one, in case this is a type variable.
    /// It's this instance itself, if not a type variable, or not substituted.
    /// </summary>
    public virtual TypeSymbol Substitution => this;

    /// <summary>
    /// The primary base type of this type (typically class inheritance).
    /// </summary>
    public TypeSymbol? BaseType => this.ImmediateBaseTypes.FirstOrDefault(s => !s.IsInterface);

    /// <summary>
    /// The immediate base types of this type.
    /// </summary>
    public virtual ImmutableArray<TypeSymbol> ImmediateBaseTypes => ImmutableArray<TypeSymbol>.Empty;

    /// <summary>
    /// All types that can be considered the base type of this one, including this type itself.
    /// The types are returned in a pre-order manner, starting from this type.
    /// </summary>
    public IEnumerable<TypeSymbol> BaseTypes
    {
        get
        {
            yield return this;
            foreach (var t in this.ImmediateBaseTypes.SelectMany(b => b.BaseTypes)) yield return t;
        }
    }

    /// <summary>
    /// The members defined directly in this type doesn't include members from <see cref="ImmediateBaseTypes"/>.
    /// </summary>
    public virtual IEnumerable<Symbol> DefinedMembers => Enumerable.Empty<Symbol>();

    /// <summary>
    /// The constructors defined directly in this type.
    /// </summary>
    public IEnumerable<FunctionSymbol> Constructors => this.DefinedMembers
        .OfType<FunctionSymbol>()
        .Where(f => f.IsConstructor);

    /// <summary>
    /// All members within this type that are not special members.
    /// </summary>
    public IEnumerable<Symbol> NonSpecialMembers => this.Members.Where(m => !m.IsSpecialName);

    public override sealed IEnumerable<Symbol> Members => InterlockedUtils.InitializeDefault(ref this.members, this.BuildMembers);
    private ImmutableArray<Symbol> members;

    /// <summary>
    /// All property accessors defined by this type.
    /// </summary>
    public IEnumerable<FunctionSymbol> DefinedPropertyAccessors =>
        this.DefinedMembers.OfType<PropertySymbol>().SelectMany(p => p.Accessors);

    // NOTE: We override for covariant return type
    public override TypeSymbol? GenericDefinition => null;

    public bool IsStatic => true;

    public override bool CanBeShadowedBy(Symbol other)
    {
        if (other is not TypeSymbol type) return false;
        if (this.Name != other.Name) return false;
        return this.GenericParameters.Length == other.GenericParameters.Length;
    }

    public T? GetOverriddenSymbol<T>(T @override)
        where T : Symbol, IOverridableSymbol => this.BaseTypes
        .SelectMany(x => x.DefinedMembers)
        .OfType<T>()
        .FirstOrDefault(x => x.CanBeOverriddenBy(@override));

    private ImmutableArray<Symbol> BuildMembers()
    {
        var builder = ImmutableArray.CreateBuilder<Symbol>();
        var ignore = new List<Symbol>();

        // TODO: Maybe just include private members but filter them out during lookup?
        // NOTE: We use this workaround so we don't import interface members that
        // are implemented privately
        var relevantBases = this.IsInterface
            ? this.BaseTypes
            : this.BaseTypes.Where(b => !b.IsInterface);

        foreach (var member in relevantBases.SelectMany(x => x.DefinedMembers))
        {
            // Ignore constructors in base types
            if (member.ContainingSymbol != this && member is FunctionSymbol { IsConstructor: true }) continue;

            if (ignore.Any(member.CanBeShadowedBy)) continue;
            ignore.Add(member);
            builder.Add(member);
            if (member is not IOverridableSymbol overridable) continue;
            if (overridable.Override is not null) ignore.Add(overridable.Override);
        }
        return builder.ToImmutable();
    }

    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (TypeSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context)
    {
        // NOTE: Is this correct? What about nested generics?
        // Is this why .NET projects down generic args?
        if (!this.IsInGenericContext) return this;
        return new TypeInstanceSymbol(containingSymbol, this, context);
    }

    public override Api.Semantics.ITypeSymbol ToApiSymbol() => new Api.Semantics.TypeSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitType(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitType(this);

    public override abstract string ToString();
}
