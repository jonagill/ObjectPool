using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace ObjectPool
{
    /// <summary>
    /// A collection of pools that creates and tracks instances of multiple prefabs.
    /// </summary>
    public class PrefabPoolCollection : IPoolCollection, IDisposable
    {
        private readonly Dictionary<Component, PrefabPool> prefabPools = new Dictionary<Component, PrefabPool>();

        private readonly Transform root;
        private bool isDisposed;

        /// <summary>
        /// Create a PrefabPoolCollection using the provided transform as the parent for all inactive prefab instances.
        /// </summary>
        public PrefabPoolCollection(Transform root)
        {
            this.root = root;
        }

        /// <summary>
        /// Create a PrefabPoolCollection using a root-level transform as the parent for all inactive prefab instances. 
        /// </summary>
        public PrefabPoolCollection(string rootName, bool dontDestroyOnLoad = false)
        {
            var rootObject = new GameObject($"Pool Collection ({rootName})");
            if (dontDestroyOnLoad)
            {
                GameObject.DontDestroyOnLoad(rootObject);
            }

            this.root = rootObject.transform;
        }

        #region Public API

        public PooledInstance<T> Acquire<T>(T prefab) where T : Component
        {
            return Acquire(prefab, null, Vector3.zero, Quaternion.identity);
        }

        public PooledInstance<T> Acquire<T>(T prefab, Transform parent) where T : Component
        {
            return Acquire(prefab, parent, Vector3.zero, Quaternion.identity);
        }

        public PooledInstance<T> Acquire<T>(T prefab, Vector3 localPosition, Quaternion localRotation)
            where T : Component
        {
            return Acquire(prefab, null, localPosition, localRotation);
        }

        public PooledInstance<T> Acquire<T>(T prefab, Transform parent, Vector3 localPosition, Quaternion localRotation)
            where T : Component
        {
            return AcquireInternal(prefab, true, parent, localPosition, localRotation);
        }

        public PooledInstance<T> AcquireDisabled<T>(T prefab) where T : Component
        {
            return AcquireDisabled(prefab, null, Vector3.zero, Quaternion.identity);
        }

        public PooledInstance<T> AcquireDisabled<T>(T prefab, Transform parent) where T : Component
        {
            return AcquireDisabled(prefab, parent, Vector3.zero, Quaternion.identity);
        }

        public PooledInstance<T> AcquireDisabled<T>(T prefab, Vector3 localPosition, Quaternion localRotation)
            where T : Component
        {
            return AcquireDisabled(prefab, null, localPosition, localRotation);
        }

        public PooledInstance<T> AcquireDisabled<T>(T prefab, Transform parent, Vector3 localPosition,
            Quaternion localRotation) where T : Component
        {
            return AcquireInternal(prefab, false, parent, localPosition, localRotation);
        }

        public void PreWarm<T>(T prefab, int capacity) where T : Component
        {
            Assert.IsFalse(isDisposed);
            if (prefab == null)
            {
                Debug.LogWarning($"Canont prewarm a pool with a null prefab.");
                return;
            }

            var pool = GetOrCreatePool(prefab);
            pool.PreWarm(capacity);
        }

        public void Return<T>(T instance) where T : Component
        {
            // Returning doesn't actually require any data from PrefabPoolCollection,
            // but is included here so that Acquire() and Return() are accessible from the same API
            PooledObject.ReturnOrDestroy(instance);
        }

        /// <summary>
        /// Destroys all instances of all prefabs that are not currently acquired by an external system.
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in prefabPools.Values)
            {
                pool.Clear();
            }
        }

        /// <summary>
        /// Destroys all instances of the prefab that are not currently acquired by an external system.
        /// </summary>
        public void Clear<T>(T prefab) where T : Component
        {
            if (prefabPools.TryGetValue(prefab, out var pool))
            {
                pool.Clear();
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            foreach (var pool in prefabPools.Values)
            {
                pool.Dispose();
            }

            prefabPools.Clear();

            isDisposed = true;
        }

        #endregion

        #region Internals

        private PooledInstance<T> AcquireInternal<T>(T prefab,
            bool activate, 
            Transform parent, 
            Vector3 localPosition,
            Quaternion localRotation) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot create a pool for a null prefab.");
                return null;
            }

            Assert.IsFalse(isDisposed);

            var pool = GetOrCreatePool(prefab);
            var pooledInstance = pool.Acquire(activate, parent, localPosition, localRotation);

            return pooledInstance;
        }

        #endregion

        #region Helpers

        private PrefabPool<T> GetOrCreatePool<T>(T prefab) where T : Component
        {
            if (!prefabPools.TryGetValue(prefab, out var pool))
            {
                pool = new PrefabPool<T>(prefab, root);
                prefabPools[prefab] = pool;
            }

            return (PrefabPool<T>)pool;
        }

        #endregion
    }
}