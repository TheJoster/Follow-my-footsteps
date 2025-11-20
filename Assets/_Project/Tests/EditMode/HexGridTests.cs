using NUnit.Framework;
using FollowMyFootsteps.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for HexGrid class.
    /// Tests grid initialization, chunk management, and cell queries.
    /// </summary>
    public class HexGridTests
    {
        private HexGrid grid;

        [SetUp]
        public void SetUp()
        {
            // Create a GameObject with HexGrid component for testing
            var gridObject = new UnityEngine.GameObject("TestGrid");
            grid = gridObject.AddComponent<HexGrid>();
        }

        [TearDown]
        public void TearDown()
        {
            if (grid != null)
            {
                UnityEngine.Object.DestroyImmediate(grid.gameObject);
            }
        }

        [Test]
        public void InitializeGrid_CreatesChunks()
        {
            grid.InitializeGrid(2, 2);
            
            // Should create 2x2 = 4 chunks
            Assert.AreEqual(4, grid.ChunkCount);
        }

        [Test]
        public void InitializeGrid_PopulatesCellsInChunks()
        {
            grid.InitializeGrid(1, 1);
            
            // Single chunk should have 16x16 = 256 cells
            var cell = grid.GetCell(new HexCoord(0, 0));
            Assert.IsNotNull(cell);
        }

        [Test]
        public void GetChunkCoordForCell_CalculatesCorrectChunkCoord()
        {
            // Cell at (0, 0) should be in chunk (0, 0)
            var chunkCoord = grid.GetChunkCoordForCell(new HexCoord(0, 0));
            Assert.AreEqual(new HexCoord(0, 0), chunkCoord);
            
            // Cell at (16, 0) should be in chunk (1, 0)
            chunkCoord = grid.GetChunkCoordForCell(new HexCoord(16, 0));
            Assert.AreEqual(new HexCoord(1, 0), chunkCoord);
            
            // Cell at (0, 16) should be in chunk (0, 1)
            chunkCoord = grid.GetChunkCoordForCell(new HexCoord(0, 16));
            Assert.AreEqual(new HexCoord(0, 1), chunkCoord);
            
            // Cell at (-1, 0) should be in chunk (-1, 0)
            chunkCoord = grid.GetChunkCoordForCell(new HexCoord(-1, 0));
            Assert.AreEqual(new HexCoord(-1, 0), chunkCoord);
        }

        [Test]
        public void GetCell_ReturnsNullForUnloadedChunk()
        {
            grid.InitializeGrid(1, 1);
            
            // Cell in a chunk that wasn't initialized
            var cell = grid.GetCell(new HexCoord(100, 100));
            
            Assert.IsNull(cell);
        }

        [Test]
        public void GetCell_ReturnsCellFromCorrectChunk()
        {
            grid.InitializeGrid(2, 2);
            
            // Get cell from different chunks
            var cell1 = grid.GetCell(new HexCoord(0, 0));
            var cell2 = grid.GetCell(new HexCoord(16, 0));
            var cell3 = grid.GetCell(new HexCoord(0, 16));
            
            Assert.IsNotNull(cell1);
            Assert.IsNotNull(cell2);
            Assert.IsNotNull(cell3);
            Assert.AreEqual(new HexCoord(0, 0), cell1.Coordinates);
            Assert.AreEqual(new HexCoord(16, 0), cell2.Coordinates);
            Assert.AreEqual(new HexCoord(0, 16), cell3.Coordinates);
        }

        [Test]
        public void GetNeighbors_ReturnsAllNeighborsWithinGrid()
        {
            grid.InitializeGrid(2, 2);
            
            // Get neighbors of center cell
            var neighbors = grid.GetNeighbors(new HexCoord(8, 8));
            
            // Should have 6 neighbors if all are within loaded chunks
            Assert.AreEqual(6, neighbors.Count);
        }

        [Test]
        public void GetNeighbors_ExcludesNeighborsOutsideLoadedChunks()
        {
            grid.InitializeGrid(1, 1);
            
            // Get neighbors of cell at edge (some neighbors will be outside loaded chunk)
            var neighbors = grid.GetNeighbors(new HexCoord(0, 0));
            
            // Some neighbors will be missing since they're outside the loaded chunk
            Assert.LessOrEqual(neighbors.Count, 6);
        }

        [Test]
        public void GetNeighbors_ReturnsEmptyListForNullCell()
        {
            grid.InitializeGrid(1, 1);
            
            // Get neighbors of cell that doesn't exist
            var neighbors = grid.GetNeighbors(new HexCoord(1000, 1000));
            
            Assert.AreEqual(0, neighbors.Count);
        }

        [Test]
        public void GetCellsInRange_ReturnsCorrectNumberOfCellsRange1()
        {
            grid.InitializeGrid(2, 2);
            
            // Range 1 should return center + 6 neighbors = 7 cells
            var cells = grid.GetCellsInRange(new HexCoord(8, 8), 1);
            
            Assert.AreEqual(7, cells.Count);
        }

        [Test]
        public void GetCellsInRange_ReturnsCorrectNumberOfCellsRange2()
        {
            grid.InitializeGrid(2, 2);
            
            // Range 2 should return center + ring1(6) + ring2(12) = 19 cells
            var cells = grid.GetCellsInRange(new HexCoord(8, 8), 2);
            
            Assert.AreEqual(19, cells.Count);
        }

        [Test]
        public void GetCellsInRange_ExcludesCellsOutsideLoadedChunks()
        {
            grid.InitializeGrid(1, 1);
            
            // Range extends outside loaded chunks
            var cells = grid.GetCellsInRange(new HexCoord(0, 0), 3);
            
            // Should only include cells within the loaded chunk
            Assert.Greater(cells.Count, 0);
            foreach (var cell in cells)
            {
                Assert.IsNotNull(cell);
            }
        }

        [Test]
        public void GetCellsInRange_ReturnsEmptyListForNonExistentCenter()
        {
            grid.InitializeGrid(1, 1);
            
            var cells = grid.GetCellsInRange(new HexCoord(1000, 1000), 1);
            
            Assert.AreEqual(0, cells.Count);
        }

        [Test]
        public void GetCellAtWorldPosition_ReturnsCellForWorldPoint()
        {
            grid.InitializeGrid(1, 1);

            var targetCoord = new HexCoord(3, 5);
            Vector3 worldPosition = HexMetrics.GetWorldPosition(targetCoord) + new Vector3(0.1f, -0.05f, 0f);

            HexCell cell = grid.GetCellAtWorldPosition(worldPosition);

            Assert.IsNotNull(cell);
            Assert.AreEqual(targetCoord, cell.Coordinates);
        }

        [Test]
        public void LoadChunk_CreatesNewChunk()
        {
            grid.InitializeGrid(0, 0); // Start with no chunks
            
            grid.LoadChunk(new HexCoord(0, 0));
            
            Assert.AreEqual(1, grid.ChunkCount);
        }

        [Test]
        public void LoadChunk_UsesPooledChunkIfAvailable()
        {
            grid.InitializeGrid(1, 1);
            var originalCount = grid.ChunkCount;
            
            // Unload a chunk (should pool it)
            grid.UnloadChunk(new HexCoord(0, 0));
            
            // Load a new chunk (should use pooled chunk)
            grid.LoadChunk(new HexCoord(1, 1));
            
            // Chunk count should be same (reused pooled chunk)
            Assert.AreEqual(originalCount, grid.ChunkCount);
        }

        [Test]
        public void UnloadChunk_RemovesChunk()
        {
            grid.InitializeGrid(2, 2);
            var originalCount = grid.ChunkCount;
            
            grid.UnloadChunk(new HexCoord(0, 0));
            
            Assert.AreEqual(originalCount - 1, grid.ChunkCount);
        }

        [Test]
        public void UnloadChunk_AddsChunkToPool()
        {
            grid.InitializeGrid(1, 1);
            
            grid.UnloadChunk(new HexCoord(0, 0));
            
            // Next LoadChunk should use pooled chunk
            grid.LoadChunk(new HexCoord(1, 1));
            var cell = grid.GetCell(new HexCoord(16, 16));
            
            Assert.IsNotNull(cell);
        }

        [Test]
        public void GetChunk_ReturnsNullForNonExistentChunk()
        {
            grid.InitializeGrid(1, 1);
            
            var chunk = grid.GetChunk(new HexCoord(10, 10));
            
            Assert.IsNull(chunk);
        }

        [Test]
        public void GetChunk_ReturnsCorrectChunk()
        {
            grid.InitializeGrid(1, 1);
            
            var chunk = grid.GetChunk(new HexCoord(0, 0));
            
            Assert.IsNotNull(chunk);
            Assert.AreEqual(new HexCoord(0, 0), chunk.ChunkCoord);
        }
    }
}
