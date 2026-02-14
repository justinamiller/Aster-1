using Aster.Compiler.Incremental;

namespace Aster.Compiler.PerfTests;

public class IncrementalCompilationTests
{
    [Fact]
    public void StableHasher_ProducesDeterministicHashes()
    {
        // Arrange & Act
        var hash1 = StableHasher.Hash("test");
        var hash2 = StableHasher.Hash("test");

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void StableHasher_DifferentInputsProduceDifferentHashes()
    {
        // Arrange & Act
        var hash1 = StableHasher.Hash("test1");
        var hash2 = StableHasher.Hash("test2");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void IncrementalDatabase_CachesResults()
    {
        // Arrange
        var db = new InMemoryIncrementalDatabase();
        var key = new ModuleQueryKey("test", 12345);
        var result = new ModuleQueryResult(new byte[] { 1, 2, 3 }, 99999);

        // Act - First call computes
        var called = false;
        var retrieved1 = db.ExecuteQuery(key, () =>
        {
            called = true;
            return result;
        });

        // Second call should use cache
        called = false;
        var retrieved2 = db.ExecuteQuery(key, () =>
        {
            called = true;
            return result;
        });

        // Assert
        Assert.False(called); // Should not compute second time
        Assert.Equal(retrieved1, retrieved2);
    }

    [Fact]
    public void IncrementalDatabase_InvalidationWorks()
    {
        // Arrange
        var db = new InMemoryIncrementalDatabase();
        var key = new ModuleQueryKey("test", 12345);
        var result = new ModuleQueryResult(new byte[] { 1, 2, 3 }, 99999);

        // Act
        db.ExecuteQuery(key, () => result);
        Assert.True(db.IsCached(key));

        db.Invalidate(key);

        // Assert
        Assert.False(db.IsCached(key));
    }

    [Fact]
    public void DependencyGraph_ComputesInvalidationSet()
    {
        // Arrange
        var graph = new DependencyGraph();
        var key1 = new ModuleQueryKey("module1", 1);
        var key2 = new FunctionQueryKey("module1", "func1", 2);
        var key3 = new FunctionQueryKey("module1", "func2", 3);

        graph.AddDependency(key2, key1); // func1 depends on module1
        graph.AddDependency(key3, key2); // func2 depends on func1

        // Act
        var invalidated = graph.ComputeInvalidationSet(key1);

        // Assert
        Assert.Equal(3, invalidated.Count);
        Assert.Contains(key1, invalidated);
        Assert.Contains(key2, invalidated);
        Assert.Contains(key3, invalidated);
    }

    [Fact]
    public void ParallelCompilationScheduler_ProducesDeterministicResults()
    {
        // Arrange
        var db = new InMemoryIncrementalDatabase();
        var scheduler = new ParallelCompilationScheduler(db, maxParallelism: 2);
        
        var units = new List<CompilationUnit>
        {
            new(new ModuleQueryKey("mod1", 1)),
            new(new ModuleQueryKey("mod2", 2)),
            new(new ModuleQueryKey("mod3", 3))
        };

        // Act
        var results1 = scheduler.CompileSequential(units, key =>
        {
            return new ModuleQueryResult(new byte[] { 1 }, key.ComputeHash());
        });

        var results2 = scheduler.CompileSequential(units, key =>
        {
            return new ModuleQueryResult(new byte[] { 1 }, key.ComputeHash());
        });

        // Assert - Results should be in same order
        Assert.Equal(results1.Count, results2.Count);
        for (int i = 0; i < results1.Count; i++)
        {
            Assert.Equal(results1[i].ComputeHash(), results2[i].ComputeHash());
        }
    }

    [Fact]
    public void CacheSerializer_RoundTrip()
    {
        // Arrange
        var serializer = new CacheSerializer();
        var original = new CodegenResult("fn main() { }", 12345);

        // Act
        var bytes = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(bytes);

        // Assert
        Assert.IsType<CodegenResult>(deserialized);
        var result = (CodegenResult)deserialized;
        Assert.Equal(original.Code, result.Code);
        Assert.Equal(original.Hash, result.Hash);
    }

    [Fact]
    public void DeterministicEmitter_ProducesStableOrder()
    {
        // Arrange
        var emitter = new DeterministicEmitter();

        // Act - Emit in different order
        emitter.Emit("func_c", "code_c");
        emitter.Emit("func_a", "code_a");
        emitter.Emit("func_b", "code_b");

        var emissions = emitter.GetEmissions();

        // Assert - Should be sorted by stable hash
        Assert.Equal(3, emissions.Count);
        
        // Verify determinism by emitting again
        var emitter2 = new DeterministicEmitter();
        emitter2.Emit("func_c", "code_c");
        emitter2.Emit("func_a", "code_a");
        emitter2.Emit("func_b", "code_b");
        
        var emissions2 = emitter2.GetEmissions();
        
        for (int i = 0; i < emissions.Count; i++)
        {
            Assert.Equal(emissions[i].Symbol, emissions2[i].Symbol);
        }
    }
}
