using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveDraftArena
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Queue<T> pool = new Queue<T>();
        private readonly Transform parent;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;

        public ObjectPool(T prefab, Transform parent = null, Action<T> onGet = null, Action<T> onRelease = null, int initialSize = 10)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.onGet = onGet;
            this.onRelease = onRelease;

            // Pre-populate pool
            for (var i = 0; i < initialSize; i++)
            {
                var obj = CreateNewObject();
                pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }

            obj.gameObject.SetActive(true);
            onGet?.Invoke(obj);

            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null) return;

            onRelease?.Invoke(obj);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }

        private T CreateNewObject()
        {
            var obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            return obj;
        }

        public int PoolSize => pool.Count;
    }
}
