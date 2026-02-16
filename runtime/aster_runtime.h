/**
 * Aster Minimal Runtime Library - Header
 * 
 * Public API for the Aster runtime.
 */

#ifndef ASTER_RUNTIME_H
#define ASTER_RUNTIME_H

#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Panic handler - prints panic message and aborts
 * @param msg Pointer to panic message
 * @param len Length of panic message
 */
void aster_panic(const char* msg, size_t len);

/**
 * Allocate memory
 * @param size Number of bytes to allocate
 * @return Pointer to allocated memory, or NULL on failure
 */
void* aster_malloc(size_t size);

/**
 * Free memory
 * @param ptr Pointer to memory to free
 */
void aster_free(void* ptr);

/**
 * Write to stdout
 * @param ptr Pointer to data
 * @param len Length of data
 */
void aster_write_stdout(const char* ptr, size_t len);

/**
 * Exit the program
 * @param code Exit code
 */
void aster_exit(int code);

/**
 * Print a null-terminated string (compatibility wrapper)
 */
int aster_puts(const char* str);

/**
 * Print an integer
 */
void aster_print_int(long long value);

/**
 * Print a newline
 */
void aster_println(void);

/**
 * Get command-line argument count
 * @return Number of command-line arguments
 */
int aster_get_argc(void);

/**
 * Get command-line argument at index
 * @param index Argument index (0 = program name)
 * @return Pointer to argument string, or NULL if index out of bounds
 */
const char* aster_get_argv(int index);

/**
 * Read entire file contents into a dynamically allocated string
 * @param path Path to file
 * @param out_len Pointer to store length of file contents
 * @return Pointer to file contents (caller must free), or NULL on error
 */
char* aster_read_file(const char* path, size_t* out_len);

#ifdef __cplusplus
}
#endif

#endif /* ASTER_RUNTIME_H */
