#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Ensures a placeholder pet host exists in empty scenes.
    /// </summary>
    public static class PetRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePetHost()
        {
            if (Object.FindFirstObjectByType<PetController>() is not null)
            {
                return;
            }

            GameObject pet = new("PetPlaceholder");
            pet.transform.position = Vector3.zero;
            pet.AddComponent<PetController>();
        }
    }
}
