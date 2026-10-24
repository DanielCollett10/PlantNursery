using System.Collections.Generic;
using NUnit.Framework;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Tests
{
    public class StateMachineTests
    {
        public enum S { Seed, Sprout, Mature, Dead }

        private bool _wet;
        private int _guardCalls;
        private List<(S from, S to)> _log;

        [SetUp]
        public void SetUp()
        {
            _wet = true;
            _guardCalls = 0;
            _log = new List<(S, S)>();
        }

        /// <summary>
        /// Plant-like table: Seed->Sprout is free, Sprout->Mature needs water, Mature->Dead and
        /// Sprout->Dead are free. Dead is terminal and there is no way back to Seed.
        /// </summary>
        private StateMachine<S> Build(S initial)
        {
            return new StateMachine<S>(initial, (from, to) => _log.Add((from, to)))
                .Allow(S.Seed, S.Sprout)
                .Allow(S.Sprout, S.Mature, () => { _guardCalls++; return _wet; })
                .Allow(S.Sprout, S.Dead)
                .Allow(S.Mature, S.Dead);
        }

        [TestCase(S.Seed, S.Sprout, true, TransitionResult.Applied)]
        [TestCase(S.Seed, S.Sprout, false, TransitionResult.Applied)]
        [TestCase(S.Sprout, S.Mature, true, TransitionResult.Applied)]
        [TestCase(S.Sprout, S.Mature, false, TransitionResult.GuardFailed)]
        [TestCase(S.Sprout, S.Dead, false, TransitionResult.Applied)]
        [TestCase(S.Mature, S.Dead, false, TransitionResult.Applied)]
        [TestCase(S.Seed, S.Mature, true, TransitionResult.Illegal)]
        [TestCase(S.Seed, S.Dead, true, TransitionResult.Illegal)]
        [TestCase(S.Mature, S.Seed, true, TransitionResult.Illegal)]
        [TestCase(S.Seed, S.Seed, true, TransitionResult.Illegal)]
        [TestCase(S.Dead, S.Seed, true, TransitionResult.Illegal)]
        [TestCase(S.Dead, S.Sprout, true, TransitionResult.Illegal)]
        public void TryTransition_ReportsOutcomeFromTable(S from, S to, bool wet, TransitionResult expected)
        {
            _wet = wet;
            var machine = Build(from);

            var result = machine.TryTransition(to);

            Assert.That(result, Is.EqualTo(expected));
            if (expected == TransitionResult.Applied)
            {
                Assert.That(machine.Current, Is.EqualTo(to));
                Assert.That(_log, Is.EqualTo(new List<(S, S)> { (from, to) }));
            }
            else
            {
                Assert.That(machine.Current, Is.EqualTo(from), "refused transition must not change state");
                Assert.That(_log, Is.Empty, "refused transition must not invoke the callback");
            }
        }

        [TestCase(S.Seed, S.Sprout, true, TransitionResult.Applied)]
        [TestCase(S.Sprout, S.Mature, true, TransitionResult.Applied)]
        [TestCase(S.Sprout, S.Mature, false, TransitionResult.GuardFailed)]
        [TestCase(S.Seed, S.Mature, true, TransitionResult.Illegal)]
        [TestCase(S.Dead, S.Sprout, true, TransitionResult.Illegal)]
        public void Check_ReportsOutcomeWithoutChangingState(S from, S to, bool wet, TransitionResult expected)
        {
            _wet = wet;
            var machine = Build(from);

            Assert.That(machine.Check(to), Is.EqualTo(expected));
            Assert.That(machine.Current, Is.EqualTo(from));
            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void Guard_IsNotEvaluatedForIllegalTransitions()
        {
            var machine = Build(S.Sprout);

            machine.TryTransition(S.Seed);

            Assert.That(_guardCalls, Is.EqualTo(0));
        }

        [Test]
        public void Guard_IsEvaluatedOnceForGuardedEntry()
        {
            _wet = false;
            var machine = Build(S.Sprout);

            machine.TryTransition(S.Mature);

            Assert.That(_guardCalls, Is.EqualTo(1));
        }

        [Test]
        public void Callback_SeesNewCurrentState()
        {
            S? seenDuringCallback = null;
            StateMachine<S> machine = null;
            machine = new StateMachine<S>(S.Seed, (from, to) => seenDuringCallback = machine.Current);
            machine.Allow(S.Seed, S.Sprout);

            machine.TryTransition(S.Sprout);

            Assert.That(seenDuringCallback, Is.EqualTo(S.Sprout));
        }

        [Test]
        public void Chain_RunsThroughLifecycleInOrder()
        {
            var machine = Build(S.Seed);

            Assert.That(machine.TryTransition(S.Sprout), Is.EqualTo(TransitionResult.Applied));
            Assert.That(machine.TryTransition(S.Mature), Is.EqualTo(TransitionResult.Applied));
            Assert.That(machine.TryTransition(S.Dead), Is.EqualTo(TransitionResult.Applied));
            Assert.That(machine.TryTransition(S.Sprout), Is.EqualTo(TransitionResult.Illegal));

            Assert.That(_log, Is.EqualTo(new List<(S, S)>
            {
                (S.Seed, S.Sprout), (S.Sprout, S.Mature), (S.Mature, S.Dead)
            }));
        }

        [Test]
        public void Restore_SetsStateWithoutCallbackOrChecks()
        {
            var machine = Build(S.Seed);

            machine.Restore(S.Mature);

            Assert.That(machine.Current, Is.EqualTo(S.Mature));
            Assert.That(_log, Is.Empty);
            Assert.That(machine.TryTransition(S.Dead), Is.EqualTo(TransitionResult.Applied));
        }

        [Test]
        public void Allow_DuplicatePair_Throws()
        {
            var machine = Build(S.Seed);

            Assert.Throws<System.InvalidOperationException>(() => machine.Allow(S.Seed, S.Sprout));
        }

        [Test]
        public void NullCallback_IsAllowed()
        {
            var machine = new StateMachine<S>(S.Seed).Allow(S.Seed, S.Sprout);

            Assert.That(machine.TryTransition(S.Sprout), Is.EqualTo(TransitionResult.Applied));
            Assert.That(machine.Current, Is.EqualTo(S.Sprout));
        }
    }
}
