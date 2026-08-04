using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Events
{
    // Typed, allocation-light pub/sub. Instance rather than static so a run owns its own bus and
    // ending a run drops every subscription with it.
    public class EventBus
    {
        private readonly Dictionary<Type, Delegate> handlers = new();

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;

            Type key = typeof(T);
            handlers[key] = handlers.TryGetValue(key, out Delegate existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;

            Type key = typeof(T);
            if (!handlers.TryGetValue(key, out Delegate existing)) return;

            Delegate remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
                handlers.Remove(key);
            else
                handlers[key] = remaining;
        }

        public void Publish<T>(in T evt) where T : struct
        {
            if (!handlers.TryGetValue(typeof(T), out Delegate existing)) return;

            // Delegates are immutable, so this snapshot stays valid even if a handler
            // subscribes or unsubscribes while we are dispatching.
            Delegate[] invocationList = existing.GetInvocationList();

            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    // Caught per handler: one misbehaving augment effect must not stop the rest.
                    ((Action<T>)invocationList[i])(evt);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public void Clear() => handlers.Clear();
    }
}
