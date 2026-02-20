using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class EnemyMoveRange2D : MonoBehaviour
{
    [Header("Range Shape (world-space from BoxCollider2D bounds)")]
    [SerializeField] private BoxCollider2D rangeTrigger;

    public float MinX
    {
        get
        {
            EnsureSetup();
            if (rangeTrigger == null) return transform.position.x;
            return rangeTrigger.bounds.min.x;
        }
    }

    public float MaxX
    {
        get
        {
            EnsureSetup();
            if (rangeTrigger == null) return transform.position.x;
            return rangeTrigger.bounds.max.x;
        }
    }

    public float ClampX(float x, float edgePadding = 0f)
    {
        float min = MinX + Mathf.Max(0f, edgePadding);
        float max = MaxX - Mathf.Max(0f, edgePadding);
        if (min > max)
        {
            float center = (MinX + MaxX) * 0.5f;
            return center;
        }
        return Mathf.Clamp(x, min, max);
    }

    public bool ContainsX(float x, float edgePadding = 0f)
    {
        float min = MinX + Mathf.Max(0f, edgePadding);
        float max = MaxX - Mathf.Max(0f, edgePadding);
        return x >= min && x <= max;
    }

    /// <summary>
    /// Calculates a backstep target on x and clamps it inside the movement range.
    /// facingRight=true means "back" is towards negative x.
    /// </summary>
    public float GetBackstepTargetX(float currentX, bool facingRight, float backstepDistance, float edgePadding = 0f)
    {
        float dir = facingRight ? -1f : 1f;
        float desiredX = currentX + dir * Mathf.Abs(backstepDistance);
        return ClampX(desiredX, edgePadding);
    }

    private void Reset()
    {
        EnsureSetup();
    }

    private void Awake()
    {
        EnsureSetup();
    }

    private void EnsureSetup()
    {
        if (rangeTrigger == null) rangeTrigger = GetComponent<BoxCollider2D>();
        if (rangeTrigger != null && !rangeTrigger.isTrigger)
            rangeTrigger.isTrigger = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float min = MinX;
        float max = MaxX;
        float y = rangeTrigger != null ? rangeTrigger.bounds.center.y : transform.position.y;

        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 1f);
        Gizmos.DrawLine(new Vector3(min, y, 0f), new Vector3(max, y, 0f));
        Gizmos.DrawWireSphere(new Vector3(min, y, 0f), 0.15f);
        Gizmos.DrawWireSphere(new Vector3(max, y, 0f), 0.15f);
    }
#endif
}
