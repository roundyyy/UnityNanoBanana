using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Roundy.UnityBanana
{
    /// <summary>
    /// Simple editor coroutine runner that doesn't require the Editor Coroutines package.
    /// Uses EditorApplication.update to drive coroutines.
    /// </summary>
    public static class EditorCoroutineRunner
    {
        private static readonly Dictionary<int, CoroutineState> _runningCoroutines = new Dictionary<int, CoroutineState>();
        private static int _nextId = 0;
        private static bool _isUpdateRegistered = false;

        private class CoroutineState
        {
            public IEnumerator Coroutine;
            public object Owner;
            public bool IsRunning = true;
        }

        /// <summary>
        /// Starts a coroutine in the editor.
        /// </summary>
        public static int StartCoroutine(IEnumerator coroutine, object owner = null)
        {
            int id = _nextId++;

            var state = new CoroutineState
            {
                Coroutine = coroutine,
                Owner = owner,
                IsRunning = true
            };

            _runningCoroutines[id] = state;

            if (!_isUpdateRegistered)
            {
                EditorApplication.update += Update;
                _isUpdateRegistered = true;
            }

            return id;
        }

        /// <summary>
        /// Stops a running coroutine.
        /// </summary>
        public static void StopCoroutine(int id)
        {
            if (_runningCoroutines.ContainsKey(id))
            {
                _runningCoroutines[id].IsRunning = false;
                _runningCoroutines.Remove(id);
            }

            CleanupIfEmpty();
        }

        /// <summary>
        /// Stops all coroutines for a specific owner.
        /// </summary>
        public static void StopAllCoroutines(object owner)
        {
            var toRemove = new List<int>();

            foreach (var kvp in _runningCoroutines)
            {
                if (kvp.Value.Owner == owner)
                {
                    kvp.Value.IsRunning = false;
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                _runningCoroutines.Remove(id);
            }

            CleanupIfEmpty();
        }

        private static void Update()
        {
            var toRemove = new List<int>();

            foreach (var kvp in _runningCoroutines)
            {
                if (!kvp.Value.IsRunning)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                bool hasMore = false;

                try
                {
                    hasMore = kvp.Value.Coroutine.MoveNext();

                    // Handle nested coroutines
                    if (hasMore && kvp.Value.Coroutine.Current is IEnumerator nested)
                    {
                        // Run nested coroutine to completion
                        while (nested.MoveNext() && kvp.Value.IsRunning)
                        {
                            // If nested yields another enumerator, handle it
                            if (nested.Current is IEnumerator deepNested)
                            {
                                while (deepNested.MoveNext() && kvp.Value.IsRunning) { }
                            }
                        }
                        hasMore = kvp.Value.IsRunning;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EditorCoroutineRunner] Coroutine exception: {ex}");
                    hasMore = false;
                }

                if (!hasMore)
                {
                    kvp.Value.IsRunning = false;
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                _runningCoroutines.Remove(id);
            }

            CleanupIfEmpty();
        }

        private static void CleanupIfEmpty()
        {
            if (_runningCoroutines.Count == 0 && _isUpdateRegistered)
            {
                EditorApplication.update -= Update;
                _isUpdateRegistered = false;
            }
        }
    }
}
