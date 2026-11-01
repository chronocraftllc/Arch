using Arch.Core;
using static NUnit.Framework.Assert;

namespace Arch.Tests;

[TestFixture]
public class MultiThreadTest
{
    /// <summary>
    /// Checks if the <see cref="World.WorldSize"/> is correct when creating World multithreaded.
    /// </summary>
    [Test]
    public void MultiThreadedCreateAndDestroy()
    {
        int originalWorldSize = World.WorldSize;
        const int testCount = 10;
        for (int i = 0; i < testCount; ++i)
        {
            var threads = new List<Thread>();
            for (var j = 0; j < Environment.ProcessorCount; j++)
            {
                var thread = new Thread(() =>
                {
                    for (var j = 0; j < 1000; j++)
                    {
                        var world = World.Create();
                        World.Destroy(world);
                    }
                });
                threads.Add(thread);
            }

            threads.ForEach(t => t.Start());

            foreach (var thread in threads)
            {
                thread.Join();
            }

            That(World.WorldSize, Is.EqualTo(originalWorldSize));
        }
    }

    /// <summary>
    /// Checks that <see cref="World"/>s created on many threads at once, while <see cref="World.Worlds"/> grows, each get
    /// an id of their own and are found under it.
    /// </summary>
    [Test]
    public void MultiThreadedCreateWhileTheWorldTableGrows()
    {
        for (var round = 0; round < 20; round++)
        {
            // Given threads that create worlds all at once, far more than the table holds
            var threadCount = Math.Max(4, Environment.ProcessorCount);
            const int worldsPerThread = 64;
            var created = new System.Collections.Concurrent.ConcurrentBag<World>();
            using var start = new Barrier(threadCount);
            var threads = Enumerable.Range(0, threadCount).Select(_ => new Thread(() =>
            {
                start.SignalAndWait();
                for (var i = 0; i < worldsPerThread; i++)
                {
                    created.Add(World.Create());
                }
            })).ToList();

            // When
            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            // Then each is where its id says
            var misplaced = created.Count(world => !ReferenceEquals(World.Worlds[world.Id], world));
            foreach (var world in created)
            {
                World.Destroy(world);
            }

            That(misplaced, Is.EqualTo(0), $"round {round}");
        }
    }
}
