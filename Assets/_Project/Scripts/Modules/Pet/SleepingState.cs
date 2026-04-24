#nullable enable
using GeminiLab.Core.FSM;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Energy recovery state.
    /// </summary>
    public sealed class SleepingState : IState<PetContext>
    {
        public const string StateName = "Sleeping";

        public string Name => StateName;

        public void Enter(PetContext context)
        {
            context.EnterState(StateName);
        }

        public void Tick(PetContext context, float deltaTime)
        {
            context.Advance(deltaTime);
        }

        public void FixedTick(PetContext context, float fixedDeltaTime)
        {
        }

        public void Exit(PetContext context)
        {
        }
    }
}
