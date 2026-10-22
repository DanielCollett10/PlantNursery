namespace PlantNursery.Sim.Core
{
    /// <summary>Why a command was rejected.</summary>
    public enum RejectReason
    {
        /// <summary>Not rejected.</summary>
        None = 0,
        /// <summary>No handler is registered for the command type.</summary>
        UnknownCommand,
        /// <summary>The command's data is malformed.</summary>
        InvalidArgument,
        /// <summary>The target location or object is not valid.</summary>
        InvalidTarget,
        /// <summary>The player cannot afford it.</summary>
        InsufficientFunds,
        /// <summary>The player is not allowed to do this now.</summary>
        NotAllowed,
        /// <summary>Something else is in the way.</summary>
        Blocked
    }

    /// <summary>Outcome of validating or applying a command.</summary>
    public readonly struct CommandResult
    {
        /// <summary>True when the command was (or would be) accepted.</summary>
        public bool IsAccepted { get; }
        /// <summary>Rejection reason; <see cref="RejectReason.None"/> when accepted.</summary>
        public RejectReason Reason { get; }
        /// <summary>Human-readable detail; empty when accepted.</summary>
        public string Message { get; }

        private CommandResult(bool accepted, RejectReason reason, string message)
        {
            IsAccepted = accepted;
            Reason = reason;
            Message = message ?? string.Empty;
        }

        /// <summary>An accepted result.</summary>
        public static CommandResult Accepted() => new CommandResult(true, RejectReason.None, null);

        /// <summary>A rejected result.</summary>
        public static CommandResult Rejected(RejectReason reason, string message = null)
        {
            if (reason == RejectReason.None) throw new System.ArgumentException("A rejection needs a reason.", nameof(reason));
            return new CommandResult(false, reason, message);
        }
    }
}
