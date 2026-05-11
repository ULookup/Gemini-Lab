#nullable enable

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Event payload emitted when pet FSM changes state.
    /// </summary>
    public readonly struct PetStateChangedEvent
    {
        public PetStateChangedEvent(string fromState, string toState)
        {
            FromState = fromState;
            ToState = toState;
        }

        public string FromState { get; }

        public string ToState { get; }
    }
}
