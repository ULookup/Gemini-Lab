#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.Pet
{
    public interface IPetCommandLinkService
    {
        string RequestWork(bool forceWake);

        bool TryDequeue(out PetCommand command);
    }

    public readonly struct PetCommand
    {
        public PetCommand(string traceId, bool forceWake)
        {
            TraceId = traceId;
            ForceWake = forceWake;
        }

        public string TraceId { get; }
        public bool ForceWake { get; }
    }

    /// <summary>
    /// Lightweight command queue with traceable IDs.
    /// </summary>
    public sealed class PetCommandLinkService : IPetCommandLinkService
    {
        private readonly Queue<PetCommand> _commands = new();

        public string RequestWork(bool forceWake)
        {
            string traceId = Guid.NewGuid().ToString("N");
            _commands.Enqueue(new PetCommand(traceId, forceWake));
            return traceId;
        }

        public bool TryDequeue(out PetCommand command)
        {
            if (_commands.Count == 0)
            {
                command = default;
                return false;
            }

            command = _commands.Dequeue();
            return true;
        }
    }
}
