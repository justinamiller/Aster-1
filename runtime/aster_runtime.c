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

/* Constants for runtime path search */
#define MAX_PARENT_SEARCH_DEPTH 5  /* Maximum depth to search up directory tree for runtime */

/**
 * Initialize command-line arguments
 */
void aster_init_args(int argc, char** argv) {
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
 * Get command-line argument by index
 */
const char* aster_get_argv(int index) {
    if (index < 0 || index >= g_argc) {
        return NULL;
    }
    return g_argv[index];
}

/**
 * Read entire file into memory
 */
char* aster_read_file(const char* path, size_t* out_length) {
    FILE* file = fopen(path, "rb");
    if (!file) {
        return NULL;
    }
    
    // Get file size using fseeko/ftello for better portability with large files
    if (fseeko(file, 0, SEEK_END) != 0) {
        fclose(file);
        return NULL;
    }
    
    off_t size = ftello(file);
    if (size < 0) {
        fclose(file);
        return NULL;
    }
    
    if (fseeko(file, 0, SEEK_SET) != 0) {
        fclose(file);
        return NULL;
    }
    
    // Check for reasonable file size (avoid overflow)
    if ((unsigned long long)size > SIZE_MAX - 1) {
        fclose(file);
        return NULL;
    }
    
    // Allocate buffer
    char* buffer = (char*)malloc((size_t)size + 1);
    if (!buffer) {
        fclose(file);
        return NULL;
    }
    
    // Read file
    size_t bytes_read = fread(buffer, 1, (size_t)size, file);
    
    // Check for read errors
    if (ferror(file)) {
        free(buffer);
        fclose(file);
        return NULL;
    }
    
    fclose(file);
    
    buffer[bytes_read] = '\0';  // Null terminate at actual bytes read
    if (out_length) {
        *out_length = bytes_read;
    }
    
    return buffer;
}
