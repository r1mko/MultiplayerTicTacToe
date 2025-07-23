using System;
using System.Collections.Generic;
using Unity.Netcode;
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
        EventManager.Subscribe<LocalSmileSentEvent>(OnLocalSmileSent);
        EventManager.Subscribe<EnemySmileReceivedEvent>(OnEnemySmileReceived);
    }

    private void OnLocalSmileSent(LocalSmileSentEvent e)
    {
        SpawnSmile(e.Index, isLocal: true);

        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("NetworkManager не инициализирован!");
            return;
        }

        NetworkPlayer.Singletone.SendSmileClientRpc(e.Index, NetworkManager.Singleton.LocalClientId);
    }

    private void OnEnemySmileReceived(EnemySmileReceivedEvent e)
    {
        Debug.Log($"[Smile] Получили смайл от противника: {e.Index}");
        SpawnSmile(e.Index, isLocal: false);
    }

    public void SpawnSmile(int index, bool isLocal)
    {
        Transform spawnPlace = isLocal ? smileScreen.GetOurParentPlace() : smileScreen.GetOpParentPlace();
        var spawnedSmile = Instantiate(smilePrefabs[index], spawnPlace);
    }

    private void OnDestroy()
    {
        EventManager.Unsubscribe<LocalSmileSentEvent>(OnLocalSmileSent);
        EventManager.Unsubscribe<EnemySmileReceivedEvent>(OnEnemySmileReceived);
    }
}
