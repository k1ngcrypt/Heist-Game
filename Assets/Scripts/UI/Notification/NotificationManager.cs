using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    [SerializeField] private NotificationText notificationPrefab;
    [SerializeField] private Transform spawnArea;
    [SerializeField] private int time = 3;
    
    public static NotificationManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SendNotification(string message, Color? color = null)
    {
        Color finalColor = color ?? Color.white;
        var notification = Instantiate(notificationPrefab, spawnArea);
        notification.Initialize(message, time, finalColor);
    }
}
