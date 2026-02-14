using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.PatternLowering;

/// <summary>
/// Lowers pattern matching constructs into explicit branch instructions.
/// Converts MIR switch terminators into conditional branch chains.
/// </summary>
public sealed class PatternLower
{
    /// <summary>Lower pattern matches in a MIR module.</summary>
    public void Lower(MirModule module)
    {
        foreach (var fn in module.Functions)
        {
            LowerFunction(fn);
        }
    }

    private void LowerFunction(MirFunction fn)
    {
        // Pattern lowering converts Switch terminators into conditional branches
        for (int i = 0; i < fn.BasicBlocks.Count; i++)
        {
            var block = fn.BasicBlocks[i];
            if (block.Terminator is MirSwitch sw)
            {
                LowerSwitch(fn, block, sw);
            }
        }
    }

    private void LowerSwitch(MirFunction fn, MirBasicBlock block, MirSwitch sw)
    {
        if (sw.Cases.Count == 0)
        {
            block.Terminator = new MirBranch(sw.DefaultBlock);
            return;
        }

        // Convert switch into a chain of conditional branches
        var currentBlock = block;
        for (int i = 0; i < sw.Cases.Count; i++)
        {
            var (value, targetBlock) = sw.Cases[i];
            var caseConst = MirOperand.Constant(value, sw.Scrutinee.Type);
            var condTemp = MirOperand.Temp($"_sw{i}", MirType.Bool);

            currentBlock.Instructions.Add(new MirInstruction(
                MirOpcode.BinaryOp,
                condTemp,
                new[] { sw.Scrutinee, caseConst },
                Frontend.Ast.BinaryOperator.Eq));

            if (i == sw.Cases.Count - 1)
            {
                // Last case: either match or default
                currentBlock.Terminator = new MirConditionalBranch(condTemp, targetBlock, sw.DefaultBlock);
            }
            else
            {
                var nextCheckBlock = fn.CreateBlock($"switch.check{i + 1}");
                currentBlock.Terminator = new MirConditionalBranch(condTemp, targetBlock, nextCheckBlock.Index);
                currentBlock = nextCheckBlock;
            }
        }
    }
}
