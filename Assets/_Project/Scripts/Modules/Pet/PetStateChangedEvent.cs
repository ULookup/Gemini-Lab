#nullable enable

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Event payload emitted when pet FSM changes state.
    /// </summary>
    public readonly struct PetStateChangedEvent
    {
        public PetStateChangedEvent(string fromState, string toState, PetId petId = PetId.Angel)
        {
            FromState = fromState;
            ToState = toState;
            PetId = petId;
        }

        public string FromState { get; }

        public string ToState { get; }

        public PetId PetId { get; }
    }
}
