namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Stable diagnostic codes organized by category.
/// Error codes are never reused to ensure stability across compiler versions.
/// </summary>
public static class DiagnosticCode
{
    // Syntax Errors (E1xxx)
    public const string E1000 = "E1000"; // Unexpected token
    public const string E1001 = "E1001"; // Expected token
    public const string E1002 = "E1002"; // Unclosed delimiter
    public const string E1003 = "E1003"; // Invalid literal
    public const string E1004 = "E1004"; // Invalid escape sequence
    public const string E1005 = "E1005"; // Invalid character
    public const string E1006 = "E1006"; // Unterminated string
    public const string E1007 = "E1007"; // Unterminated comment
    public const string E1008 = "E1008"; // Invalid number format

    // Name Resolution Errors (E2xxx)
    public const string E2000 = "E2000"; // Undefined name
    public const string E2001 = "E2001"; // Duplicate definition
    public const string E2002 = "E2002"; // Ambiguous name
    public const string E2003 = "E2003"; // Undefined module
    public const string E2004 = "E2004"; // Cyclic module dependency
    public const string E2005 = "E2005"; // Private item access
    public const string E2006 = "E2006"; // Item not found in module

    // Type System Errors (E3xxx)
    public const string E3000 = "E3000"; // Type mismatch
    public const string E3001 = "E3001"; // Cannot infer type
    public const string E3002 = "E3002"; // Invalid type argument count
    public const string E3003 = "E3003"; // Type parameter bound not satisfied
    public const string E3010 = "E3010"; // Cannot unify types
    public const string E3011 = "E3011"; // Occurs check failed (infinite type)
    public const string E3100 = "E3100"; // Function return type mismatch
    public const string E3101 = "E3101"; // Variable assignment type mismatch
    public const string E3102 = "E3102"; // Function argument count mismatch
    public const string E3103 = "E3103"; // Function argument type mismatch
    public const string E3104 = "E3104"; // If condition must be bool
    public const string E3105 = "E3105"; // Type has no such field
    public const string E3124 = "E3124"; // Cannot unify types (complex)

    // Trait System Errors (E4xxx)
    public const string E4000 = "E4000"; // Type does not implement trait
    public const string E4001 = "E4001"; // Trait method not implemented
    public const string E4002 = "E4002"; // Conflicting trait implementations
    public const string E4003 = "E4003"; // Orphan trait implementation
    public const string E4020 = "E4020"; // Cycle detected in trait resolution
    public const string E4021 = "E4021"; // Type does not implement required trait

    // Effect System Errors (E5xxx)
    public const string E5000 = "E5000"; // Function has undeclared effects
    public const string E5001 = "E5001"; // Effect not allowed in this context
    public const string E5002 = "E5002"; // Async function called in sync context
    public const string E5003 = "E5003"; // Unsafe operation in safe context

    // Ownership Errors (E6xxx)
    public const string E6000 = "E6000"; // Use of moved value
    public const string E6001 = "E6001"; // Cannot move while borrowed
    public const string E6002 = "E6002"; // Cannot borrow moved value
    public const string E6003 = "E6003"; // Cannot immutably borrow while mutably borrowed
    public const string E6004 = "E6004"; // Cannot borrow moved value (duplicate)
    public const string E6005 = "E6005"; // Cannot mutably borrow while already borrowed

    // Borrow Checking Errors (E7xxx)
    public const string E7000 = "E7000"; // Use of moved value
    public const string E7001 = "E7001"; // Cannot move while borrowed
    public const string E7002 = "E7002"; // Cannot borrow moved value
    public const string E7003 = "E7003"; // Cannot mutably borrow while already borrowed
    public const string E7004 = "E7004"; // Cannot immutably borrow while mutably borrowed
    public const string E7005 = "E7005"; // Borrow outlives its referent
    public const string E7006 = "E7006"; // Dangling reference

    // Pattern Matching Errors (E8xxx)
    public const string E8000 = "E8000"; // Match expression has no arms
    public const string E8001 = "E8001"; // Non-exhaustive match
    public const string E8002 = "E8002"; // Unreachable pattern
    public const string E8003 = "E8003"; // Invalid pattern for type

    // MIR/Backend Errors (E9xxx)
    public const string E9000 = "E9000"; // Invalid MIR structure
    public const string E9001 = "E9001"; // Codegen error
    public const string E9002 = "E9002"; // Optimization error

    // Warnings (Wxxx)
    public const string W0001 = "W0001"; // Unreachable pattern
    public const string W0002 = "W0002"; // Unused variable
    public const string W0003 = "W0003"; // Unused function
    public const string W0004 = "W0004"; // Dead code
    public const string W0005 = "W0005"; // Deprecated item

    // Lint Warnings (W1xxx)
    public const string W1000 = "W1000"; // Style violation
    public const string W1001 = "W1001"; // Naming convention
    public const string W1002 = "W1002"; // Missing documentation

    // Package/Toolchain (W2xxx)
    public const string W2000 = "W2000"; // Missing dependency
    public const string W2001 = "W2001"; // Version conflict

    // Info (Ixxx)
    public const string I0001 = "I0001"; // Inferred type
    public const string I0002 = "I0002"; // Optimization applied
    public const string I0003 = "I0003"; // Information message

    // Internal Compiler Errors (E0xxx)
    public const string E0000 = "E0000"; // Internal compiler error
    public const string E0001 = "E0001"; // Assertion failed
}
