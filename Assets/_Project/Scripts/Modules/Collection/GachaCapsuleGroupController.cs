using UnityEngine;

public class GachaCapsuleGroupController : MonoBehaviour
{
    [SerializeField] private GachaCapsuleWiggle[] capsules;

    private void Awake()
    {
        if (capsules == null || capsules.Length == 0)
        {
            capsules = GetComponentsInChildren<GachaCapsuleWiggle>();
        }
    }

    public void StartRolling()
    {
        foreach (var capsule in capsules)
        {
            capsule.StartRolling();
        }
    }

    public void StopRolling()
    {
        foreach (var capsule in capsules)
        {
            capsule.StopRolling();
        }
    }
}
