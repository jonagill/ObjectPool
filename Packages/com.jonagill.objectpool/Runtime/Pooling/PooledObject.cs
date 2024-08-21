using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace ObjectPool
{
    /// <summary>
    /// Markup component that tracks what PrefabPool a component came from.
    /// </summary>
    [AddComponentMenu("")]
    public class PooledObject : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        private static void OnRuntimeLoad()
        {
            OnParentDestroyed = null;
        }

        // TODO: Replace with List-based Action implementation to prevent heavy allocations
        private static event Action<Transform> OnParentDestroyed;

        public PrefabPool Pool { get; private set; }

        internal void SetPool(PrefabPool pool)
        {
            Debug.Assert(Pool == null);
            Pool = pool;
        }

        private void OnEnable()
        {
            OnParentDestroyed += HandleOnParentDestroyed;
        }

        private void OnDisable()
        {
            OnParentDestroyed -= HandleOnParentDestroyed;
        }

        private void HandleOnParentDestroyed(Transform parent)
        {
            if (transform.IsChildOf(parent))
            {
                ReturnOrDestroy(gameObject);
            }
        }

        /// <summary>
        /// Returns the given object to its pool if it is pooled
        /// or destroys it if it is not pooled.
        /// </summary>
        public static void ReturnOrDestroy(GameObject gameObject)
        {
            Assert.IsNotNull(gameObject);
            ReturnOrDestroy(gameObject.transform);
        }

        /// <summary>
        /// Returns the given object to its pool if it is pooled
        /// or destroys it if it is not pooled.
        /// </summary>
        public static void ReturnOrDestroy<T>(T instance) where T : Component
        {
            var pooledObject = instance.GetComponent<PooledObject>();
            if (pooledObject == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(instance.gameObject);
                    return;
                }
#endif
                // There is no pool to return to, so simply destroy this object
                Destroy(instance.gameObject);
                return;
            }

            pooledObject.Pool.Return(instance.gameObject);
        }

        /// <summary>
        /// Notifies pooled objects that an object that pooled objects might be parented to has been destroyed,
        /// giving them a chance to return to the pool rather than be destroyed.
        /// </summary>
        public static void NotifyParentDestroyed(GameObject parent)
        {
            OnParentDestroyed?.Invoke(parent.transform);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            Debug.LogError(
                $"{nameof(PooledObject)} components should only be added to GameObjects at runtime. ({this})", this);
            DestroyImmediate(this);
        }
#endif
    }
}