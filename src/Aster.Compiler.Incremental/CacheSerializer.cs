namespace Aster.Compiler.Incremental;

/// <summary>
/// Serializer for cached compilation data.
/// Supports versioning for schema evolution.
/// Uses a simple binary format with version header.
/// </summary>
public sealed class CacheSerializer
{
    private const uint MagicNumber = 0x41535452; // "ASTR"
    private const uint CurrentVersion = 1;

    /// <summary>
    /// Serialize a query result to bytes.
    /// Format: [Magic][Version][TypeTag][Data]
    /// </summary>
    public byte[] Serialize(QueryResult result)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write header
        writer.Write(MagicNumber);
        writer.Write(CurrentVersion);

        // Write type tag and data based on result type
        switch (result)
        {
            case ModuleQueryResult module:
                writer.Write((byte)1);
                writer.Write(module.Data.Length);
                writer.Write(module.Data);
                writer.Write(module.Hash);
                break;

            case FunctionQueryResult function:
                writer.Write((byte)2);
                writer.Write(function.FunctionName);
                writer.Write(function.MirData.Length);
                writer.Write(function.MirData);
                writer.Write(function.Hash);
                break;

            case TypeCheckResult typeCheck:
                writer.Write((byte)3);
                writer.Write(typeCheck.Success);
                writer.Write(typeCheck.Errors.Length);
                foreach (var error in typeCheck.Errors)
                {
                    writer.Write(error);
                }
                writer.Write(typeCheck.Hash);
                break;

            case OptimizedMirResult optimized:
                writer.Write((byte)4);
                writer.Write(optimized.OptimizedMir.Length);
                writer.Write(optimized.OptimizedMir);
                writer.Write(optimized.Hash);
                break;

            case CodegenResult codegen:
                writer.Write((byte)5);
                writer.Write(codegen.Code);
                writer.Write(codegen.Hash);
                break;

            default:
                throw new InvalidOperationException($"Unknown result type: {result.GetType()}");
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserialize a query result from bytes.
    /// Validates version and magic number.
    /// </summary>
    public QueryResult Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // Read and validate header
        var magic = reader.ReadUInt32();
        if (magic != MagicNumber)
        {
            throw new InvalidOperationException("Invalid cache file: bad magic number");
        }

        var version = reader.ReadUInt32();
        if (version != CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported cache version: {version}");
        }

        // Read type tag and deserialize based on type
        var typeTag = reader.ReadByte();
        return typeTag switch
        {
            1 => DeserializeModuleResult(reader),
            2 => DeserializeFunctionResult(reader),
            3 => DeserializeTypeCheckResult(reader),
            4 => DeserializeOptimizedMirResult(reader),
            5 => DeserializeCodegenResult(reader),
            _ => throw new InvalidOperationException($"Unknown type tag: {typeTag}")
        };
    }

    private static ModuleQueryResult DeserializeModuleResult(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        var data = reader.ReadBytes(length);
        var hash = reader.ReadUInt64();
        return new ModuleQueryResult(data, hash);
    }

    private static FunctionQueryResult DeserializeFunctionResult(BinaryReader reader)
    {
        var functionName = reader.ReadString();
        var length = reader.ReadInt32();
        var mirData = reader.ReadBytes(length);
        var hash = reader.ReadUInt64();
        return new FunctionQueryResult(functionName, mirData, hash);
    }

    private static TypeCheckResult DeserializeTypeCheckResult(BinaryReader reader)
    {
        var success = reader.ReadBoolean();
        var errorCount = reader.ReadInt32();
        var errors = new string[errorCount];
        for (int i = 0; i < errorCount; i++)
        {
            errors[i] = reader.ReadString();
        }
        var hash = reader.ReadUInt64();
        return new TypeCheckResult(success, errors, hash);
    }

    private static OptimizedMirResult DeserializeOptimizedMirResult(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        var optimizedMir = reader.ReadBytes(length);
        var hash = reader.ReadUInt64();
        return new OptimizedMirResult(optimizedMir, hash);
    }

    private static CodegenResult DeserializeCodegenResult(BinaryReader reader)
    {
        var code = reader.ReadString();
        var hash = reader.ReadUInt64();
        return new CodegenResult(code, hash);
    }
}
