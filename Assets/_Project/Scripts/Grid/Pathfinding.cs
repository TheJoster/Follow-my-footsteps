using System.Collections.Generic;
using UnityEngine;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// A* pathfinding implementation for hex grid navigation.
    /// Finds optimal paths around unwalkable terrain.
    /// Phase 2.2+ enhancement for player movement.
    /// </summary>
    public static class Pathfinding
    {
        private class PathNode
        {
            public HexCoord Coord;
            public PathNode Parent;
            public int GCost; // Distance from start
            public int HCost; // Heuristic distance to goal
            public int FCost => GCost + HCost;

            public PathNode(HexCoord coord)
            {
                Coord = coord;
            }
        }

        /// <summary>
        /// Find a path from start to goal using A* algorithm.
        /// Returns null if no path exists.
        /// </summary>
        public static List<HexCoord> FindPath(HexGrid grid, HexCoord start, HexCoord goal, int maxDistance = 999)
        {
            if (grid == null)
            {
                // Grid not ready - return silently
                return null;
            }

            HexCell startCell = grid.GetCell(start);
            HexCell goalCell = grid.GetCell(goal);

            if (startCell == null || goalCell == null)
            {
                // Cells not loaded yet - return silently
                // This is normal during initialization
                return null;
            }

            // Goal must be walkable
            if (goalCell.Terrain != null && !goalCell.Terrain.IsWalkable)
            {
                // Don't log - this is a common query case (hovering over water, etc.)
                return null;
            }

            // Start and goal are the same
            if (start.Equals(goal))
            {
                return new List<HexCoord> { start };
            }

            var openSet = new List<PathNode>();
            var closedSet = new HashSet<HexCoord>();
            var allNodes = new Dictionary<HexCoord, PathNode>();

            var startNode = new PathNode(start)
            {
                GCost = 0,
                HCost = HexMetrics.Distance(start, goal)
            };

            openSet.Add(startNode);
            allNodes[start] = startNode;

            int iterations = 0;
            const int maxIterations = 1000; // Prevent infinite loops

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                // Find node with lowest FCost
                PathNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < currentNode.FCost ||
                        (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode.Coord);

                // Goal reached
                if (currentNode.Coord.Equals(goal))
                {
                    return RetracePath(startNode, currentNode);
                }

                // Check all neighbors
                List<HexCoord> neighbors = HexMetrics.GetNeighbors(currentNode.Coord);
                foreach (HexCoord neighborCoord in neighbors)
                {
                    // Skip if already evaluated
                    if (closedSet.Contains(neighborCoord))
                        continue;

                    HexCell neighborCell = grid.GetCell(neighborCoord);
                    if (neighborCell == null)
                        continue;

                    // Skip unwalkable terrain
                    if (neighborCell.Terrain != null && !neighborCell.Terrain.IsWalkable)
                        continue;

                    // Get movement cost for this terrain
                    int movementCost = neighborCell.Terrain != null ? neighborCell.Terrain.MovementCost : 1;
                    int newGCost = currentNode.GCost + movementCost;

                    // Check if we've exceeded max distance
                    if (newGCost > maxDistance)
                        continue;

                    // Get or create neighbor node
                    if (!allNodes.TryGetValue(neighborCoord, out PathNode neighborNode))
                    {
                        neighborNode = new PathNode(neighborCoord);
                        allNodes[neighborCoord] = neighborNode;
                    }

                    bool isInOpenSet = openSet.Contains(neighborNode);

                    // If this path to neighbor is better, or neighbor not in open set
                    if (newGCost < neighborNode.GCost || !isInOpenSet)
                    {
                        neighborNode.GCost = newGCost;
                        neighborNode.HCost = HexMetrics.Distance(neighborCoord, goal);
                        neighborNode.Parent = currentNode;

                        if (!isInOpenSet)
                        {
                            openSet.Add(neighborNode);
                        }
                    }
                }
            }

            // No path found
            Debug.LogWarning($"[Pathfinding] No path found from {start} to {goal} (iterations: {iterations})");
            return null;
        }

        /// <summary>
        /// Retrace path from goal back to start.
        /// </summary>
        private static List<HexCoord> RetracePath(PathNode startNode, PathNode endNode)
        {
            List<HexCoord> path = new List<HexCoord>();
            PathNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode.Coord);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Calculate the total movement cost of a path.
        /// </summary>
        public static int GetPathCost(HexGrid grid, List<HexCoord> path)
        {
            if (path == null || path.Count == 0)
                return 0;

            int totalCost = 0;
            foreach (HexCoord coord in path)
            {
                HexCell cell = grid.GetCell(coord);
                if (cell?.Terrain != null)
                {
                    totalCost += cell.Terrain.MovementCost;
                }
                else
                {
                    totalCost += 1; // Default cost
                }
            }

            return totalCost;
        }

        /// <summary>
        /// Get all cells reachable from start position within movement budget.
        /// Useful for highlighting valid move targets.
        /// </summary>
        public static HashSet<HexCoord> GetReachableCells(HexGrid grid, HexCoord start, int maxMovement)
        {
            var reachable = new HashSet<HexCoord>();
            var frontier = new Queue<(HexCoord coord, int cost)>();
            var visited = new Dictionary<HexCoord, int>();

            frontier.Enqueue((start, 0));
            visited[start] = 0;

            while (frontier.Count > 0)
            {
                var (current, currentCost) = frontier.Dequeue();

                foreach (HexCoord neighbor in HexMetrics.GetNeighbors(current))
                {
                    HexCell neighborCell = grid.GetCell(neighbor);
                    if (neighborCell == null)
                        continue;

                    // Skip unwalkable terrain
                    if (neighborCell.Terrain != null && !neighborCell.Terrain.IsWalkable)
                        continue;

                    int movementCost = neighborCell.Terrain != null ? neighborCell.Terrain.MovementCost : 1;
                    int newCost = currentCost + movementCost;

                    // Check if within budget and better than previous path
                    if (newCost <= maxMovement && (!visited.ContainsKey(neighbor) || newCost < visited[neighbor]))
                    {
                        visited[neighbor] = newCost;
                        reachable.Add(neighbor);
                        frontier.Enqueue((neighbor, newCost));
                    }
                }
            }

            return reachable;
        }
    }
}
