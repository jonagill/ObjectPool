using System;

namespace ObjectPool 
{
    public interface IPool : IDisposable
    {
        int TotalCount { get; }
        int ActiveCount { get; }
        int ReserveCount { get; }
        void Clear();
    }

    public interface IPool<T> : IPool where T : class
    {
        PooledInstance<T> Acquire();
        void Return(T obj);
        void PreWarm(int capacity);
    }
}