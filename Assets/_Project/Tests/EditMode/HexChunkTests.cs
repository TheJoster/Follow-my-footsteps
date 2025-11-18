using NUnit.Framework;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for HexChunk class.
    /// Tests chunk management, cell storage, and lifecycle.
    /// </summary>
    public class HexChunkTests
    {
        [Test]
        public void Constructor_SetsChunkCoord()
        {
            var chunkCoord = new HexCoord(2, -1);
            
            var chunk = new HexChunk(chunkCoord);
            
            Assert.AreEqual(chunkCoord, chunk.ChunkCoord);
        }

        [Test]
        public void Constructor_InitializesAsDirty()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            
            Assert.IsTrue(chunk.IsDirty);
        }

        [Test]
        public void AddCell_IncrementsCellCount()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            var cell = new HexCell(new HexCoord(0, 0));
            
            chunk.AddCell(cell);
            
            Assert.AreEqual(1, chunk.CellCount);
        }

        [Test]
        public void AddCell_SetsChunkReference()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            var cell = new HexCell(new HexCoord(0, 0));
            
            chunk.AddCell(cell);
            
            Assert.AreEqual(chunk, cell.Chunk);
        }

        [Test]
        public void AddCell_SetsDirtyFlag()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            chunk.IsDirty = false;
            
            chunk.AddCell(new HexCell(new HexCoord(0, 0)));
            
            Assert.IsTrue(chunk.IsDirty);
        }

        [Test]
        public void GetCell_ReturnsAddedCell()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            var coord = new HexCoord(5, 3);
            var cell = new HexCell(coord);
            chunk.AddCell(cell);
            
            var retrievedCell = chunk.GetCell(coord);
            
            Assert.AreEqual(cell, retrievedCell);
        }

        [Test]
        public void GetCell_ReturnsNullForNonexistentCell()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            
            var retrievedCell = chunk.GetCell(new HexCoord(99, 99));
            
            Assert.IsNull(retrievedCell);
        }

        [Test]
        public void HasCell_ReturnsTrueForExistingCell()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            var coord = new HexCoord(5, 3);
            chunk.AddCell(new HexCell(coord));
            
            Assert.IsTrue(chunk.HasCell(coord));
        }

        [Test]
        public void HasCell_ReturnsFalseForNonexistentCell()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            
            Assert.IsFalse(chunk.HasCell(new HexCoord(99, 99)));
        }

        [Test]
        public void RemoveCell_DecrementsCellCount()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            var coord = new HexCoord(5, 3);
            chunk.AddCell(new HexCell(coord));
            
            chunk.RemoveCell(coord);
            
            Assert.AreEqual(0, chunk.CellCount);
        }

        [Test]
        public void RemoveCell_ReturnsTrueIfCellWasRemoved()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            var coord = new HexCoord(5, 3);
            chunk.AddCell(new HexCell(coord));
            
            bool removed = chunk.RemoveCell(coord);
            
            Assert.IsTrue(removed);
        }

        [Test]
        public void RemoveCell_ReturnsFalseIfCellDoesNotExist()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            
            bool removed = chunk.RemoveCell(new HexCoord(99, 99));
            
            Assert.IsFalse(removed);
        }

        [Test]
        public void Activate_SetsIsActiveToTrue()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            
            chunk.Activate();
            
            Assert.IsTrue(chunk.IsActive);
        }

        [Test]
        public void Deactivate_SetsIsActiveToFalse()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            chunk.Activate();
            
            chunk.Deactivate();
            
            Assert.IsFalse(chunk.IsActive);
        }

        [Test]
        public void Clear_RemovesAllCells()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            chunk.AddCell(new HexCell(new HexCoord(0, 0)));
            chunk.AddCell(new HexCell(new HexCoord(1, 0)));
            chunk.AddCell(new HexCell(new HexCoord(2, 0)));
            
            chunk.Clear();
            
            Assert.AreEqual(0, chunk.CellCount);
        }

        [Test]
        public void Clear_SetsDirtyFlag()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            chunk.IsDirty = false;
            
            chunk.Clear();
            
            Assert.IsTrue(chunk.IsDirty);
        }

        [Test]
        public void GetAllCells_ReturnsAllAddedCells()
        {
            var chunk = new HexChunk(new HexCoord(0, 0));
            chunk.AddCell(new HexCell(new HexCoord(0, 0)));
            chunk.AddCell(new HexCell(new HexCoord(1, 0)));
            chunk.AddCell(new HexCell(new HexCoord(2, 0)));
            
            var allCells = chunk.GetAllCells();
            
            int count = 0;
            foreach (var cell in allCells)
            {
                count++;
            }
            Assert.AreEqual(3, count);
        }
    }
}
