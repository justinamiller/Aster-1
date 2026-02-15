; ASTER Compiler - LLVM IR Output
; Generated at 2026-02-15 04:30:41 UTC

; External runtime declarations
declare i32 @puts(ptr)
declare i32 @printf(ptr, ...)
declare ptr @malloc(i64)
declare void @free(ptr)
declare void @exit(i32)

@.str.0 = private unnamed_addr constant [18 x i8] c"Hello from Aster!\00"

define void @main() {
entry:
  %_t0 = call i32 @puts(ptr @.str.0)
  ret i64 %_t0
}

