using System.Collections.Generic;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// An unconditional jump.
/// </summary>
internal sealed class JumpInstruction : InstructionBase
{
    public override bool IsBranch => true;
    public override IEnumerable<BasicBlock> JumpTargets => new[] { this.Target };
    public override IEnumerable<IOperand> Operands => new[] { this.Target };

    /// <summary>
    /// The jump target.
    /// </summary>
    public BasicBlock Target { get; set; }

    public JumpInstruction(BasicBlock target)
    {
        this.Target = target;
    }

    public override JumpInstruction Clone() => new(this.Target);
}
