# Promises
This library provides a system for [pooling](https://gameprogrammingpatterns.com/object-pool.html) Unity prefab instances. This can help improve performance in games by reducing the amount of time spent and memory allocated instantiating and destroying instances of the same prefab.

## Installation
We recommend you install the Object Pool library via [OpenUPM](https://openupm.com/packages/com.jonagill.promises/). Per OpenUPM's documentation:

1. Open `Edit/Project Settings/Package Manager`
2. Add a new Scoped Registry (or edit the existing OpenUPM entry) to read:
    * Name: `package.openupm.com`
    * URL: `https://package.openupm.com`
    * Scope(s): `com.jonagill.objectpool`
3. Click Save (or Apply)
4. Open Window/Package Manager
5. Click the + button
6. Select Add package by name...
6. Click Add

# Usage

## PrefabPoolCollection

To begin pooling objects, you must first construct a `PrefabPoolCollection`. This collection controls the lifecycle of the given pool. While it is totally fine to use a single `PrefabPoolCollection` for your entire game, you may want have separate systems maintain separate pools so that they can be torn down and cleaned up at appropriate times in your game loop.

```

private class ProjectileManager {

    [SerializeField] private Transform projectilePrefab;
    [SerializeField] private Transform altProjectilePrefab;

    private PrefabPoolCollection projectilePoolCollection;

    private void Awake() {
        // Construct our pool, providing the transform under which disabled instances should be parented
        // This should be a transform that rarely moves, as moving a hierarchy with a lot of objects parented
        // underneath it can be slow
        projectilePoolCollection = new PrefabPoolCollection( transform );

        // Prewarming the collection allocates the given number of instances immediately
        // By prewarming during a loading screen or other safe period, you can avoid framerate drops 
        // from instantiating objects during gameplay
        projectilePoolCollection.PreWarm( projectilePrefab, 32 );

        // Multiple prefabs can be managed by the same PrefabPoolCollection
        projectilePoolCollection.PreWarm( altProjectilePrefab, 8 );
    }

    private void OnDestroy() {
        // The pool should be disposed when no longer in use so that it can release resources
        // and ensure correct cleanup of all of the objects that it spawned
        projectilePoolCollection?.Dispose();
        projectilePoolCollection = null;
    }

    public Transform GetProjectile(Vector3 position, Quaternion rotation) {
        // Aquire an instance from the pool
        // If there are unused instances in reserve, one will be re-enabled and returned
        // If no reserve instances are available, a new instance will be constructed instead
        PooledInstance<Transform> projectileInstance = projectilePoolCollection.Acquire(projectilePrefab, position, rotation);

        return projectileInstance;
    }

}

```

Under the hood, a `PrefabPoolCollection` works by constructing a disabled `GameObject` under the provided root transform. When objects are returned to the pool, they are disabled and parented under the disabled root object to keep them out of the rest of the scene hierarchy.

## PooledInstance<T>

Pooled instances are returned as `PooledInstance<T>` objects when acquired. This wrapper is only valid until the underlying instance is returned to the pool or destroyed, allowing you to verify that an acquired object is still valid to operate on.

Specifically, the same way that `UnityEngine.Object` overrides the equality operator to allow `object == null` to return true when an object has been destroyed, `PooledInstance<T>` will return true for `instance == null` when the underlying object has been destroyed _or returned to the pool_.  This means you can use the usual pattern of Unity-style nullity checks for pooled instances to verify that your pooled instance is still valid to operate on.

While you just assign the results of `PrefabPoolCollection.Acquire()` directly to the underlying component type and ignore `PooledInstance<T>` entirely, this can open the door to bugs. Specifically, ignoring `PooledInstance<T>` means your code will not know when the instance is returned to the pool by an external system. This can lead to errors where multiple pieces of code think they own the same underlying component instance at the same time.

## PooledObject

GameObjects acquired from the pool will have a special `PooledObject` component added to them automatically. This component tracks metadata about the pool that the object came from. It also exposes several important static helper methods.

### `PooledObject.ReturnOnDestroy<T>(T component)` and `PooledObject.ReturnOrDestroy(GameObject go)`

These methods will check whether the provided target object was acquired from a pool. If it was, the object gets returned to the pool. If it wasn't (or if the pool no longer exists), the object is simply destroyed via `Object.Destroy()`.

This is the best way to return objects to their pool, as it handles lots of common failure cases for you. It also means you can replace all calls to `Object.Destroy()` for GameObjects with `PooledObject.ReturnOrDestroy()` and it will automatically handle pooling for you.

### `PooledObject.NotifyParentDestroyed(Transform parent)`

Pooled objects frequently get parented under other Transform hierarchies. For instance, a pooled effect might get parented to an enemy that it should follow around. Unfortunately, this means that when the Transform your instance is parented to gets destroyed, your pooled instance will be destroyed as well. The `ObjectPool` system should handle this gracefully, but it defeats the purpose of pooling if your pooled instances are constantly getting destroyed and new ones must be re-instantiated.

Unity does not provide a good mechanism for automatically detecting that a parent Transform is getting destroyed. However, if you have objects that you know are frequently destroyed with pooled objects parented to them, you can notify the pooling system that your hierarchy will be destroyed with the method `PooledObject.NotifyParentDestroyed()`. Any pooled objects parented to that hierarchy will then automatically un-parent themselves and return to the pool instead of being destroyed.

Note that this must be called _before_ your hierarchy is destroyed -- `OnDisable()` or `OnDestroy()` are too late in the process and Unity will print errors if you attempt to unparent objects while they are running. Instead, you must invoke the notification function before triggering any destruction. For instance, if our enemy destroyed itself after being killed, you might have a function that looks something like:

```

public void Die() {
    PooledObject.NotifyParentDestroyed(transform);
    Destroy(gameObject);
}

```


## IPooledComponent

Acquiring and returning instances to the pool essentially creates a new secondary lifecycle for those objects beyond the normal lifecycle of `Awake()` through `Destroy()`. It is common to want to re-initialize an object each time it is acquired from the pool and to tear it down again each time it is returned to the pool.

The `IPooledComponent` interface exists to enable this. When an instance is acquired from the pool, any behaviours on its root GameObject that implement `IPooledComponent` will have their `OnAcquire()` method called. When that instance is returned to the pool, its `OnReturn()` method will be called.

### Timing issues

This new pooled lifecycle can create some complexities around timing and code re-use. For instance, the first time `OnAcquire()` is run, `Start()` will not have run (since the instance will have only just been instantiated). This can mean data that is not initialized until `Start()` will not exist the first time `OnAcquire()` is run.

Similarly, if your object is destroyed without being returned to the pool (perhaps due to a scene unloading or its parent being destroyed without invoking `PooledObject.NotifyParentDestroyed()`), `OnReturn()` will not get invoked. This could mean that resources allocated during `OnAcquire()` do not get released properly.

Because of this, it is often a good idea to invoke the same code multiple places, such as running initialization code in `Start()` the first time your object is created and then in `OnAcquire()` on subsequent acquisitions. Similarly, you may want to release resources in both `OnReturn()` and `OnDestroy()` to guarantee they are released regardless of how your instance is disabled.

This kind of lifecycle boilerplate can be finnicky and error-prone, so it is often a good idea to create a base class to guarantee this flow for you. `PooledBehaviour.cs` is provided as an example of one potential approach to this problem.

# Additional helpers
Some additional helper classes are present in the package but not required for basic usage:

## Global Pool

`GlobalPool` is a singleton MonoBehaviour that initializes a single generic `PrefabPoolCollection` and exposes methods for accessing it. This allows any code to easily acquire prefab instances from the shared pool at the cost of some control over the lifecycle of that pool and its instantiated objects.

## Pooled Behaviour

`PooledBehaviour` is an abstract base class that pooled components can be derived from. It is one example of how to solve the timing issues inherent in the distinction of the separate object and pooling lifecycles described above by providing `Initialize()` and `Cleanup()` methods that are guaranteed to be called when an object is acquired and when it is destroyed or returned to the pool.

While this class can be inherited from directly in production code, it is more intended as an example of one way to approach this timing issue that can be adapted to fit your existing classes and inheritance hierarchy.