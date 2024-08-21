using UnityEngine;

namespace ObjectPool
{
    /// <summary>
    /// MonoBehaviour extension that provides Initialize() and Cleanup() methods that standardizes the object's
    /// lifecycle whether it was instantiated for the first time or acquired from the pool.
    /// As many other frameworks may declare their own MonoBehaviour child classes (e.g. for networking),
    /// this is provided more as an example than as production code.
    /// </summary>
    public abstract class PooledBehaviour : MonoBehaviour, IPooledComponent
    {
        private bool hasRunAwake = false;
        
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Called directly after the behaviour is instantiated.
        /// Should be used to configure state that lasts the entire lifecycle of the behaviour's GameObject
        /// and that does not rely on references to other objects.
        /// </summary>
        protected virtual void Awake()
        {
            hasRunAwake = true;
        }
        
        /// <summary>
        /// Called after an object is instantiated and before its first Update() function is invoked.
        /// Can reference other objects, as we can assume their Awake() has run already and they are in
        /// something approaching a sensible state.
        /// Should be used to configure state that lasts the entire lifecycle of the behaviour's GameObject.
        /// State related to a single lifetime of a pooled object should be set in Initialize() instead.
        /// </summary>
        protected virtual void Start()
        {
            TryInitialize();
        }
        
        /// <summary>
        /// Called right before an object is destroyed and cleaned up by Unity.
        /// Should be used to tear down and state and release any resources that were configured for the
        /// entire lifecycle of the behaviour's GameObject.
        /// State related to a single lifetime of a pooled object should be reset in Cleanup() instead.
        /// </summary>
        protected virtual void OnDestroy()
        {
            TryCleanup();
        }

        /// <summary>
        /// Called when a behaviour is retrieved from the pooling system.
        /// Note that this will run BEFORE Start() for Entities that were just instantiated.
        /// Should only be used to specify code that is specifically related to the pooling system.
        /// Most other code should be put in Initialize(), which is called regardless of whether the object
        /// is spawned regularly or retrieved from the pooling system.
        /// </summary>
        public virtual void OnAcquire()
        {
            TryInitialize();
        }

        /// <summary>
        /// Called when an behaviour is returned to the pooling system.
        /// Should only be used to specify code that is specifically related to the pooling system.
        /// Most other code should be put in Cleanup(), which is called regardless of whether the object
        /// is destroyed regularly or returned to the pooling system.
        /// </summary>
        public virtual void OnReturn()
        {
            TryCleanup();
        }

        /// <summary>
        /// Trigger a call to Initialize() if required.
        /// Should be called instead of invoking Initialize() directly.
        /// </summary>
        private void TryInitialize()
        {
            if ( !hasRunAwake )
            {
                // Don't run this if we haven't run Awake() yet
                return;
            }

            if ( !IsInitialized )
            {
                Initialize();
                IsInitialized = true;
            }
        }

        /// <summary>
        /// Trigger a call to Cleanup() if required.
        /// Should be called instead of invoking Cleanup() directly.
        /// </summary>
        private void TryCleanup()
        {
            if ( IsInitialized )
            {
                Cleanup();
                IsInitialized = false;
            }
        }

        /// <summary>
        /// Called when the behaviour is instantiated (via Start()) or retrieved from the pooling system (via OnAcquire()).
        /// Should be used to configure state that is specific to a single pooled lifecycle.
        ///
        /// Note that for behaviours that were just instantiated by the pooling system,
        /// this will be invoked BEFORE Start() gets called, and thus we should not rely on data set in Start()
        /// during Initialize().
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Called when the behaviour is returned to the pooling system or destroyed.
        /// Should be used to dispose references and release memory that were allocated in Initialize()
        /// </summary>
        protected virtual void Cleanup() { }
    }
}

