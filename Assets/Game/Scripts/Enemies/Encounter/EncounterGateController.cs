using UnityEngine;

[DisallowMultipleComponent]
public class EncounterGateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation Triggers")]
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string closeTrigger = "Close";

    [Header("State")]
    [SerializeField] private bool startsOpen = true;

    private int _openTriggerHash;
    private int _closeTriggerHash;
    private bool _isOpen;

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
        CacheHashes();
        _isOpen = startsOpen;
    }

    public void OpenGate()
    {
        if (animator == null || _isOpen)
            return;

        if (_closeTriggerHash != 0)
            animator.ResetTrigger(_closeTriggerHash);

        if (_openTriggerHash != 0)
            animator.SetTrigger(_openTriggerHash);

        _isOpen = true;
    }

    public void CloseGate()
    {
        if (animator == null || !_isOpen)
            return;

        if (_openTriggerHash != 0)
            animator.ResetTrigger(_openTriggerHash);

        if (_closeTriggerHash != 0)
            animator.SetTrigger(_closeTriggerHash);

        _isOpen = false;
    }

    public void SetGateOpen(bool open)
    {
        if (open)
            OpenGate();
        else
            CloseGate();
    }

    private void AutoWire()
    {
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    private void CacheHashes()
    {
        _openTriggerHash = string.IsNullOrWhiteSpace(openTrigger) ? 0 : Animator.StringToHash(openTrigger);
        _closeTriggerHash = string.IsNullOrWhiteSpace(closeTrigger) ? 0 : Animator.StringToHash(closeTrigger);
    }
}
