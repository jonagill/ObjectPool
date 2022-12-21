using UnityEditor;
using UnityEngine;

namespace ObjectPool
{
    /// <summary>
    /// Markup component that tracks what PrefabPool a component came from.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public PrefabPool Pool { get; private set; }

        internal void SetPool(PrefabPool pool)
        {
            Debug.Assert(Pool == null);
            Pool = pool;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this))
            {
                Debug.LogError($"{nameof(PooledObject)} components should only be added to GameObjects at runtime. ({this})", this);
                DestroyImmediate(this);
            }
        }
#endif

        public static void Return<T>(T instance) where T : Component
        {
            var pooledObject = instance.GetComponent<PooledObject>();
            if (pooledObject == null)
            {
                // There is no pool to return to, so simply destroy this object
                Destroy( instance.gameObject  );
                return;
            }

            pooledObject.Pool.Return(instance);
        }
    }
}
