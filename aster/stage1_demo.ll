; ASTER Compiler - LLVM IR Output
; Generated at 2026-02-14 23:34:23 UTC

; External runtime declarations
declare i32 @puts(ptr)
declare i32 @printf(ptr, ...)
declare ptr @malloc(i64)
declare void @free(ptr)
declare void @exit(i32)

define ptr @new_rectangle(i32 %w, i32 %h) {
entry:
  ret void
}

define i32 @area(ptr %rect) {
entry:
  %_t2 = mul i64 %_t0, %_t1
  ret i64 %_t2
}

define i32 @perimeter(ptr %rect) {
entry:
  %_t1 = mul i64 2, %_t0
  %_t3 = mul i64 2, %_t2
  %_t4 = add i64 %_t1, %_t3
  ret i64 %_t4
}

define i1 @is_square(ptr %rect) {
entry:
  %_t2 = icmp eq i64 %_t0, %_t1
  ret i64 %_t2
}

define i32 @factorial(i32 %n) {
entry:
  %_t0 = icmp sle i64 %n, 1
  br i1 %_t0, label %if.then, label %if.else
if.then:
  br label %if.merge
if.else:
  %_t1 = sub i64 %n, 1
  %_t2 = call i64 @factorial(i64 %_t1)
  %_t3 = mul i64 %n, %_t2
  br label %if.merge
if.merge:
  ret i64 1
}

define i32 @sum_range(i32 %start, i32 %end) {
entry:
  ; assign sum
  ; assign i
  br label %while.cond
while.cond:
  %_t0 = icmp sle i64 %i, %end
  br i1 %_t0, label %while.body, label %while.exit
while.body:
  %_t1 = add i64 %sum, %i
  ; assign sum
  %_t2 = add i64 %i, 1
  ; assign i
  br label %while.cond
while.exit:
  ; drop i
  ret i64 %sum
}

define void @main() {
entry:
  %_t0 = call i64 @new_rectangle(i64 10, i64 20)
  ; assign rect
  %_t1 = call i64 @area(i64 %rect)
  ; assign a
  ; call print (non-string arg)
  %_t3 = call i64 @perimeter(i64 %rect)
  ; assign p
  ; call print (non-string arg)
  %_t5 = call i64 @is_square(i64 %rect)
  ; assign is_sq
  %_t6 = call i64 @new_rectangle(i64 5, i64 5)
  ; assign square
  %_t7 = call i64 @area(i64 %square)
  ; assign sq_area
  ; call print (non-string arg)
  %_t9 = call i64 @factorial(i64 5)
  ; assign fact5
  ; call print (non-string arg)
  %_t11 = call i64 @sum_range(i64 1, i64 10)
  ; assign sum
  ; call print (non-string arg)
  ; drop rect
  ; drop a
  ; drop p
  ; drop is_sq
  ; drop square
  ; drop sq_area
  ; drop fact5
  ; drop sum
  ret void
}

