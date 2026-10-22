using System.Collections.Generic;
using NUnit.Framework;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Tests
{
    public class CommandPipelineTests
    {
        private sealed class AddCmd : ICommand
        {
            public PlayerId PlayerId { get; }
            public int Amount;
            public AddCmd(int player, int amount) { PlayerId = new PlayerId(player); Amount = amount; }
        }

        private sealed class Unregistered : ICommand
        {
            public PlayerId PlayerId => new PlayerId(0);
        }

        private sealed class Counter
        {
            public int Value;
            public List<int> Log = new List<int>();
        }

        private sealed class Added : ISimEvent
        {
            public int Amount;
            public Added(int a) { Amount = a; }
        }

        private sealed class AddHandler : ICommandHandler<AddCmd>
        {
            private readonly Counter _c;
            public AddHandler(Counter c) { _c = c; }
            public CommandResult Validate(AddCmd cmd, SimWorld w) =>
                cmd.Amount < 0 ? CommandResult.Rejected(RejectReason.InvalidArgument, "negative") : CommandResult.Accepted();
            public void Apply(AddCmd cmd, SimWorld w)
            {
                _c.Value += cmd.Amount;
                _c.Log.Add(cmd.Amount);
                w.Raise(new Added(cmd.Amount));
            }
        }

        private sealed class LogSystem : ISimSystem
        {
            private readonly Counter _c;
            public LogSystem(Counter c) { _c = c; }
            public void Update(SimWorld w) { _c.Log.Add(-1); }
        }

        private static (SimWorld, Counter) Make()
        {
            var w = new SimWorld();
            var c = new Counter();
            w.RegisterHandler(new AddHandler(c));
            return (w, c);
        }

        [Test]
        public void SubmitDoesNotApplyUntilStep()
        {
            var (w, c) = Make();
            w.Submit(new AddCmd(1, 5));
            Assert.That(c.Value, Is.EqualTo(0));
            Assert.That(w.PendingCommandCount, Is.EqualTo(1));
            w.Step();
            Assert.That(c.Value, Is.EqualTo(5));
            Assert.That(w.PendingCommandCount, Is.EqualTo(0));
        }

        [Test]
        public void CommandsApplyInSubmissionOrderBeforeSystems()
        {
            var (w, c) = Make();
            w.AddSystem(new LogSystem(c));
            w.Submit(new AddCmd(2, 3));
            w.Submit(new AddCmd(1, 1));
            w.Submit(new AddCmd(2, 2));
            w.Step();
            Assert.That(c.Log, Is.EqualTo(new[] { 3, 1, 2, -1 }));
        }

        [Test]
        public void ValidateDoesNotChangeState()
        {
            var (w, c) = Make();
            Assert.That(w.Validate(new AddCmd(1, 4)).IsAccepted, Is.True);
            Assert.That(c.Value, Is.EqualTo(0));
            Assert.That(w.PendingCommandCount, Is.EqualTo(0));
            Assert.That(w.DrainEvents(), Is.Empty);
        }

        [Test]
        public void RejectionLeavesStateUnchangedAndRaisesEvent()
        {
            var (w, c) = Make();
            w.Submit(new AddCmd(1, -1));
            w.Step();
            Assert.That(c.Value, Is.EqualTo(0));
            var evts = w.DrainEvents();
            Assert.That(evts.Count, Is.EqualTo(1));
            var rej = (CommandRejectedEvent)evts[0];
            Assert.That(rej.Result.Reason, Is.EqualTo(RejectReason.InvalidArgument));
            Assert.That(rej.Result.Message, Is.EqualTo("negative"));
        }

        [Test]
        public void UnregisteredCommandIsRejected()
        {
            var (w, _) = Make();
            var r = w.Validate(new Unregistered());
            Assert.That(r.IsAccepted, Is.False);
            Assert.That(r.Reason, Is.EqualTo(RejectReason.UnknownCommand));
        }

        [Test]
        public void DrainEventsReturnsThenClears()
        {
            var (w, _) = Make();
            w.Submit(new AddCmd(1, 7));
            w.Step();
            var first = w.DrainEvents();
            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(((Added)first[0]).Amount, Is.EqualTo(7));
            Assert.That(w.DrainEvents(), Is.Empty);
        }

        [Test]
        public void PausedWorldDoesNotApplyCommandsUntilResumed()
        {
            var (w, c) = Make();
            w.Pause();
            w.Submit(new AddCmd(1, 5));
            w.Step(3);
            Assert.That(c.Value, Is.EqualTo(0));
            Assert.That(w.PendingCommandCount, Is.EqualTo(1));
            w.Resume();
            w.Step();
            Assert.That(c.Value, Is.EqualTo(5));
        }

        [Test]
        public void DuplicateHandlerRegistrationThrows()
        {
            var (w, c) = Make();
            Assert.Throws<System.InvalidOperationException>(() => w.RegisterHandler(new AddHandler(c)));
        }
    }
}
