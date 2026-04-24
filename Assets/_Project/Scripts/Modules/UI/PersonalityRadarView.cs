#nullable enable
using GeminiLab.Core;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.UI
{
    /// <summary>
    /// Lightweight radar data provider for personality/status panel.
    /// </summary>
    public sealed class PersonalityRadarView : MonoBehaviour
    {
        [SerializeField] private Vector4 _radarValues;

        public Vector4 RadarValues => _radarValues;

        private void Update()
        {
            PetController? controller = null;
            if (!ServiceLocator.TryResolve(out controller) || controller is null)
            {
                controller = FindFirstObjectByType<PetController>();
            }

            if (controller?.RuntimeData is null)
            {
                return;
            }

            PetRuntimeData data = controller.RuntimeData;
            _radarValues = new Vector4(
                data.Mood / 100f,
                data.Energy / 100f,
                data.Satiety / 100f,
                data.WorkRequested ? 1f : 0f);
        }
    }
}
