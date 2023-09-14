using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace ObjectPool
{
    internal interface IPooledLifetime
    {
        bool IsValid { get; }
        void MarkInvalid();
    }
    
    /// <summary>
    /// Wrapper class that is returned from the pool to represent a single pooled lifetime of an object.
    /// When the object is returned to the pool, this wrapper will get disposed and marked as invalid.
    /// </summary>
    public abstract class PooledInstance : IPooledLifetime
    {
        public IPool Pool { get; private set; }

        private bool _isValid;
        public virtual bool IsValid => _isValid;

        public PooledInstance(IPool pool)
        {
            Debug.Assert(pool != null);
            Pool = pool;
            _isValid = true;
        }
        
        // Explicitly implement the IPooledLifetime interface so this has to be called very intentionally
        void IPooledLifetime.MarkInvalid()
        {
            _isValid = false;
        }
        

        // Implicitly convert pooled instances to bools by comparing against null. This matches Unity's 
        // default behavior when casting a UnityEngine.Object to a bool
        public static implicit operator bool(PooledInstance pooledInstance)
        {
            return pooledInstance != null;
        }
        
        // Mirror Unity's default behavior where comparing against null can be used to check if an object is still valid or not
        public static bool operator ==(PooledInstance x, PooledInstance y) => CompareInstances(x, y);

        public static bool operator !=(PooledInstance x, PooledInstance y) => !CompareInstances(x, y);

        private static bool CompareInstances(PooledInstance x, PooledInstance y)
        {
            if (ReferenceEquals(x, y))
            {
                // These are either both null or are the same object
                return true;
            }

            if (ReferenceEquals(null, x))
            {
                // x is null -- check if y is invalid (and thus should return true when compared to null)
                return !y.IsValid;
            }

            if (ReferenceEquals(null, y))
            {
                // y is null -- check if x is invalid (and thus should return true when compared to null)
                return !x.IsValid;
            }
            
            // Neither are null -- actually invoke standard equality comparison
            // (It's expected that this should return false, since we failed the ReferenceEquals
            // check at the top of the function.)
            return x.Equals(y);
        }
    }
    
    /// <summary>
    /// Wrapper class that is returned from the pool to represent a single pooled lifetime of a prefab instance.
    /// When the instance is returned to the pool, this wrapper will get disposed and marked as invalid.
    /// </summary>
    public class PooledInstance<T> : PooledInstance where T : class
    {
        public T Instance { get; private set; }
        public override bool IsValid => base.IsValid && Instance != null;

        public PooledInstance(T instance, PrefabPool pool) : base(pool)
        {
            Assert.IsNotNull(instance);
            Instance = instance;
        }

        // Implicitly convert pooled instances to their backing type so that users can assign directly
        // to the backing type if they want to. (This will deprive them of access to checking if their instance
        // is still valid or if something has returned it to the pool, however.)
        public static implicit operator T(PooledInstance<T> pooledInstance)
        {
            if (pooledInstance != null)
            {
                return pooledInstance.Instance;
            }

            return null;
        }
    }
}
