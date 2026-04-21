using System.Collections.Generic;
using NUnit.Framework;
using Robotech.TBS.Combat;
using Robotech.TBS.Hex;

namespace Robotech.Tests.EditMode
{
    public class LineOfSightTests
    {
        private static System.Func<HexCoord, bool> Blockers(params HexCoord[] blocked)
        {
            var set = new HashSet<HexCoord>(blocked);
            return c => set.Contains(c);
        }

        [Test]
        public void SameHex_AlwaysTrue_EvenIfSelfMarkedBlocker()
        {
            var a = new HexCoord(0, 0);
            Assert.IsTrue(LineOfSight.HasLineOfSight(a, a, Blockers(a)));
        }

        [Test]
        public void Adjacent_AlwaysTrue_EvenIfEndpointsMarkedBlockers()
        {
            var a = new HexCoord(0, 0);
            var b = new HexCoord(1, 0);
            Assert.IsTrue(LineOfSight.HasLineOfSight(a, b, Blockers(a, b)));
        }

        [Test]
        public void Distance2_NoBlockers_ReturnsTrue()
        {
            var a = new HexCoord(0, 0);
            var b = new HexCoord(2, 0);
            Assert.IsTrue(LineOfSight.HasLineOfSight(a, b, Blockers()));
        }

        [Test]
        public void Distance2_MiddleBlocked_ReturnsFalse()
        {
            // Line (0,0) -> (2,0) passes through (1,0).
            var a = new HexCoord(0, 0);
            var b = new HexCoord(2, 0);
            var middle = new HexCoord(1, 0);
            Assert.IsFalse(LineOfSight.HasLineOfSight(a, b, Blockers(middle)));
        }

        [Test]
        public void Distance3_UnblockedIntermediates_ReturnsTrue()
        {
            var a = new HexCoord(0, 0);
            var b = new HexCoord(3, 0);
            Assert.IsTrue(LineOfSight.HasLineOfSight(a, b, Blockers()));
        }

        [Test]
        public void Distance3_OneIntermediateBlocked_ReturnsFalse()
        {
            var a = new HexCoord(0, 0);
            var b = new HexCoord(3, 0);
            // Intermediates are (1,0) and (2,0). Block only one.
            Assert.IsFalse(LineOfSight.HasLineOfSight(a, b, Blockers(new HexCoord(2, 0))));
        }

        [Test]
        public void EndpointsNeverBlockEvenWhenMarked()
        {
            // Target hex itself is a blocker (e.g., enemy on a hill). Attacker must still see it.
            var a = new HexCoord(0, 0);
            var b = new HexCoord(3, 0);
            Assert.IsTrue(LineOfSight.HasLineOfSight(a, b, Blockers(a, b)));
        }

        [Test]
        public void NullPredicate_ReturnsTrue()
        {
            var a = new HexCoord(0, 0);
            var b = new HexCoord(5, 0);
            Assert.IsTrue(LineOfSight.HasLineOfSight(a, b, (System.Func<HexCoord, bool>)null));
        }
    }
}
