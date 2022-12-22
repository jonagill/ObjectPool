using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ObjectPool
{
    public interface IPoolCollection
    {
        T Acquire<T>(T prefab) where T : Component;
        T Acquire<T>(T prefab, Transform parent) where T : Component;
        T Acquire<T>(T prefab, Vector3 localPosition, Quaternion localRotation) where T : Component;
        T Acquire<T>(T prefab, Transform parent, Vector3 localPosition, Quaternion localRotation) where T : Component;
        T AcquireDisabled<T>(T prefab) where T : Component;
        T AcquireDisabled<T>(T prefab, Transform parent) where T : Component;
        T AcquireDisabled<T>(T prefab, Vector3 localPosition, Quaternion localRotation) where T : Component; 
        T AcquireDisabled<T>(T prefab, Transform parent, Vector3 localPosition, Quaternion localRotation) where T : Component;

        void PreWarm<T>(T prefab, int capacity) where T : Component;
        void Return<T>(T instance) where T : Component;
        void ClearAll();
        void Clear<T>(T prefab) where T : Component;
    }
}