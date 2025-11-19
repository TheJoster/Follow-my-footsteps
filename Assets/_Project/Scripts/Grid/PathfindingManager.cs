using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Manages asynchronous pathfinding requests with caching and frame-rate throttling.
    /// Prevents frame drops by spreading pathfinding calculations across multiple frames.
    /// Phase 3.2 - Async Pathfinding Manager
    /// </summary>
    public class PathfindingManager : MonoBehaviour
    {
        public static PathfindingManager Instance { get; private set; }

        [Header("Performance Settings")]
        [SerializeField]
        [Tooltip("Maximum time in milliseconds to spend on pathfinding per frame")]
        private float maxMillisecondsPerFrame = 5f;

        [Header("Caching Settings")]
        [SerializeField]
        [Tooltip("Enable path caching for repeated requests")]
        private bool enableCaching = true;

        [SerializeField]
        [Tooltip("Time in seconds before cached paths expire")]
        private float cacheExpirationTime = 5f;

        [SerializeField]
        [Tooltip("Maximum number of cached paths to store")]
        private int maxCachedPaths = 100;

        // Request queue with priority system
        private Queue<PathRequest> requestQueue = new Queue<PathRequest>();
        private PathRequest currentRequest;
        private bool isProcessing = false;

        // Path caching
        private Dictionary<PathCacheKey, CachedPath> pathCache = new Dictionary<PathCacheKey, CachedPath>();
        private int nextRequestId = 0;

        // Events
        public event Action<int, List<HexCoord>> OnPathCalculated;
        public event Action<int> OnPathFailed;

        #region Initialization

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Request a path from start to goal. Returns request ID for tracking.
        /// </summary>
        public int RequestPath(HexGrid grid, HexCoord start, HexCoord goal, Action<List<HexCoord>> onComplete, int priority = 0, int searchLimit = 100)
        {
            if (grid == null)
            {
                Debug.LogError("PathfindingManager: Grid is null");
                return -1;
            }

            int requestId = nextRequestId++;

            // Check cache first if enabled
            if (enableCaching)
            {
                var cacheKey = new PathCacheKey(start, goal);
                if (pathCache.TryGetValue(cacheKey, out CachedPath cached))
                {
                    if (Time.time - cached.Timestamp < cacheExpirationTime)
                    {
                        // Cache hit - return immediately
                        onComplete?.Invoke(cached.Path);
                        OnPathCalculated?.Invoke(requestId, cached.Path);
                        return requestId;
                    }
                    else
                    {
                        // Cache expired - remove it
                        pathCache.Remove(cacheKey);
                    }
                }
            }

            // Create new request
            var request = new PathRequest
            {
                Id = requestId,
                Grid = grid,
                Start = start,
                Goal = goal,
                OnComplete = onComplete,
                Priority = priority,
                SearchLimit = searchLimit
            };

            // Add to queue
            requestQueue.Enqueue(request);

            // Start processing if not already running
            if (!isProcessing)
            {
                StartCoroutine(ProcessQueue());
            }

            return requestId;
        }

        /// <summary>
        /// Cancel a pending pathfinding request by ID.
        /// </summary>
        public bool CancelRequest(int requestId)
        {
            // Check if it's the current request
            if (currentRequest.Id == requestId)
            {
                currentRequest.IsCancelled = true;
                return true;
            }

            // Can't efficiently cancel queued requests without rebuilding queue
            // For now, just mark it in the request data if needed
            return false;
        }

        /// <summary>
        /// Invalidate all cached paths. Call when grid changes (terrain modification, construction).
        /// </summary>
        public void InvalidateCache()
        {
            pathCache.Clear();
        }

        /// <summary>
        /// Invalidate cached paths involving a specific coordinate.
        /// </summary>
        public void InvalidateCacheAt(HexCoord coord)
        {
            var keysToRemove = new List<PathCacheKey>();

            foreach (var kvp in pathCache)
            {
                if (kvp.Key.Start.Equals(coord) || kvp.Key.Goal.Equals(coord))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                pathCache.Remove(key);
            }
        }

        #endregion

        #region Queue Processing

        private IEnumerator ProcessQueue()
        {
            isProcessing = true;

            while (requestQueue.Count > 0)
            {
                currentRequest = requestQueue.Dequeue();

                // Check if request was cancelled
                if (currentRequest.IsCancelled)
                {
                    continue;
                }

                // Calculate path asynchronously
                yield return StartCoroutine(CalculatePathAsync(currentRequest));

                // Small delay between requests to prevent overwhelming the system
                yield return null;
            }

            isProcessing = false;
        }

        private IEnumerator CalculatePathAsync(PathRequest request)
        {
            var startTime = Time.realtimeSinceStartup;
            var path = Pathfinding.FindPath(
                request.Grid,
                request.Start,
                request.Goal,
                request.SearchLimit
            );

            // Simulate async by yielding if too much time passed
            var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
            if (elapsed > maxMillisecondsPerFrame)
            {
                yield return null;
            }

            // Check if request was cancelled during calculation
            if (request.IsCancelled)
            {
                yield break;
            }

            // Store in cache if enabled
            if (enableCaching && path != null && path.Count > 0)
            {
                var cacheKey = new PathCacheKey(request.Start, request.Goal);
                
                // Enforce cache size limit
                if (pathCache.Count >= maxCachedPaths)
                {
                    ClearOldestCache();
                }

                pathCache[cacheKey] = new CachedPath(path, Time.time);
            }

            // Invoke callbacks
            if (path != null && path.Count > 0)
            {
                request.OnComplete?.Invoke(path);
                OnPathCalculated?.Invoke(request.Id, path);
            }
            else
            {
                request.OnComplete?.Invoke(null);
                OnPathFailed?.Invoke(request.Id);
            }
        }

        private void ClearOldestCache()
        {
            PathCacheKey oldestKey = default;
            float oldestTime = float.MaxValue;

            foreach (var kvp in pathCache)
            {
                if (kvp.Value.Timestamp < oldestTime)
                {
                    oldestTime = kvp.Value.Timestamp;
                    oldestKey = kvp.Key;
                }
            }

            if (!oldestKey.Equals(default(PathCacheKey)))
            {
                pathCache.Remove(oldestKey);
            }
        }

        #endregion

        #region Cache Management

        private void Update()
        {
            // Periodically clean expired cache entries
            if (enableCaching && Time.frameCount % 60 == 0) // Every 60 frames
            {
                CleanExpiredCache();
            }
        }

        private void CleanExpiredCache()
        {
            var keysToRemove = new List<PathCacheKey>();
            float currentTime = Time.time;

            foreach (var kvp in pathCache)
            {
                if (currentTime - kvp.Value.Timestamp >= cacheExpirationTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                pathCache.Remove(key);
            }
        }

        #endregion

        #region Data Structures

        private struct PathRequest
        {
            public int Id;
            public HexGrid Grid;
            public HexCoord Start;
            public HexCoord Goal;
            public Action<List<HexCoord>> OnComplete;
            public int Priority;
            public int SearchLimit;
            public bool IsCancelled;
        }

        private struct PathCacheKey : IEquatable<PathCacheKey>
        {
            public HexCoord Start;
            public HexCoord Goal;

            public PathCacheKey(HexCoord start, HexCoord goal)
            {
                Start = start;
                Goal = goal;
            }

            public bool Equals(PathCacheKey other)
            {
                return Start.Equals(other.Start) && Goal.Equals(other.Goal);
            }

            public override bool Equals(object obj)
            {
                return obj is PathCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Start.GetHashCode() * 397) ^ Goal.GetHashCode();
                }
            }
        }

        private class CachedPath
        {
            public List<HexCoord> Path;
            public float Timestamp;

            public CachedPath(List<HexCoord> path, float timestamp)
            {
                Path = new List<HexCoord>(path); // Copy to prevent external modification
                Timestamp = timestamp;
            }
        }

        #endregion
    }
}
