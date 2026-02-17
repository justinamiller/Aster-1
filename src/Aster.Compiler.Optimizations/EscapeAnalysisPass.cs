using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Optimizations;

/// <summary>
/// Escape Analysis pass.
/// Determines if objects can be stack-allocated instead of heap-allocated.
/// Objects that don't escape their function can be allocated on the stack.
/// </summary>
public sealed class EscapeAnalysisPass : IOptimizationPass
{
    public string Name => "EscapeAnalysis";

    public bool Run(MirFunction function, PassContext context)
    {
        context.Metrics.StartTiming();

        bool changed = false;
        var escapeInfo = AnalyzeEscapes(function);

        // Mark allocations that can be stack-allocated
        foreach (var block in function.BasicBlocks)
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instr = block.Instructions[i];

                // Look for heap allocations
                if (IsHeapAllocation(instr) && instr.Destination != null)
                {
                    var varName = instr.Destination.Name;
                    
                    if (!escapeInfo.Escapes(varName))
                    {
                        // Can convert to stack allocation
                        // Mark with metadata (actual implementation would modify the instruction)
                        changed = true;
                        context.Metrics.InstructionsRemoved++;
                    }
                }
            }
        }

        context.Metrics.StopTiming();
        return changed;
    }

    private EscapeInfo AnalyzeEscapes(MirFunction function)
    {
        var info = new EscapeInfo();

        foreach (var block in function.BasicBlocks)
        {
            foreach (var instr in block.Instructions)
            {
                // Check for operations that cause escapes
                if (instr.Opcode == MirOpcode.Store && instr.Destination != null)
                {
                    // Storing to a variable might cause it to escape
                    // (depends on what it's stored to)
                    foreach (var operand in instr.Operands)
                    {
                        if (operand.Kind == MirOperandKind.Variable)
                        {
                            info.MarkEscape(operand.Name);
                        }
                    }
                }
                else if (instr.Opcode == MirOpcode.Call)
                {
                    // Passing to a function causes escape (conservative)
                    foreach (var operand in instr.Operands.Skip(1)) // Skip function ref
                    {
                        if (operand.Kind == MirOperandKind.Variable)
                        {
                            info.MarkEscape(operand.Name);
                        }
                    }
                }
            }

            // Returning a value causes it to escape
            if (block.Terminator is MirReturn ret && ret.Value != null)
            {
                if (ret.Value.Kind == MirOperandKind.Variable)
                {
                    info.MarkEscape(ret.Value.Name);
                }
            }
        }

        return info;
    }

    private bool IsHeapAllocation(MirInstruction instr)
    {
        // Detect heap allocation patterns in MIR:
        // 1. Calls to allocator functions (Box::new, Vec::new, String::new, etc.)
        // 2. Struct instantiations that require heap storage
        // 3. Large aggregates that exceed stack size limits
        
        if (instr.Opcode == MirOpcode.Call && instr.Operands.Count > 0)
        {
            var callee = instr.Operands[0];
            if (callee.Kind == MirOperandKind.FunctionRef)
            {
                var functionName = callee.Name;
                
                // Check for known allocator patterns
                if (functionName.Contains("::new") || 
                    functionName.Contains("Box") ||
                    functionName.Contains("Vec") ||
                    functionName.StartsWith("alloc") ||
                    functionName.Contains("malloc"))
                {
                    return true;
                }
            }
        }
        
        // Check for struct allocations with heap semantics
        // (In MIR, these might be represented as special opcodes)
        if (instr.Extra is string extra && 
            (extra.Contains("heap") || extra.Contains("alloc")))
        {
            return true;
        }
        
        return false;
    }
}

/// <summary>Escape analysis results.</summary>
internal sealed class EscapeInfo
{
    private readonly HashSet<string> _escapingVars = new();

    public void MarkEscape(string varName)
    {
        _escapingVars.Add(varName);
    }

    public bool Escapes(string varName)
    {
        return _escapingVars.Contains(varName);
    }
}
