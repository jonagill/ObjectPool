namespace ObjectPool 
{
    /// <summary>
    /// Interface for components that need to receive callbacks when the GameObject they're attached to
    /// is acquired from or returned to a PrefabPool.
    ///
    /// Pooled components are cached when a prefab instance is created, so these components must be
    /// on the prefab at the time its PrefabPool is constructed to receive callbacks.
    /// </summary>
    public interface IPooledComponent
    {
        /// <summary>
        /// Called when this object is acquired from its pool.
        /// Invoked before Start() is run the first time this object is acquired.
        /// </summary>
        void OnAcquire();
        /// <summary>
        /// Called when this object is returned to its pool.
        /// </summary>
        void OnReturn();
    }
}