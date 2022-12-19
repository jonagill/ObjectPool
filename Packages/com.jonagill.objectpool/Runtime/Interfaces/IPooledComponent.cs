namespace ObjectPool 
{
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