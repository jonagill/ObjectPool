using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace ObjectPool
{
    public class PrefabPool : IPool<Component>
    {
        private static readonly List<IPooledComponent> SCRATCH_POOLED_COMPONENTS = new List<IPooledComponent>();

        public int TotalCount => ActiveCount + ReserveCount;
        public int ActiveCount => _activeInstances.Count;
        public int ReserveCount => _reserveInstances.Count;

        private readonly Component prefab;
        private readonly Transform disabledRoot;
        private readonly bool hasPooledComponents;
        private bool isDisposed;

        private readonly List<Component> _activeInstances = new List<Component>();
        private readonly List<Component> _reserveInstances = new List<Component>();

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

        public Component Acquire()
        {
            Assert.IsFalse(isDisposed);

            Component instance = null;
            if (_reserveInstances.Count > 0)
            {
                var lastIndex = _reserveInstances.Count - 1;
                instance = _reserveInstances[lastIndex];
                _reserveInstances.RemoveAt(lastIndex);
            }
            else
            {
                instance = CreateInstance();
            }

            _activeInstances.Add(instance);

            // Set the object active
            // Its components should still be disabled due to being under a disabled transform
            instance.gameObject.SetActive(true);

            // Notify any IPooledComponents that they've been acquired
            if (hasPooledComponents)
            {
                instance.GetComponentsInChildren(SCRATCH_POOLED_COMPONENTS);
                foreach (var component in SCRATCH_POOLED_COMPONENTS)
                {
                    component.OnAcquire();
                }

                SCRATCH_POOLED_COMPONENTS.Clear();
            }

            return instance;
        }

        public void Return(Component instance)
        {
            Assert.IsFalse(isDisposed);

#if UNITY_ASSERTIONS
            var pooledObject = instance.GetComponent<PooledObject>();
            Assert.IsNotNull(
                pooledObject,
                $"Component {instance} cannot be returned as it was not instantiated by a pool.");
            Assert.AreEqual(
                this,
                pooledObject.Pool,
                $"Component {instance} cannot be returned as it was instantiated by a different pool.");
            Assert.IsTrue(
                _activeInstances.Contains(instance),
                $"Component {instance} cannot be returned as it is not considered an active instance by this pool.");
#endif

            // Notify any IPooledComponents that they're being returned
            if (hasPooledComponents)
            {
                instance.GetComponentsInChildren(SCRATCH_POOLED_COMPONENTS);
                foreach (var component in SCRATCH_POOLED_COMPONENTS)
                {
                    component.OnReturn();
                }

                SCRATCH_POOLED_COMPONENTS.Clear();
            }

            // Disable the object
            pooledObject.gameObject.SetActive(false);

            // Reparent under the disabled root
            pooledObject.transform.SetParent(disabledRoot, false);

            _activeInstances.Remove(instance);
            _reserveInstances.Add(instance);
        }

        /// <summary>
        /// Allocates new instances of the prefab until we have at least the specified capacity spawned.
        /// </summary>
        /// <param name="capacity"></param>
        public void PreWarm(int capacity)
        {
            Assert.IsFalse(isDisposed);

            var needInReserve = capacity - ActiveCount;
            while (_reserveInstances.Count < needInReserve)
            {
                _reserveInstances.Add(CreateInstance());
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            Object.Destroy(disabledRoot);
            _activeInstances.Clear();
            _reserveInstances.Clear();

            isDisposed = true;
        }

        private Component CreateInstance()
        {
            var instance = Object.Instantiate(prefab, disabledRoot);
            var pooledObject = instance.gameObject.AddComponent<PooledObject>();
            pooledObject.SetPool(this);
            return instance;
        }
    }
}
