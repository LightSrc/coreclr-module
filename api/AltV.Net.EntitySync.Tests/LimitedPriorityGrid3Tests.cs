using System;
using System.Collections.Generic;
using System.Numerics;
using AltV.Net.EntitySync.SpatialPartitions;
using NUnit.Framework;

namespace AltV.Net.EntitySync.Tests
{
    public class LimitedPriorityGrid3Tests
    {
        private LimitedPriorityGrid3 grid2;

        [SetUp]
        public void Setup()
        {
            AltEntitySync.Init(1, (id) => 500, _ => true,
                (threadCount, repository) => new MockNetworkLayer(threadCount, repository),
                (entity, threadCount) => (entity.Id % threadCount), 
                (entityId, entityType, threadCount) => (entityId % threadCount),
                (id) => new LimitedPriorityGrid3(50_000, 50_000, 100, 10_000, 10_000, 3),
                new IdProvider());
            grid2 = new LimitedPriorityGrid3(50_000, 50_000, 100, 10_000, 10_000, 3);
        }

        [Test]
        public void AddTest()
        {
            var position = GetRandomVector3();
            var entity = new PriorityEntity(1, position, 0, 1);
            var entity2 = new PriorityEntity(1, new Vector3(position.X + 1, position.Y, position.Z), 0, 1);
            var entity3 = new PriorityEntity(1, new Vector3(position.X, position.Y + 1, position.Z), 0, 1);
            var entity4 = new PriorityEntity(1, new Vector3(position.X, position.Y + 1, position.Z + 1), 0, 2);
            grid2.Add(entity);
            grid2.Add(entity2);
            grid2.Add(entity3);
            grid2.Add(entity4);
            using (var enumerator = grid2.Find(position, 0).GetEnumerator())
            {
                var currSet = new HashSet<IEntity>();
                while (enumerator.MoveNext())
                {
                    currSet.Add(enumerator.Current);
                }
                Assert.True(currSet.Contains(entity));
                Assert.True(currSet.Contains(entity2));
                Assert.True(currSet.Contains(entity3));
                Assert.False(currSet.Contains(entity4));
            }
        }
        
        [Test]
        public void AddPriorityTest()
        {
            var position = GetRandomVector3();
            var entity = new PriorityEntity(1, position, 0, 1);
            var entity2 = new PriorityEntity(1, position, 0, 1);
            var entity3 = new PriorityEntity(1, new Vector3(position.X, position.Y + 1, position.Z), 0, 1);
            var entity4 =
                new PriorityEntity(1, new Vector3(position.X, position.Y + 1, position.Z + 1), 0, 2)
                {
                    IsHighPriority = true
                };
            grid2.Add(entity);
            grid2.Add(entity2);
            grid2.Add(entity3);
            grid2.Add(entity4);
            using (var enumerator = grid2.Find(position, 0).GetEnumerator())
            {
                var currSet = new HashSet<IEntity>();
                while (enumerator.MoveNext())
                {
                    currSet.Add(enumerator.Current);
                }
                Assert.True(currSet.Contains(entity));
                Assert.True(currSet.Contains(entity2));
                Assert.True(currSet.Contains(entity4));
                Assert.False(currSet.Contains(entity3));
            }
        }

        private static Vector3 GetRandomVector3()
        {
            return new Vector3((float) GetRandomNumber(0, 49_899), (float) GetRandomNumber(0, 49_899),
                (float) GetRandomNumber(0, 49_899));
        }

        private static double GetRandomNumber(double minimum, double maximum)
        {
            var random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
    }
}