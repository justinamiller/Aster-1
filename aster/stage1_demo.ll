; ASTER Compiler - LLVM IR Output
; Generated at 2026-02-16 17:09:43 UTC

; External runtime declarations
declare i32 @puts(ptr)
declare i32 @printf(ptr, ...)
declare ptr @malloc(i64)
declare void @free(ptr)
declare void @exit(i32)

define ptr @new_rectangle(i32 %w, i32 %h) {
entry:
  ret ptr null
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
  ret i1 %_t2
}

define i32 @factorial(i32 %n) {
entry:
  %_t0 = icmp sle i32 %n, 1
  br i1 %_t0, label %if.then, label %if.else
if.then:
  br label %if.merge
if.else:
  %_t1 = sub i32 %n, 1
  %_t2 = call i64 @factorial(i32 %_t1)
  %_t3 = mul i32 %n, %_t2
  br label %if.merge
if.merge:
  ret i64 1
}

define i32 @sum_range(i32 %start, i32 %end) {
entry:
  br label %while.cond
while.cond:
  %_t0 = icmp sle i32 %start, %end
  br i1 %_t0, label %while.body, label %while.exit
while.body:
  %_t1 = add i64 0, %start
  ; assign 
  %_t2 = add i32 %start, 1
  ; assign start
  br label %while.cond
while.exit:
  ; drop start
  ret i64 0
}

define void @main() {
entry:
  %_t0 = call i64 @new_rectangle(i64 10, i64 20)
  %_t1 = call i64 @area(i64 %_t0)
  ; call print (non-string arg)
  %_t3 = call i64 @perimeter(i64 %_t0)
  ; call print (non-string arg)
  %_t5 = call i64 @is_square(i64 %_t0)
  %_t6 = call i64 @new_rectangle(i64 5, i64 5)
  %_t7 = call i64 @area(i64 %_t6)
  ; call print (non-string arg)
  %_t9 = call i64 @factorial(i64 5)
  ; call print (non-string arg)
  %_t11 = call i64 @sum_range(i64 1, i64 10)
  ; call print (non-string arg)
  ret void
}

