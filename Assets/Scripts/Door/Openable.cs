using UnityEngine;

public class Openable : MonoBehaviour, IDoorOpenBehavior
{
    [SerializeField] private bool isOpen;

    public bool IsOpen => isOpen;

    public void OpenDoor()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
    }

    public void CloseDoor()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
    }
}
