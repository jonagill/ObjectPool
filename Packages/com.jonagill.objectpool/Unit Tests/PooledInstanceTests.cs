using NUnit.Framework;
using UnityEngine;

namespace ObjectPool.Tests
{
    public static class PooledInstanceTests
    {
        private class StubPool : IPool
        {
            public void Dispose() { }
            public int TotalCount => 0;
            public int ActiveCount => 0;
            public int ReserveCount => 0;
            public void Clear() { }
        }
        
        private static GameObject instanceObject;
        private static PooledInstance<Transform> pooledInstance;

        [SetUp]
        public static void SetUp()
        {
            instanceObject = new GameObject("PooledObject");
            pooledInstance = new PooledPrefabInstance<Transform>(instanceObject.transform, new StubPool());
        }

        [TearDown]
        public static void TearDown()
        {
            if (instanceObject != null)
            {
                Object.DestroyImmediate(instanceObject);    
            }

            instanceObject = null;
            pooledInstance = null;
        }

        [Test]
        public static void PooledInstanceExposesInstance()
        {
            Assert.AreEqual(instanceObject.transform, pooledInstance.Instance);
        }
        
        [Test]
        public static void PooledInstanceCanBeImplicitlyCast()
        {
            Transform pooledTransform = pooledInstance;
            Assert.AreEqual(instanceObject, pooledTransform.gameObject);
        }
        
        [Test]
        public static void PooledInstanceCanBeInvalidatedExactlyOnce()
        {
            Assert.IsTrue(pooledInstance.IsValid);
            
            IPooledLifetime lifetime = pooledInstance;
            lifetime.MarkInvalid();
            
            Assert.IsFalse(pooledInstance.IsValid);

            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => lifetime.MarkInvalid());
        }
        
        [Test]
        public static void InvalidPooledInstancesEqualNull()
        {
            Assert.IsTrue(pooledInstance != null );
            
            IPooledLifetime lifetime = pooledInstance;
            lifetime.MarkInvalid();
            
            Assert.IsTrue(pooledInstance == null);
        }
        
        [Test]
        public static void InvalidPooledInstancesEqualFalse()
        {
            Assert.IsTrue(pooledInstance);
            
            IPooledLifetime lifetime = pooledInstance;
            lifetime.MarkInvalid();
            
            Assert.IsFalse(pooledInstance);
        }
        
        [Test]
        public static void PooledInstancesWithDestroyedInstanceAreInvalid()
        {
            Assert.IsTrue(pooledInstance.IsValid);
            
            Object.DestroyImmediate(instanceObject);
            
            Assert.IsFalse(pooledInstance.IsValid);
        }
    }
}

