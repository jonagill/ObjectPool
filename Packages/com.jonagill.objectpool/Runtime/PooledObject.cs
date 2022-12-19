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
    }
}
