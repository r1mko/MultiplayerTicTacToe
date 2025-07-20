using Unity.Netcode;
using UnityEngine;

public class TimerController : MonoBehaviour
{
    public static TimerController Singletone;

    private bool isActive = false;
    private double startTime;
    private double endTime;
    public double duration;

    private void Awake()
    {
        Singletone = this;
    }

    public void StartTime()
    {
        if (isActive)
        {
            return;
        }

        isActive = true;
        startTime = GetTime();
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

        double remainingTime = endTime - GetTime();
        if (remainingTime <= 0)
        {
            NetworkPlayer.Singletone.MoveToNextPlayerRpc();
            EndTime();
            return;
        }

        int secondsRemaining = Mathf.FloorToInt((float)remainingTime);
        UIManager.Singletone.SetTimerText(secondsRemaining);
    }

    private double GetTime()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            double serverTime = NetworkManager.Singleton.ServerTime.Time;
            return serverTime;
        }
        else
        {
            double gameTime = Time.time;
            return gameTime;
        }
    }
}
