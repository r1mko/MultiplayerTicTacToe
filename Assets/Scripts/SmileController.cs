using System;
using System.Collections.Generic;
using UnityEngine;

public class SmileController : MonoBehaviour
{
    public static SmileController Singltone;

    [SerializeField] private List<GameObject> smilePrefabs;

    [SerializeField] private SmileScreen smileScreen;

    private Action<int> OnSendSmile;

    private void Awake()
    {
        Singltone = this;
    }

    private void Start()
    {
        OnSendSmile += SendSmile;
        smileScreen.Init(OnSendSmile);
    }
    public void SpawnSmile(int index)
    {
        var spawnedSmile = Instantiate(smilePrefabs[index], smileScreen.GetParentPlace());
    }

    public void SendSmile(int index)
    {
        Debug.Log("Вызвали метод SendSmile, отправили в нетворк плеер");
        NetworkPlayer.Singletone.OnClickSmileRpc(index);
    }

    private void OnDestroy()
    {
        OnSendSmile -= SendSmile;
    }
}
