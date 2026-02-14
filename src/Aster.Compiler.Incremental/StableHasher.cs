using System.Security.Cryptography;
using System.Text;

namespace Aster.Compiler.Incremental;

/// <summary>
/// Deterministic hasher that produces stable hashes across machines and runs.
/// Uses SHA256 for cryptographic strength and stability.
/// </summary>
public sealed class StableHasher
{
    private readonly IncrementalHash _hasher;

    public StableHasher()
    {
        _hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    }

    /// <summary>Hash a string value.</summary>
    public void HashString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        _hasher.AppendData(bytes);
    }

    /// <summary>Hash an integer value.</summary>
    public void HashInt32(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        _hasher.AppendData(bytes);
    }

    /// <summary>Hash a long value.</summary>
    public void HashInt64(long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        _hasher.AppendData(bytes);
    }

    /// <summary>Hash a boolean value.</summary>
    public void HashBool(bool value)
    {
        _hasher.AppendData(new[] { (byte)(value ? 1 : 0) });
    }

    /// <summary>Hash raw bytes.</summary>
    public void HashBytes(ReadOnlySpan<byte> bytes)
    {
        _hasher.AppendData(bytes);
    }

    /// <summary>Finalize the hash and get the result as a 64-bit fingerprint.</summary>
    public ulong Finalize()
    {
        var hash = _hasher.GetHashAndReset();
        // Take first 8 bytes for a 64-bit fingerprint
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(hash);
    }

    /// <summary>Compute a stable hash for a string.</summary>
    public static ulong Hash(string value)
    {
        var hasher = new StableHasher();
        hasher.HashString(value);
        return hasher.Finalize();
    }

    /// <summary>Compute a stable hash for multiple strings.</summary>
    public static ulong Hash(params string[] values)
    {
        var hasher = new StableHasher();
        foreach (var value in values)
        {
            hasher.HashString(value);
        }
        return hasher.Finalize();
    }
}
