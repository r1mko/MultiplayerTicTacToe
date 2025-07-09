using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SmileScreen: MonoBehaviour
{
    [SerializeField] private Transform spawnPlace;
    [SerializeField] List<SmileButton> smileButtonList;

    private void Start()
    {
        EventManager.Subscribe<OnDisableSmileButtonsEvent>((a)=>StartSmileCooldown());
    }

    private void StartSmileCooldown()
    {
        StartCoroutine(SmileCooldown());
    }

    public Transform GetParentPlace()
    {
        return spawnPlace;
    }

    private void AllButtonDisable()
    {
        foreach (var item in smileButtonList)
        {
            item.smileButton.interactable = false;
        }
    }

    private void AllButtonEnable()
    {
        foreach (var item in smileButtonList)
        {
            item.smileButton.interactable = true;
        }
    }

    public IEnumerator SmileCooldown()
    {

        AllButtonDisable();
        yield return new WaitForSeconds(3f);
        AllButtonEnable();
    }

    private void OnDestroy()
    {
        EventManager.Unsubscribe<OnDisableSmileButtonsEvent>((a) => StartSmileCooldown());
    }
}
