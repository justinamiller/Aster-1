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
    if (ptr == NULL) {
        // Treat allocation failure as panic, except for size 0
        // which is allowed to return NULL on some platforms
        if (size > 0) {
            aster_panic("allocation failed", 17);
        }
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

/* Global storage for command-line arguments */
static int g_argc = 0;
static char** g_argv = NULL;

/**
 * Initialize command-line arguments (called from main wrapper)
 */
void __aster_init_args(int argc, char** argv) {
    g_argc = argc;
    g_argv = argv;
}

/**
 * Get command-line argument count
 */
int aster_get_argc(void) {
    return g_argc;
}

/**
 * Get command-line argument at index
 */
const char* aster_get_argv(int index) {
    if (index < 0 || index >= g_argc || g_argv == NULL) {
        return NULL;
    }
    return g_argv[index];
}

/**
 * Read entire file contents into a dynamically allocated string
 */
char* aster_read_file(const char* path, size_t* out_len) {
    if (path == NULL || out_len == NULL) {
        return NULL;
    }

    FILE* f = fopen(path, "rb");
    if (f == NULL) {
        return NULL;
    }

    // Get file size
    fseek(f, 0, SEEK_END);
    long size = ftell(f);
    if (size < 0) {
        fclose(f);
        return NULL;
    }
    fseek(f, 0, SEEK_SET);

    // Allocate buffer (+ 1 for null terminator)
    char* buffer = (char*)aster_malloc(size + 1);
    if (buffer == NULL) {
        fclose(f);
        return NULL;
    }

    // Read file
    size_t bytes_read = fread(buffer, 1, size, f);
    fclose(f);

    if (bytes_read != (size_t)size) {
        aster_free(buffer);
        return NULL;
    }

    // Null-terminate
    buffer[size] = '\0';
    *out_len = (size_t)size;

    return buffer;
}
