# Aster Language Grammar Reference

## Overview

This document defines the complete grammar for the Aster programming language using EBNF (Extended Backus-Naur Form) notation.

## Notation

- `::=` - Definition
- `|` - Alternation (OR)
- `[ ... ]` - Optional (0 or 1 times)
- `{ ... }` - Repetition (0 or more times)
- `( ... )` - Grouping
- `"..."` - Literal terminal symbol
- `'...'` - Literal character/keyword

## Lexical Structure

### Keywords

```
'fn' | 'let' | 'mut' | 'if' | 'else' | 'while' | 'loop' | 'for' | 'in' |
'match' | 'return' | 'break' | 'continue' | 'struct' | 'enum' | 'use' |
'true' | 'false'
```

### Identifiers

```ebnf
identifier ::= ( letter | '_' ) { letter | digit | '_' }
letter     ::= 'a'..'z' | 'A'..'Z'
digit      ::= '0'..'9'
```

### Literals

```ebnf
integer_literal  ::= decimal_literal | hex_literal | octal_literal | binary_literal
decimal_literal  ::= digit { digit }
hex_literal      ::= '0x' hex_digit { hex_digit }
octal_literal    ::= '0o' octal_digit { octal_digit }
binary_literal   ::= '0b' binary_digit { binary_digit }

hex_digit        ::= digit | 'a'..'f' | 'A'..'F'
octal_digit      ::= '0'..'7'
binary_digit     ::= '0' | '1'

float_literal    ::= digit { digit } '.' digit { digit } [ exponent ]
exponent         ::= ( 'e' | 'E' ) [ '+' | '-' ] digit { digit }

boolean_literal  ::= 'true' | 'false'

char_literal     ::= "'" ( char_escape | any_char ) "'"
string_literal   ::= '"' { char_escape | any_char } '"'
char_escape      ::= '\n' | '\r' | '\t' | '\\' | '\'' | '\"'
```

## Program Structure

```ebnf
program ::= { item }

item ::= function_item | struct_item | enum_item | use_item
```

## Items

### Use Declarations

```ebnf
use_item ::= 'use' path [ '::' '*' ] ';'

path ::= identifier { '::' identifier }
```

### Function Definitions

```ebnf
function_item ::= 'fn' identifier '(' [ param_list ] ')' [ '->' type ] block_expr

param_list ::= param { ',' param } [ ',' ]
param      ::= identifier ':' type
```

### Struct Definitions

```ebnf
struct_item ::= 'struct' identifier '{' field_list '}'

field_list ::= field { ',' field } [ ',' ]
field      ::= identifier ':' type
```

### Enum Definitions

```ebnf
enum_item ::= 'enum' identifier '{' variant_list '}'

variant_list ::= variant { ',' variant } [ ',' ]
variant      ::= identifier [ '(' type ')' ]
```

## Types

```ebnf
type ::= primitive_type | named_type | path_type | tuple_type | unit_type

primitive_type ::= 'i8' | 'i16' | 'i32' | 'i64' | 'isize' |
                   'u8' | 'u16' | 'u32' | 'u64' | 'usize' |
                   'f32' | 'f64' | 'bool' | 'char'

named_type ::= identifier

path_type ::= path [ '<' type_list '>' ]

type_list ::= type { ',' type } [ ',' ]

tuple_type ::= '(' type ',' type { ',' type } [ ',' ] ')'

unit_type ::= '(' ')'
```

## Expressions

```ebnf
expr ::= literal_expr
       | identifier_expr
       | call_expr
       | binary_expr
       | unary_expr
       | if_expr
       | while_expr
       | loop_expr
       | match_expr
       | block_expr
       | let_stmt
       | assign_expr
       | return_expr
       | break_expr
       | continue_expr
       | field_expr
       | struct_init_expr
       | path_expr
       | grouped_expr

literal_expr ::= integer_literal | float_literal | boolean_literal | 
                 char_literal | string_literal

identifier_expr ::= identifier

call_expr ::= expr '(' [ expr_list ] ')'

expr_list ::= expr { ',' expr } [ ',' ]

binary_expr ::= expr binary_op expr

binary_op ::= '+' | '-' | '*' | '/' | '%' |
              '==' | '!=' | '<' | '<=' | '>' | '>=' |
              '&&' | '||' |
              '&' | '|' | '^' | '<<' | '>>'

unary_expr ::= unary_op expr

unary_op ::= '-' | '!'

if_expr ::= 'if' expr block_expr [ 'else' ( if_expr | block_expr ) ]

while_expr ::= 'while' expr block_expr

loop_expr ::= 'loop' block_expr

match_expr ::= 'match' expr '{' match_arms '}'

match_arms ::= match_arm { ',' match_arm } [ ',' ]
match_arm  ::= pattern '=>' ( expr | block_expr )

block_expr ::= '{' { expr ';' } [ expr ] '}'

let_stmt ::= 'let' [ 'mut' ] identifier [ ':' type ] '=' expr

assign_expr ::= expr '=' expr

return_expr ::= 'return' [ expr ]

break_expr ::= 'break'

continue_expr ::= 'continue'

field_expr ::= expr '.' identifier

struct_init_expr ::= path '{' field_init_list '}'

field_init_list ::= field_init { ',' field_init } [ ',' ]
field_init      ::= identifier ':' expr

path_expr ::= path [ '::' '<' type_list '>' ]

grouped_expr ::= '(' expr ')'
```

## Patterns

```ebnf
pattern ::= wildcard_pattern
          | literal_pattern
          | identifier_pattern
          | enum_pattern

wildcard_pattern ::= '_'

literal_pattern ::= literal_expr

identifier_pattern ::= identifier

enum_pattern ::= path [ '(' pattern ')' ]
```

## Operator Precedence

From highest to lowest precedence:

1. Field access: `.`
2. Function call: `()`
3. Unary: `-`, `!`
4. Multiplicative: `*`, `/`, `%`
5. Additive: `+`, `-`
6. Shift: `<<`, `>>`
7. Bitwise AND: `&`
8. Bitwise XOR: `^`
9. Bitwise OR: `|`
10. Comparison: `==`, `!=`, `<`, `<=`, `>`, `>=`
11. Logical AND: `&&`
12. Logical OR: `||`
13. Assignment: `=`, `+=`, `-=`, etc.

## Ambiguity Resolution

### Struct Initialization vs Block

When parsing `{ }`, the parser uses lookahead:
- If followed by `identifier:`, it's a struct initialization
- Otherwise, it's a block expression

### Path Expressions

`A::B::C` can be:
- A path to a type (in type position)
- A path to a function/variant (in expression position)

Context determines interpretation.

## Grammar Restrictions (Stage 1 / Core-0)

In Stage 1, the following are NOT supported:
- Generics (user-defined)
- Traits and impl blocks
- References (`&T`, `&mut T`)
- Lifetimes
- Closures
- Async/await
- Macros
- Attributes (except basic allow/deny)

See [STAGE1_SCOPE.md](../bootstrap/STAGE1_SCOPE.md) for complete Stage 1 restrictions.

## Examples

### Function Definition
```rust
fn add(a: i32, b: i32) -> i32 {
    a + b
}
```

### Struct Definition and Initialization
```rust
struct Point {
    x: i32,
    y: i32
}

let p = Point { x: 10, y: 20 };
```

### Enum and Pattern Matching
```rust
enum Option<T> {
    Some(T),
    None
}

match opt {
    Option::Some(value) => { /* use value */ },
    Option::None => { /* handle none */ }
}
```

### Control Flow
```rust
if x > 0 {
    println("positive");
} else if x < 0 {
    println("negative");
} else {
    println("zero");
}

while i < 10 {
    i = i + 1;
}
```

## References

- [Types Reference](types.md)
- [Expressions Reference](expressions.md) (to be created)
- [Patterns Reference](patterns.md) (to be created)
- [Stage1 Scope](../bootstrap/STAGE1_SCOPE.md)

---

**Status**: Stage 1 grammar defined  
**Last Updated**: 2026-02-15
