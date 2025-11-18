using NUnit.Framework;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for HexCoord struct.
    /// Tests axial coordinate functionality, equality, and operators.
    /// </summary>
    public class HexCoordTests
    {
        [Test]
        public void Constructor_SetsQAndR()
        {
            var coord = new HexCoord(3, -2);
            
            Assert.AreEqual(3, coord.q);
            Assert.AreEqual(-2, coord.r);
        }

        [Test]
        public void SProperty_CalculatesCorrectly()
        {
            var coord = new HexCoord(1, 2);
            
            // s = -q - r = -1 - 2 = -3
            Assert.AreEqual(-3, coord.s);
        }

        [Test]
        public void SProperty_SatisfiesCubeConstraint()
        {
            var coord = new HexCoord(5, -3);
            
            // Cube constraint: q + r + s = 0
            Assert.AreEqual(0, coord.q + coord.r + coord.s);
        }

        [Test]
        public void Equality_IdenticalCoords_AreEqual()
        {
            var coord1 = new HexCoord(2, 3);
            var coord2 = new HexCoord(2, 3);
            
            Assert.AreEqual(coord1, coord2);
            Assert.IsTrue(coord1 == coord2);
            Assert.IsFalse(coord1 != coord2);
        }

        [Test]
        public void Equality_DifferentCoords_AreNotEqual()
        {
            var coord1 = new HexCoord(2, 3);
            var coord2 = new HexCoord(2, 4);
            
            Assert.AreNotEqual(coord1, coord2);
            Assert.IsFalse(coord1 == coord2);
            Assert.IsTrue(coord1 != coord2);
        }

        [Test]
        public void GetHashCode_IdenticalCoords_HaveSameHash()
        {
            var coord1 = new HexCoord(7, -4);
            var coord2 = new HexCoord(7, -4);
            
            Assert.AreEqual(coord1.GetHashCode(), coord2.GetHashCode());
        }

        [Test]
        public void Addition_AddsQAndR()
        {
            var coord1 = new HexCoord(2, 3);
            var coord2 = new HexCoord(1, -1);
            
            var result = coord1 + coord2;
            
            Assert.AreEqual(3, result.q);
            Assert.AreEqual(2, result.r);
        }

        [Test]
        public void Subtraction_SubtractsQAndR()
        {
            var coord1 = new HexCoord(5, 2);
            var coord2 = new HexCoord(3, 1);
            
            var result = coord1 - coord2;
            
            Assert.AreEqual(2, result.q);
            Assert.AreEqual(1, result.r);
        }

        [Test]
        public void Multiplication_ScalesQAndR()
        {
            var coord = new HexCoord(2, -1);
            
            var result = coord * 3;
            
            Assert.AreEqual(6, result.q);
            Assert.AreEqual(-3, result.r);
        }

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            var coord = new HexCoord(4, -2);
            
            string result = coord.ToString();
            
            Assert.AreEqual("HexCoord(4, -2)", result);
        }
    }
}
