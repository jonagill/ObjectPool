using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObjectPool
{
    /// <summary>
    /// A singleton MonoBehaviour that wraps a PrefabPoolCollection
    /// and can be used to spawn instances of commonly used prefabs
    /// from a single shared source.
    /// </summary>
    public class GlobalPool : MonoBehaviour, IPoolCollection
    {
        [SerializeField] private bool clearOnSceneUnload = false;

        private PrefabPoolCollection prefabPoolCollection;

        public GlobalPool Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Multiple GlobalPools detected! Deleting...");
                Object.Destroy(this);
                return;
            }

            Instance = this;
            prefabPoolCollection = new PrefabPoolCollection(transform);
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (clearOnSceneUnload)
            {
                prefabPoolCollection.ClearAll();
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            prefabPoolCollection?.Dispose();
            prefabPoolCollection = null;

            Instance = null;
        }

        public PooledInstance<T> Acquire<T>(T prefab) where T : Component
        {
            return prefabPoolCollection.Acquire(prefab);
        }

        public PooledInstance<T> Acquire<T>(T prefab, Transform parent) where T : Component
        {
            return prefabPoolCollection.Acquire(prefab, parent);
        }

        public PooledInstance<T> Acquire<T>(T prefab, Vector3 localPosition, Quaternion localRotation)
            where T : Component
        {
            return prefabPoolCollection.Acquire(prefab, localPosition, localRotation);
        }

        public PooledInstance<T> Acquire<T>(T prefab, Transform parent, Vector3 localPosition, Quaternion localRotation)
            where T : Component
        {
            return prefabPoolCollection.Acquire(prefab, parent, localPosition, localRotation);
        }

        public PooledInstance<T> AcquireDisabled<T>(T prefab) where T : Component
        {
            return prefabPoolCollection.AcquireDisabled(prefab);
        }

        public PooledInstance<T> AcquireDisabled<T>(T prefab, Transform parent) where T : Component
        {
            return prefabPoolCollection.AcquireDisabled(prefab, parent);
        }

        public PooledInstance<T> AcquireDisabled<T>(T prefab, Vector3 localPosition, Quaternion localRotation)
            where T : Component
        {
            return prefabPoolCollection.AcquireDisabled(prefab, localPosition, localRotation);
        }

        public PooledInstance<T> AcquireDisabled<T>(T prefab, Transform parent, Vector3 localPosition,
            Quaternion localRotation) where T : Component
        {
            return prefabPoolCollection.AcquireDisabled(prefab, parent, localPosition, localRotation);
        }

        public void PreWarm<T>(T prefab, int capacity) where T : Component
        {
            prefabPoolCollection.PreWarm(prefab, capacity);
        }

        public void Return<T>(T instance) where T : Component
        {
            prefabPoolCollection.Return(instance);
        }

        public void ClearAll()
        {
            prefabPoolCollection.ClearAll();
        }

        public void Clear<T>(T prefab) where T : Component
        {
            prefabPoolCollection.Clear(prefab);
        }
    }
}