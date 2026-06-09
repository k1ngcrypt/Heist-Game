using UnityEngine;

public class BloodPuddle : MonoBehaviour
{
    private void OnEnable()
    {
        AwarenessManager.Instance.RegisterCorpse(transform);
    }

    private void OnDisable()
    {
        AwarenessManager.Instance.UnregisterCorpse(transform);
    }
}
