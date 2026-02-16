# Stage 3 Stub Information

## Overview

During bootstrap development, before Stage 2 and Stage 3 are fully implemented, the bootstrap system creates a **Stage 3 stub binary** to enable testing of the self-hosting validation infrastructure.

## What is the Stub?

The stub (`build/bootstrap/stage3/aster3`) is:
- A shell script wrapper around Stage 1
- Created automatically by `bootstrap.sh --stage 3`
- Marked clearly as "Stage 3 Stub" in its content
- Enables verification scripts to run without errors

## Why Use a Stub?

The stub solves a chicken-and-egg problem:

1. **Self-hosting validation logic** needs to be implemented and tested
2. But **Stage 3 binary** won't exist for 9-12 months (requires Stage 1 & 2 completion)
3. **Solution**: Create a stub to test the validation infrastructure now

## What the Stub Enables

With the stub in place:
- ✅ `bootstrap.sh --stage 3` completes successfully
- ✅ `verify.sh --self-check` executes self-hosting logic
- ✅ `check-and-advance.sh` shows Stage 3 as "built"
- ✅ Verification infrastructure can be tested and debugged
- ✅ Clear documentation of what's needed for real Stage 3

## How Verification Handles the Stub

The `verify.sh` script intelligently detects stubs:

```bash
# Checks if binary contains "Stage 3 Stub" marker
if grep -q "Stage 3 Stub" "$aster3" 2>/dev/null; then
    log_warning "Stage 3 is currently a stub"
    log_info "Self-hosting validation will be enabled when real Stage 3 exists"
    # Test that stub at least executes
    "$aster3" --help > /dev/null 2>&1
else
    # Real Stage 3 - perform actual self-hosting validation
    # aster3 compiles itself → aster3'
    # Compare aster3 == aster3'
fi
```

## Transition to Real Stage 3

When Stage 2 is complete and you're ready to build real Stage 3:

1. The stub will be automatically replaced when `bootstrap.sh` succeeds:
   ```bash
   # If Stage 2 exists and Stage 3 source exists
   ./bootstrap/scripts/bootstrap.sh --stage 3
   # → Builds real aster3, overwrites stub
   ```

2. Verification automatically switches to real validation:
   ```bash
   ./bootstrap/scripts/verify.sh --self-check
   # → Detects real binary
   # → Performs actual self-compilation test
   # → Verifies aster3 == aster3'
   ```

## Implementation Timeline

| Phase | Duration | Status |
|-------|----------|--------|
| Stage 1 completion | 2-3 months | 🚧 20% done |
| Stage 2 implementation | 3-4 months | ⏳ Pending |
| Stage 3 implementation | 4-6 months | ⏳ Pending |
| **Total to real Stage 3** | **9-13 months** | ⏳ |

## See Also

- `/build/bootstrap/stage3/README.md` - Detailed stub documentation
- `/bootstrap/spec/bootstrap-stages.md` - Complete bootstrap specification
- `/bootstrap/scripts/verify.sh` - Verification implementation
