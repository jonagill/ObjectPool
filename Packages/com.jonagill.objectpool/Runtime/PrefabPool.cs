using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace ObjectPool
{
    public abstract class PrefabPool : IPool
    {
        public abstract int TotalCount { get; }
        public abstract int ActiveCount { get; }
        public abstract int ReserveCount { get; }
        
        public abstract void Return(GameObject gameObject);
        public abstract void Clear();
        public abstract void Dispose();
    }
    
    /// <summary>
    /// A pool that creates and tracks instances of a single prefab.
    /// </summary>
    public class PrefabPool<T> : PrefabPool, IPool<T> where T : Component
    {
        public override int TotalCount => ActiveCount + ReserveCount;
        public override int ActiveCount => activeInstances.Count;
        public override int ReserveCount => reserveInstances.Count;

        private readonly T prefab;
        private readonly Transform disabledRoot;
        private bool isDisposed;

        private readonly List<T> activeInstances = new List<T>();
        private readonly List<T> reserveInstances = new List<T>();

        private readonly bool hasPooledComponents;
        private readonly Dictionary<GameObject, IPooledComponent[]> pooledComponentMap =
            new Dictionary<GameObject, IPooledComponent[]>();

        private readonly bool hasTrailRenderers;
        private readonly Dictionary<GameObject, TrailRenderer[]> trailRendererMap =
            new Dictionary<GameObject, TrailRenderer[]>();
        
        /// <summary>
        /// Create a new prefab pool. Will create a new disabled GameObject under the root
        /// transform under which all prefab instances will be constructed so that they are
        /// instantiated as disabled and do not run Start() until they have been acquired for the first time.
        /// </summary>
        public PrefabPool(T prefab, Transform root)
        {
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(root);

            this.prefab = prefab;
            hasPooledComponents = prefab.GetComponentInChildren<IPooledComponent>(true) != null;
            hasTrailRenderers = prefab.GetComponentInChildren<TrailRenderer>( true ) != null;

            disabledRoot = new GameObject($"PrefabPool ({prefab.name})").transform;
            disabledRoot.gameObject.SetActive(false);
            disabledRoot.SetParent(root, worldPositionStays: false);
        }

        /// <summary>
        /// Retrieve an instance of the configured prefab from the pool, creating a new instance if necessary.
        /// Returns the instance as disabled so that the invoking system can control when to re-enable the instance.
        /// </summary>
        public T Acquire()
        {
            Assert.IsFalse(isDisposed);
            Assert.IsNotNull( disabledRoot );

            T instance = null;
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
                var pooledComponents = pooledComponentMap[instance.gameObject];
                foreach (var component in pooledComponents)
                {
                    if (component != null)
                    {
                        component.OnAcquire();
                    }
                }
            }
            
            // Clear any old vertices from our trail renderers
            if ( hasTrailRenderers )
            {
                var trailRenderers = trailRendererMap[instance.gameObject];
                foreach ( var renderer in trailRenderers )
                {
                    renderer.Clear();
                }
            }

            // Do not re-activate the instance -- leave that for the invoker to decide when to activate the instance
            return instance;
        }
        
        public override void Return(GameObject gameObject)
        {
            Assert.IsNotNull( gameObject );
            var instance = gameObject.GetComponent<T>();
            Assert.IsNotNull( instance );

            Return( instance );
        }

        public void Return(T instance)
        {
            Assert.IsNotNull( instance );
            Assert.IsNotNull( disabledRoot );

            if (isDisposed)
            {
                // We can't return this instance to our pool -- just destroy it
                Object.Destroy(instance.gameObject);
                return;
            }
            
            if ( reserveInstances.Contains( instance ) )
            {
                // Someone else has already returned this object
                return;
            }

#if UNITY_ASSERTIONS
            // Check that this is the right pool to be returning to
            var pooledObjectForAssertions = instance.GetComponent<PooledObject>();
            Assert.IsNotNull(
                pooledObjectForAssertions,
                $"Component {instance} cannot be returned as it was not instantiated by a pool.");
            Assert.AreEqual(
                this,
                pooledObjectForAssertions.Pool,
                $"Component {instance} cannot be returned as it was instantiated by a different pool.");
#endif
            
            Assert.IsTrue( activeInstances.Contains( instance ), $"Component {instance} cannot be returned as it is not considered an active instance by this pool." );
            
            // Notify any IPooledComponents that they're being returned
            if (hasPooledComponents)
            {
                var pooledComponents = pooledComponentMap[instance.gameObject];
                foreach (var component in pooledComponents)
                {
                    if (component != null)
                    {
                        component.OnReturn();    
                    }
                }
            }
            
            // Disable the object again so we don't pay additional costs for reparenting (e.g. recalculating UI layouts)
            instance.gameObject.SetActive(false);

            // Reparent under the disabled root
            instance.transform.SetParent(disabledRoot, false);

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

        public override void Clear()
        {
            foreach (var instance in reserveInstances)
            {
                if ( hasPooledComponents )
                {
                    pooledComponentMap.Remove( instance.gameObject );
                }
                
                if ( hasTrailRenderers )
                {
                    trailRendererMap.Remove( instance.gameObject );
                }
                
                Object.Destroy(instance.gameObject);
            }
            reserveInstances.Clear();
        }

        public override void Dispose()
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

        private T CreateInstance()
        {
            // Instantiate the object under the disabled root so Start() doesn't run until it gets acquired
            var instance = Object.Instantiate(prefab, disabledRoot);
            instance.gameObject.SetActive(false);
            
            var pooledObject = instance.gameObject.AddComponent<PooledObject>();
            pooledObject.SetPool(this);

            if (hasPooledComponents)
            {
                pooledComponentMap[instance.gameObject] = instance.GetComponentsInChildren<IPooledComponent>(true);
            }
            
            if ( hasTrailRenderers )
            {
                trailRendererMap[instance.gameObject] = instance.GetComponentsInChildren<TrailRenderer>( true );
            }

            return instance;
        }
    }
}
