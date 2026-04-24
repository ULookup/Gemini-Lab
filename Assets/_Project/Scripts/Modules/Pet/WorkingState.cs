#nullable enable
using GeminiLab.Core.FSM;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Placeholder externally-driven working state.
    /// </summary>
    public sealed class WorkingState : IState<PetContext>
    {
        public const string StateName = "Working";

        public string Name => StateName;

        public void Enter(PetContext context)
        {
            context.EnterState(StateName);
        }

        public void Tick(PetContext context, float deltaTime)
        {
            context.Advance(deltaTime);
            if (context.RuntimeData.TimeInCurrentState >= 2f)
            {
                context.RuntimeData.WorkRequested = false;
            }
        }

        public void FixedTick(PetContext context, float fixedDeltaTime)
        {
        }

        public void Exit(PetContext context)
        {
        }
    }
}
