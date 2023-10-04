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
            Assert.IsTrue(_isValid);
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
        
        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || this.GetType() != obj.GetType())
            {
                return false;
            }
            else
            {
                PooledInstance other = (PooledInstance) obj;
                return (this.Pool == other.Pool) &&
                       (this.IsValid == other.IsValid);
            }
        }

        public override int GetHashCode()
        {
            // Require this to be implemented in a child class that has enough information to generate a unique hashcode
            throw new NotImplementedException();
        }

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
    /// Wrapper class that is returned from the pool to represent a single pooled lifetime of an object instance.
    /// When the instance is returned to the pool, this wrapper will get disposed and marked as invalid.
    /// </summary>
    public class PooledInstance<T> : PooledInstance where T : class
    {
        private T _instance;
        public T Instance
        {
            get
            {
                if ( IsValid )
                {
                    return _instance;
                }

                throw new InvalidOperationException($"Attempting to retrieve instance {(_instance)} from {nameof(PooledInstance)} that is no longer valid.");
            }
        }

        public PooledInstance(T instance, IPool pool) : base(pool)
        {
            Assert.IsNotNull(instance);
            _instance = instance;
        }

        protected T GetRawInstance() => _instance;
        
        public override bool Equals(Object obj)
        {
            if ( !base.Equals( obj ) )
            {
                return false;
            }
            else
            {
                PooledInstance<T> other = (PooledInstance<T>) obj;
                return (this.Instance == other.Instance);
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine( Pool.GetHashCode(), Instance.GetHashCode() );
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

    /// <summary>
    /// Wrapper class that is returned from the pool to represent a single pooled lifetime of a prefab instance.
    /// When the instance is returned to the pool, this wrapper will get disposed and marked as invalid.
    /// </summary>
    public class PooledPrefabInstance<T> : PooledInstance<T> where T : UnityEngine.Object
    {
        // If our instance gets destroyed, mark ourselves as invalid
        // Must be performed in here rather than the more generic PooledInstance
        // to invoke UnityEngine.Object's special comparison to null
        public override bool IsValid => base.IsValid && GetRawInstance() != null;

        public PooledPrefabInstance(T instance, IPool pool) : base(instance, pool) { }
    }
}
