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

    public float MinY
    {
        get
        {
            EnsureSetup();
            if (rangeTrigger == null) return transform.position.y;
            return rangeTrigger.bounds.min.y;
        }
    }

    public float MaxY
    {
        get
        {
            EnsureSetup();
            if (rangeTrigger == null) return transform.position.y;
            return rangeTrigger.bounds.max.y;
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

    public float ClampY(float y, float edgePadding = 0f)
    {
        float min = MinY + Mathf.Max(0f, edgePadding);
        float max = MaxY - Mathf.Max(0f, edgePadding);
        if (min > max)
        {
            float center = (MinY + MaxY) * 0.5f;
            return center;
        }
        return Mathf.Clamp(y, min, max);
    }

    public bool ContainsY(float y, float edgePadding = 0f)
    {
        float min = MinY + Mathf.Max(0f, edgePadding);
        float max = MaxY - Mathf.Max(0f, edgePadding);
        return y >= min && y <= max;
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
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 1f);
        Vector3 center = new Vector3((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f, 0f);
        Vector3 size = new Vector3(Mathf.Abs(MaxX - MinX), Mathf.Abs(MaxY - MinY), 0f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
