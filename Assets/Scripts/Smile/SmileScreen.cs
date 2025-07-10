using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SmileScreen: MonoBehaviour
{
    [SerializeField] private Transform ourSpawnPlace;
    [SerializeField] private Transform opSpawnPlace;
    [SerializeField] List<SmileButton> smileButtonList;

    private void Start()
    {
        EventManager.Subscribe<OnDisableSmileButtonsEvent>(HandleDisableSmileButtons);
    }

    private void HandleDisableSmileButtons(OnDisableSmileButtonsEvent e)
    {
        StartSmileCooldown();
    }

    private void StartSmileCooldown()
    {
        StartCoroutine(SmileCooldown());
    }

    public Transform GetOurParentPlace()
    {
        return ourSpawnPlace;
    }

    public Transform GetOpParentPlace()
    {
        return opSpawnPlace; 
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
        EventManager.Unsubscribe<OnDisableSmileButtonsEvent>(HandleDisableSmileButtons);
    }
}
