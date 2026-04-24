#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// A point where pet can interact with furniture.
    /// </summary>
    public sealed class InteractionAnchor : MonoBehaviour
    {
        [SerializeField] private bool _isAvailable = true;

        public bool IsAvailable => _isAvailable;

        public Vector2 WorldPosition => transform.position;

        public void SetAvailable(bool value)
        {
            _isAvailable = value;
        }
    }
}
