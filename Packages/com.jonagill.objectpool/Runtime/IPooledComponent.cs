namespace ObjectPool 
{
    public interface IPooledComponent
    {
        /// <summary>
        /// Called when this object is acquired from its pool.
        /// </summary>
        void OnAcquire();
        /// <summary>
        /// Called when this object is returned to its pool.
        /// </summary>
        void OnReturn();
    }
}