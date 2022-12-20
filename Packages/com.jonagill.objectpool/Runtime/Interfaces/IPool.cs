using System;

namespace ObjectPool 
{
    public interface IPool<T> : IDisposable
    {
        int TotalCount { get; }
        int ActiveCount { get; }
        int ReserveCount { get; }
        
        T Acquire();
        void Return(T obj);
        void PreWarm(int capacity);
        void Clear();
    }
}