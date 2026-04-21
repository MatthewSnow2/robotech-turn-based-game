using NUnit.Framework;
using Robotech.TBS.Hex;

namespace Robotech.Tests.EditMode
{
    public class HexMathTests
    {
        [Test]
        public void Sanity_Addition_Works()
        {
            Assert.AreEqual(4, 2 + 2);
        }

        [Test]
        public void LineBetween_SameHex_ReturnsSingleHex()
        {
            var line = HexMath.LineBetween(new HexCoord(0, 0), new HexCoord(0, 0));
            Assert.AreEqual(1, line.Count);
            Assert.AreEqual(new HexCoord(0, 0), line[0]);
        }

        [Test]
        public void LineBetween_Adjacent_ReturnsBothEndpoints()
        {
            var a = new HexCoord(0, 0);
            var b = new HexCoord(1, 0);
            var line = HexMath.LineBetween(a, b);
            Assert.AreEqual(2, line.Count);
            Assert.AreEqual(a, line[0]);
            Assert.AreEqual(b, line[1]);
        }

        [Test]
        public void LineBetween_AlongQAxis_ReturnsContiguousHexes()
        {
            var line = HexMath.LineBetween(new HexCoord(0, 0), new HexCoord(3, 0));
            Assert.AreEqual(4, line.Count);
            Assert.AreEqual(new HexCoord(0, 0), line[0]);
            Assert.AreEqual(new HexCoord(1, 0), line[1]);
            Assert.AreEqual(new HexCoord(2, 0), line[2]);
            Assert.AreEqual(new HexCoord(3, 0), line[3]);
        }

        [Test]
        public void LineBetween_LengthIsDistancePlusOne()
        {
            var a = new HexCoord(-2, 1);
            var b = new HexCoord(3, -1);
            var line = HexMath.LineBetween(a, b);
            Assert.AreEqual(a.Distance(b) + 1, line.Count);
            Assert.AreEqual(a, line[0]);
            Assert.AreEqual(b, line[line.Count - 1]);
        }

        [Test]
        public void LineBetween_SymmetricEndpoints()
        {
            var a = new HexCoord(0, 0);
            var b = new HexCoord(2, -2);
            var forward = HexMath.LineBetween(a, b);
            var reverse = HexMath.LineBetween(b, a);
            Assert.AreEqual(forward.Count, reverse.Count);
            Assert.AreEqual(a, forward[0]);
            Assert.AreEqual(b, reverse[0]);
        }
    }
}
