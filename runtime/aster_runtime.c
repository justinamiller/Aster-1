/**
 * Aster Minimal Runtime Library
 * 
 * Provides essential runtime intrinsics for Aster programs.
 * This is a minimal C implementation of the runtime ABI.
 * 
 * Required intrinsics:
 * - aster_panic(msg, len) — Panic handler with message
 * - aster_malloc(size) — Allocate memory
 * - aster_free(ptr) — Free memory
 * - aster_write_stdout(ptr, len) — Write to stdout
 * - aster_exit(code) — Exit the program
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

/**
 * Panic handler - prints panic message and aborts
 * @param msg Pointer to panic message
 * @param len Length of panic message
 */
void aster_panic(const char* msg, size_t len) {
    fprintf(stderr, "panic: ");
    fwrite(msg, 1, len, stderr);
    fprintf(stderr, "\n");
    abort();
}

/**
 * Allocate memory
 * @param size Number of bytes to allocate
 * @return Pointer to allocated memory, or NULL on failure
 */
void* aster_malloc(size_t size) {
    void* ptr = malloc(size);
    if (ptr == NULL && size > 0) {
        aster_panic("allocation failed", 17);
    }
    return ptr;
}

/**
 * Free memory
 * @param ptr Pointer to memory to free
 */
void aster_free(void* ptr) {
    free(ptr);
}

/**
 * Write to stdout
 * @param ptr Pointer to data
 * @param len Length of data
 */
void aster_write_stdout(const char* ptr, size_t len) {
    fwrite(ptr, 1, len, stdout);
    fflush(stdout);
}

/**
 * Exit the program
 * @param code Exit code
 */
void aster_exit(int code) {
    exit(code);
}

/**
 * Print a null-terminated string (compatibility wrapper for puts)
 */
int aster_puts(const char* str) {
    return puts(str);
}

/**
 * Print an integer
 */
void aster_print_int(long long value) {
    printf("%lld", value);
    fflush(stdout);
}

/**
 * Print a newline
 */
void aster_println(void) {
    putchar('\n');
    fflush(stdout);
}
