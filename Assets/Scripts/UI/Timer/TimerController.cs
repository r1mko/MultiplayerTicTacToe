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
        Debug.Log($"<color=green>[TimerController] Вызвали метод StartTime. startTime: {startTime}, endTime: {endTime}</color>");
    }

    public void EndTime()
    {
        isActive = false;
        Debug.Log("[TimerController] Вызвали метод EndTime");
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }
        if (NetworkManager.Singleton.ServerTime.Time >= endTime)
        {
            EndTime();
        }
    }
}
