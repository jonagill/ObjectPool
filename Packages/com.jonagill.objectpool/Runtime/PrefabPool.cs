using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace ObjectPool
{
    /// <summary>
    /// A pool that creates and tracks instances of a single prefab.
    /// </summary>
    public class PrefabPool : IPool<Component>
    {
        public int TotalCount => ActiveCount + ReserveCount;
        public int ActiveCount => activeInstances.Count;
        public int ReserveCount => reserveInstances.Count;

        private readonly Component prefab;
        private readonly Transform disabledRoot;
        private readonly bool hasPooledComponents;
        private bool isDisposed;

        private readonly List<Component> activeInstances = new List<Component>();
        private readonly List<Component> reserveInstances = new List<Component>();

        private readonly Dictionary<Component, IPooledComponent[]> pooledComponentMap =
            new Dictionary<Component, IPooledComponent[]>();

        /// <summary>
        /// Create a new prefab pool. Will create a new disabled GameObject under the root
        /// transform under which all prefab instances will be constructed so that they are
        /// instantiated as disabled and do not run Start() until they have been acquired for the first time.
        /// </summary>
        public PrefabPool(Component prefab, Transform root)
        {
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(root);

            this.prefab = prefab;
            hasPooledComponents = prefab.GetComponentInChildren<IPooledComponent>(true) != null;

            disabledRoot = new GameObject($"PrefabPool ({prefab.name})").transform;
            disabledRoot.gameObject.SetActive(false);
            disabledRoot.SetParent(root, worldPositionStays: false);
        }

        /// <summary>
        /// Retrieve an instance of the configured prefab from the pool, creating a new instance if necessary.
        /// Returns the instance as disabled so that the invoking system can control when to re-enable the instance.
        /// </summary>
        public Component Acquire()
        {
            Assert.IsFalse(isDisposed);

            Component instance = null;
            if (reserveInstances.Count > 0)
            {
                var lastIndex = reserveInstances.Count - 1;
                instance = reserveInstances[lastIndex];
                reserveInstances.RemoveAt(lastIndex);
            }
            else
            {
                instance = CreateInstance();
            }

            // Track the new instance as active
            activeInstances.Add(instance);

            // Notify any IPooledComponents that they've been acquired
            if (hasPooledComponents)
            {
                var pooledComponents = pooledComponentMap[instance];
                foreach (var component in pooledComponents)
                {
                    if (component != null)
                    {
                        component.OnAcquire();
                    }
                }
            }

            // Do not re-activate the instance -- leave that for the invoker to decide when to activate the instance
            return instance;
        }

        public void Return(Component instance)
        {
            if (isDisposed)
            {
                // We can't return this instance to our pool -- just destroy it
                Object.Destroy(instance.gameObject);
                return;
            }

#if UNITY_ASSERTIONS
            var pooledObject = instance.GetComponent<PooledObject>();
            Assert.IsNotNull(
                pooledObject,
                $"Component {instance} cannot be returned as it was not instantiated by a pool.");
            Assert.AreEqual(
                this,
                pooledObject.Pool,
                $"Component {instance} cannot be returned as it was instantiated by a different pool.");
            Assert.AreEqual(
                prefab.GetType(),
                instance.GetType(),
                $"Component {instance} cannot be returned as it is of type {instance.GetType()}, but this pool expects type {prefab.GetType()}.");

            Assert.IsTrue(
                activeInstances.Contains(instance),
                $"Component {instance} cannot be returned as it is not considered an active instance by this pool.");
#endif

            // Notify any IPooledComponents that they're being returned
            if (hasPooledComponents)
            {
                var pooledComponents = pooledComponentMap[instance];
                foreach (var component in pooledComponents)
                {
                    if (component != null)
                    {
                        component.OnReturn();    
                    }
                }
            }

            // Disable the object again so we don't pay additional costs for reparenting (e.g. recalculating UI layouts)
            pooledObject.gameObject.SetActive(false);

            // Reparent under the disabled root
            pooledObject.transform.SetParent(disabledRoot, false);

            activeInstances.Remove(instance);
            reserveInstances.Add(instance);
        }

        /// <summary>
        /// Allocates new instances of the prefab until we have at least the specified capacity spawned.
        /// </summary>
        /// <param name="capacity"></param>
        public void PreWarm(int capacity)
        {
            Assert.IsFalse(isDisposed);

            var needInReserve = capacity - ActiveCount;
            while (reserveInstances.Count < needInReserve)
            {
                reserveInstances.Add(CreateInstance());
            }
        }

        public void Clear()
        {
            foreach (var instance in reserveInstances)
            {
                Object.Destroy(instance.gameObject);
            }
            reserveInstances.Clear();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            Object.Destroy(disabledRoot);
            activeInstances.Clear();
            reserveInstances.Clear();
            pooledComponentMap.Clear();

            isDisposed = true;
        }

        private Component CreateInstance()
        {
            // Instantiate the object under the disabled root so Start() doesn't run until it gets acquired
            var instance = Object.Instantiate(prefab, disabledRoot);
            instance.gameObject.SetActive(false);
            
            var pooledObject = instance.gameObject.AddComponent<PooledObject>();
            pooledObject.SetPool(this);

            if (hasPooledComponents)
            {
                pooledComponentMap[instance] = instance.GetComponentsInChildren<IPooledComponent>(true);
            }
            
            return instance;
        }
    }
}
