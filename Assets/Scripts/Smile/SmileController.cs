using System;
using System.Collections.Generic;
using UnityEngine;

public class SmileController : MonoBehaviour
{
    public static SmileController Singltone;

    [SerializeField] private List<GameObject> smilePrefabs;

    [SerializeField] private SmileScreen smileScreen;

    private void Awake()
    {
        Singltone = this;
    }

    private void Start()
    {
        EventManager.Subscribe<SendSmileEvent>(SendSmile);
    }

    private void SendSmile(SendSmileEvent obj)
    {
        NetworkPlayer.Singletone.OnClickSmileRpc(obj.Index);
        Debug.Log("Вызвали метод SendSmile, отправили в нетворк плеер");
    }

    public void SpawnSmile(int index)
    {
        var spawnedSmile = Instantiate(smilePrefabs[index], smileScreen.GetParentPlace());
    }

    private void OnDestroy()
    {
        EventManager.Unsubscribe<SendSmileEvent>(SendSmile);
    }
}
