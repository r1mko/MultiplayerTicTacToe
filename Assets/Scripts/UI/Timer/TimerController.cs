using Unity.Netcode;
using UnityEngine;

public class TimerController : MonoBehaviour
{
    public static TimerController Singltone;

    private bool isActive = false;
    private double startTime;
    private double endTime;
    public double duration;

    private void Awake()
    {
        Singltone = this;
    }

    public void StartTime()
    {

        if (isActive)
        {
            return;
        }
        isActive = true;
        startTime = NetworkManager.Singleton.ServerTime.Time;
        endTime = startTime + duration;

        UIManager.Singletone.ShowTimerText();
    }

    public void EndTime()
    {
        isActive = false;
        UIManager.Singletone.HideTimerText();
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        
        double remainingTime = endTime - NetworkManager.Singleton.ServerTime.Time;
        if (remainingTime <= 0)
        {
            NetworkPlayer.Singletone.MoveToNextPlayerRpc();
            EndTime();
            return;
        }
        int secondsRemaining = Mathf.FloorToInt((float)remainingTime);
        UIManager.Singletone.SetTimerText(secondsRemaining);
    }
}
