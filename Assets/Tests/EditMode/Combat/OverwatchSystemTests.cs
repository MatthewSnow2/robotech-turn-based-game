using NUnit.Framework;
using UnityEngine;
using Robotech.TBS.Bootstrap;
using Robotech.TBS.Combat;
using Robotech.TBS.Data;
using Robotech.TBS.Hex;
using Robotech.TBS.Systems;
using Robotech.TBS.Units;

namespace Robotech.TBS.Tests.EditMode.Combat
{
    /// <summary>
    /// EditMode tests for OverwatchSystem and the overwatch state on Unit.
    /// MapGenerator is intentionally passed as null — CombatResolver/LineOfSight treat null
    /// mapGen as "no obstruction", which keeps these tests focused on overwatch logic
    /// (eligibility, range, faction, single-use, post-fire cleanup) rather than LoS rules
    /// already covered by LineOfSightTests.
    /// </summary>
    [TestFixture]
    public class OverwatchSystemTests
    {
        private GameObject registryObj;

        [SetUp]
        public void SetUp()
        {
            // Unity invokes Awake when AddComponent is called in EditMode tests, which assigns
            // UnitRegistry.Instance. Each test gets a clean registry via DestroyImmediate in TearDown.
            registryObj = new GameObject("UnitRegistry");
            registryObj.AddComponent<UnitRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in Object.FindObjectsOfType<GameObject>())
            {
                Object.DestroyImmediate(obj);
            }
        }

        private WeaponDefinition Rifle(int rangeMin = 1, int rangeMax = 4, int damage = 30)
        {
            return DefinitionsFactory.CreateWeapon(
                "rifle", "Rifle", "ballistic",
                dmg: damage, salvo: 1, rmin: rangeMin, rmax: rangeMax, acc: 1.0f);
        }

        private UnitDefinition MakeDef(Faction faction, bool canOverwatch, int hp = 100, int armor = 0, WeaponDefinition[] weapons = null)
        {
            var def = DefinitionsFactory.CreateUnit(
                id: $"unit_{faction}", name: faction.ToString(), faction: faction, layer: UnitLayer.Ground,
                hp: hp, armor: armor, move: 3, vision: 2,
                weapons: weapons ?? new[] { Rifle() });
            def.canOverwatch = canOverwatch;
            return def;
        }

        private Unit Spawn(UnitDefinition def, HexCoord coord)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var unit = go.AddComponent<Unit>();
            unit.Init(def, coord, 1.0f);
            return unit;
        }

        // ---- IsEligibleOverwatcher --------------------------------------------------

        [Test]
        public void IsEligibleOverwatcher_TrueWhenAllConditionsMet()
        {
            var u = Spawn(MakeDef(Faction.RDF, canOverwatch: true), new HexCoord(0, 0));
            u.isOverwatching = true;
            Assert.IsTrue(OverwatchSystem.IsEligibleOverwatcher(u));
        }

        [Test]
        public void IsEligibleOverwatcher_FalseWhenCanOverwatchFlagOff()
        {
            var u = Spawn(MakeDef(Faction.RDF, canOverwatch: false), new HexCoord(0, 0));
            u.isOverwatching = true;
            Assert.IsFalse(OverwatchSystem.IsEligibleOverwatcher(u));
        }

        [Test]
        public void IsEligibleOverwatcher_FalseWhenNotInOverwatch()
        {
            var u = Spawn(MakeDef(Faction.RDF, canOverwatch: true), new HexCoord(0, 0));
            u.isOverwatching = false;
            Assert.IsFalse(OverwatchSystem.IsEligibleOverwatcher(u));
        }

        [Test]
        public void IsEligibleOverwatcher_FalseWhenAlreadyAttacked()
        {
            var u = Spawn(MakeDef(Faction.RDF, canOverwatch: true), new HexCoord(0, 0));
            u.isOverwatching = true;
            u.hasAttackedThisTurn = true;
            Assert.IsFalse(OverwatchSystem.IsEligibleOverwatcher(u));
        }

        // ---- SetOverwatch -----------------------------------------------------------

        [Test]
        public void SetOverwatch_RequiresCanOverwatchFlag()
        {
            var u = Spawn(MakeDef(Faction.RDF, canOverwatch: false), new HexCoord(0, 0));
            Assert.IsFalse(u.SetOverwatch());
            Assert.IsFalse(u.isOverwatching);
        }

        [Test]
        public void SetOverwatch_ConsumesAllMovement()
        {
            var u = Spawn(MakeDef(Faction.RDF, canOverwatch: true), new HexCoord(0, 0));
            u.NewTurn(); // fills movesLeft from definition
            Assert.Greater(u.movesLeft, 0, "preconditions");
            Assert.IsTrue(u.SetOverwatch());
            Assert.IsTrue(u.isOverwatching);
            Assert.AreEqual(0, u.movesLeft);
        }

        [Test]
        public void NewTurn_ResetsOverwatchAndAttackFlags()
        {
            var u = Spawn(MakeDef(Faction.RDF, canOverwatch: true), new HexCoord(0, 0));
            u.isOverwatching = true;
            u.hasAttackedThisTurn = true;
            u.NewTurn();
            Assert.IsFalse(u.isOverwatching);
            Assert.IsFalse(u.hasAttackedThisTurn);
        }

        // ---- TriggerOnMove ----------------------------------------------------------

        [Test]
        public void TriggerOnMove_FiresAtEnemyInRange()
        {
            var defender = Spawn(MakeDef(Faction.RDF, canOverwatch: true, armor: 0), new HexCoord(0, 0));
            defender.isOverwatching = true;

            var attacker = Spawn(MakeDef(Faction.Zentradi, canOverwatch: false, hp: 100), new HexCoord(2, 0));
            int hpBefore = attacker.currentHP;

            OverwatchSystem.TriggerOnMove(attacker, mapGen: null);

            Assert.Less(attacker.currentHP, hpBefore, "Overwatcher should have damaged the mover");
            Assert.IsFalse(defender.isOverwatching, "Overwatch should be one-shot");
            Assert.IsTrue(defender.hasAttackedThisTurn);
        }

        [Test]
        public void TriggerOnMove_DoesNotFireOnSameFaction()
        {
            var defender = Spawn(MakeDef(Faction.RDF, canOverwatch: true), new HexCoord(0, 0));
            defender.isOverwatching = true;

            var friendly = Spawn(MakeDef(Faction.RDF, canOverwatch: false), new HexCoord(2, 0));
            int hpBefore = friendly.currentHP;

            OverwatchSystem.TriggerOnMove(friendly, mapGen: null);

            Assert.AreEqual(hpBefore, friendly.currentHP, "Same-faction movers should not draw overwatch fire");
            Assert.IsTrue(defender.isOverwatching, "Overwatch should be preserved if no shot taken");
        }

        [Test]
        public void TriggerOnMove_DoesNotFireBeyondWeaponRange()
        {
            var defender = Spawn(
                MakeDef(Faction.RDF, canOverwatch: true, weapons: new[] { Rifle(rangeMin: 1, rangeMax: 2) }),
                new HexCoord(0, 0));
            defender.isOverwatching = true;

            // Mover sits at distance 5 — outside the overwatcher's max range of 2.
            var mover = Spawn(MakeDef(Faction.Zentradi, canOverwatch: false), new HexCoord(5, 0));
            int hpBefore = mover.currentHP;

            OverwatchSystem.TriggerOnMove(mover, mapGen: null);

            Assert.AreEqual(hpBefore, mover.currentHP, "Out-of-range overwatcher must not fire");
            Assert.IsTrue(defender.isOverwatching, "Overwatch should be preserved if no shot taken");
        }

        [Test]
        public void TriggerOnMove_OverwatcherFiresOnlyOncePerTurn()
        {
            var defender = Spawn(MakeDef(Faction.RDF, canOverwatch: true), new HexCoord(0, 0));
            defender.isOverwatching = true;

            var mover = Spawn(MakeDef(Faction.Zentradi, canOverwatch: false, hp: 500), new HexCoord(2, 0));
            int hpAfterFirst, hpBefore = mover.currentHP;

            OverwatchSystem.TriggerOnMove(mover, mapGen: null);
            hpAfterFirst = mover.currentHP;
            Assert.Less(hpAfterFirst, hpBefore);

            // Second trigger should be a no-op — overwatch was consumed.
            OverwatchSystem.TriggerOnMove(mover, mapGen: null);
            Assert.AreEqual(hpAfterFirst, mover.currentHP, "Already-fired overwatcher must not fire again");
        }

        [Test]
        public void TriggerOnMove_NullMover_ReturnsFalseSafely()
        {
            // Just make sure no NRE
            Assert.DoesNotThrow(() => OverwatchSystem.TriggerOnMove(null, mapGen: null));
        }
    }
}
