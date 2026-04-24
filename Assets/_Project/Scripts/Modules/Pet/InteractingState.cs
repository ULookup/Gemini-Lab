#nullable enable
using GeminiLab.Core.FSM;
using GeminiLab.Modules.Furniture;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Applies furniture interaction effects.
    /// </summary>
    public sealed class InteractingState : IState<PetContext>
    {
        public const string StateName = "Interacting";

        public string Name => StateName;

        public void Enter(PetContext context)
        {
            context.EnterState(StateName);
            if (context.FurnitureService is not null &&
                context.FurnitureService.TryConsumeInteractionBuff(context.RuntimeData.TargetFurnitureId, out EnvironmentalBuff buff))
            {
                context.RuntimeData.Mood = Mathf.Clamp(context.RuntimeData.Mood + buff.MoodDelta, 0f, 100f);
                context.RuntimeData.Energy = Mathf.Clamp(context.RuntimeData.Energy + buff.EnergyDelta, 0f, 100f);
            }
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
