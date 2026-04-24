#nullable enable
using System;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Navigation;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// FSM context object shared by all pet states.
    /// </summary>
    public sealed class PetContext
    {
        public PetContext(
            PetRuntimeData runtimeData,
            PetStateValueSO config,
            INavigationService? navigationService = null,
            IFurnitureService? furnitureService = null,
            EventBus? eventBus = null,
            IPetCommandLinkService? commandLinkService = null)
        {
            RuntimeData = runtimeData;
            Config = config;
            NavigationService = navigationService;
            FurnitureService = furnitureService;
            EventBus = eventBus;
            CommandLinkService = commandLinkService;
        }

        public PetRuntimeData RuntimeData { get; }

        public PetStateValueSO Config { get; }

        public INavigationService? NavigationService { get; set; }

        public IFurnitureService? FurnitureService { get; set; }

        public EventBus? EventBus { get; set; }

        public IPetCommandLinkService? CommandLinkService { get; set; }

        public Action<Vector2>? ApplyPosition { get; set; }

        public float MoveSpeed { get; set; } = 2f;

        public bool IsSleeping => RuntimeData.CurrentState == SleepingState.StateName;

        public void EnterState(string stateName)
        {
            RuntimeData.CurrentState = stateName;
            RuntimeData.TimeInCurrentState = 0f;
        }

        public void Advance(float deltaTime)
        {
            RuntimeData.TimeInCurrentState += deltaTime;
            RuntimeData.RuntimeTimeSeconds += deltaTime;
        }
    }
}
