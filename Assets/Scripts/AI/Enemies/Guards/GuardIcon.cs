using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class GuardIcon : MonoBehaviour
{
    private readonly int stateHash = Animator.StringToHash("Icon");
    private Animator animator;
    
    void Awake() {
        animator = GetComponent<Animator>();
    }

    public void HideIcon() {
        animator.SetInteger(stateHash, 0);
    }

    public void TriggerSuspicious() {
        animator.SetInteger(stateHash, 1);
    }
    
    public void TriggerAlerted() {
        animator.SetInteger(stateHash, 2);
    }
}