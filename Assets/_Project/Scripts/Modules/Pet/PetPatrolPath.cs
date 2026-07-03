#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Defines a patrol path for a pet to follow. Waypoints are stored as local positions
    /// relative to this GameObject's transform.
    /// Attach to a GameObject in the scene; use the PetPatrolPathEditor to edit in Scene View.
    /// </summary>
    public sealed class PetPatrolPath : MonoBehaviour
    {
        [SerializeField] private List<Vector2> _waypoints = new();
        [SerializeField] private bool _loop = true;
        [SerializeField] private float _waitTimeAtWaypoint = 2f;
        [SerializeField] private Color _gizmoColor = new(0.2f, 0.8f, 0.4f, 0.8f);
        [SerializeField] private float _gizmoRadius = 0.15f;

        public IReadOnlyList<Vector2> Waypoints => _waypoints;
        public bool Loop => _loop;
        public float WaitTimeAtWaypoint => _waitTimeAtWaypoint;

        public Vector2 GetWorldWaypoint(int index)
        {
            if (index < 0 || index >= _waypoints.Count)
                return transform.position;
            return (Vector2)transform.position + _waypoints[index];
        }

        public int GetWaypointCount() => _waypoints.Count;

        public void AddWaypoint(Vector2 localPosition)
        {
            _waypoints.Add(localPosition);
        }

        public void InsertWaypoint(int index, Vector2 localPosition)
        {
            _waypoints.Insert(Mathf.Clamp(index, 0, _waypoints.Count), localPosition);
        }

        public void RemoveWaypoint(int index)
        {
            if (index < 0 || index >= _waypoints.Count) return;
            _waypoints.RemoveAt(index);
        }

        public void MoveWaypoint(int index, Vector2 localPosition)
        {
            if (index < 0 || index >= _waypoints.Count) return;
            _waypoints[index] = localPosition;
        }

        public void ClearWaypoints()
        {
            _waypoints.Clear();
        }

        private void OnDrawGizmos()
        {
            if (_waypoints.Count == 0) return;

            Gizmos.color = _gizmoColor;

            for (int i = 0; i < _waypoints.Count; i++)
            {
                var worldPos = GetWorldWaypoint(i);
                Gizmos.DrawSphere(worldPos, _gizmoRadius);
            }

            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                Gizmos.DrawLine(GetWorldWaypoint(i), GetWorldWaypoint(i + 1));
            }

            if (_loop && _waypoints.Count > 1)
            {
                Gizmos.DrawLine(GetWorldWaypoint(_waypoints.Count - 1), GetWorldWaypoint(0));
            }
        }
    }
}
