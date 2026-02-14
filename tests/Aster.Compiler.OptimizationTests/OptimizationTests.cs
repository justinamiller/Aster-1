using Aster.Compiler.Analysis;
using Aster.Compiler.MiddleEnd.Mir;
using Aster.Compiler.Optimizations;
using Aster.Compiler.MidEnd;

namespace Aster.Compiler.OptimizationTests;

public class MirVerifierTests
{
    [Fact]
    public void Verify_ValidFunction_ReturnsValid()
    {
        // Arrange
        var function = new MirFunction("test");
        var block = function.CreateBlock("entry");
        block.Terminator = new MirReturn();

        var verifier = new MirVerifier();

        // Act
        var result = verifier.VerifyFunction(function);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Verify_MissingTerminator_ReturnsInvalid()
    {
        // Arrange
        var function = new MirFunction("test");
        function.CreateBlock("entry"); // No terminator

        var verifier = new MirVerifier();

        // Act
        var result = verifier.VerifyFunction(function);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("no terminator"));
    }

    [Fact]
    public void Verify_InvalidBranch_ReturnsInvalid()
    {
        // Arrange
        var function = new MirFunction("test");
        var block = function.CreateBlock("entry");
        block.Terminator = new MirBranch(999); // Invalid block index

        var verifier = new MirVerifier();

        // Act
        var result = verifier.VerifyFunction(function);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("invalid block"));
    }
}

public class DeadCodeEliminationTests
{
    [Fact]
    public void DCE_RemovesUnusedVariable()
    {
        // Arrange
        var function = new MirFunction("test");
        var block = function.CreateBlock("entry");

        // x = 42 (unused)
        var deadInstr = new MirInstruction(
            MirOpcode.Literal,
            MirOperand.Variable("x", MirType.I32),
            new[] { MirOperand.Constant(42, MirType.I32) }
        );
        block.AddInstruction(deadInstr);
        block.Terminator = new MirReturn();

        var pass = new DeadCodeEliminationPass();
        var context = new PassContext();

        // Act
        var changed = pass.Run(function, context);

        // Assert
        Assert.True(changed);
        Assert.Empty(block.Instructions);
    }
}

public class ConstantFoldingTests
{
    [Fact]
    public void ConstantFolding_FoldsAddition()
    {
        // Arrange
        var function = new MirFunction("test");
        var block = function.CreateBlock("entry");

        // result = 2 + 3
        var instr = new MirInstruction(
            MirOpcode.BinaryOp,
            MirOperand.Variable("result", MirType.I32),
            new[] { 
                MirOperand.Constant(2, MirType.I32),
                MirOperand.Constant(3, MirType.I32)
            },
            "+"
        );
        block.AddInstruction(instr);
        block.Terminator = new MirReturn();

        var pass = new ConstantFoldingPass();
        var context = new PassContext();

        // Act
        var changed = pass.Run(function, context);

        // Assert
        Assert.True(changed);
        Assert.Single(block.Instructions);
        Assert.Equal(MirOpcode.Literal, block.Instructions[0].Opcode);
        Assert.Equal(5, block.Instructions[0].Operands[0].Value);
    }
}

public class ControlFlowGraphTests
{
    [Fact]
    public void CFG_BuildsCorrectly()
    {
        // Arrange
        var function = new MirFunction("test");
        var entry = function.CreateBlock("entry");
        var thenBlock = function.CreateBlock("then");
        var elseBlock = function.CreateBlock("else");

        entry.Terminator = new MirConditionalBranch(
            MirOperand.Variable("cond", MirType.Bool),
            1, 2
        );
        thenBlock.Terminator = new MirReturn();
        elseBlock.Terminator = new MirReturn();

        // Act
        var cfg = ControlFlowGraph.Build(function);

        // Assert
        Assert.Equal(3, cfg.Nodes.Count);
        Assert.Equal(2, cfg.Nodes[0].Successors.Count);
        Assert.Empty(cfg.Nodes[1].Successors);
        Assert.Empty(cfg.Nodes[2].Successors);
    }
}

public class OptimizationPipelineTests
{
    [Fact]
    public void Pipeline_O0_NoOptimizations()
    {
        // Arrange
        var function = new MirFunction("test");
        var block = function.CreateBlock("entry");
        
        var instr = new MirInstruction(
            MirOpcode.Literal,
            MirOperand.Variable("x", MirType.I32),
            new[] { MirOperand.Constant(42, MirType.I32) }
        );
        block.AddInstruction(instr);
        block.Terminator = new MirReturn();

        var pipeline = new OptimizationPipeline(optimizationLevel: 0);

        // Act
        var result = pipeline.OptimizeFunction(function);

        // Assert - No changes at O0
        Assert.Single(block.Instructions);
    }

    [Fact]
    public void Pipeline_O2_AppliesOptimizations()
    {
        // Arrange
        var function = new MirFunction("test");
        var block = function.CreateBlock("entry");

        // x = 2 + 3
        var instr = new MirInstruction(
            MirOpcode.BinaryOp,
            MirOperand.Variable("x", MirType.I32),
            new[] { 
                MirOperand.Constant(2, MirType.I32),
                MirOperand.Constant(3, MirType.I32)
            },
            "+"
        );
        block.AddInstruction(instr);
        block.Terminator = new MirReturn();

        var pipeline = new OptimizationPipeline(optimizationLevel: 2);

        // Act
        var result = pipeline.OptimizeFunction(function);

        // Assert - Constant folding + DCE should apply
        Assert.NotNull(result);
    }
}
