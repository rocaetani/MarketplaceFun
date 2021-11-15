﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class Interactable : NetworkBehaviour
{
    public const string TAG_NAME = "Interactable";
    public const string LAYER_NAME = "Interact";
    public const string UI_NAME = "InteractCanvas";

    private static int _layerMask = -1;
    public static int LAYER_MASK
    {
        get => _layerMask;
    }

    public GameObject InteractUI { get; private set; }
    private bool _configured = false;

    private void Awake()
    {
        if(_layerMask == -1)
        {
            _layerMask = 1 << LayerMask.NameToLayer(LAYER_NAME);
        }

        if(gameObject.tag != TAG_NAME || 1 << gameObject.layer != LAYER_MASK)
        {
            #if UNITY_EDITOR
                Debug.LogError($"[{gameObject.name}::Interactable]: Could not find suitable interactable gameobject. Tag: {gameObject.tag}, Layer: {LayerMask.LayerToName(gameObject.layer)}");
            #endif

            return;
        }

        InteractUI = gameObject.transform.Find(UI_NAME)?.gameObject;
        _configured = InteractUI != null;
    }

    public delegate void OnLookEnterDelegate(Player player, Collider enteredTrigger);
    public event OnLookEnterDelegate OnLookEnter;
    public void TriggerLookEnter(Player player, Collider enteredTrigger)
    {
        if(_configured && isActiveAndEnabled)
        {
            OnLookEnter?.Invoke(player, enteredTrigger);
        }
    }

    public delegate void OnLookExitDelegate(Player player, Collider exitedTrigger);
    public event OnLookExitDelegate OnLookExit;
    public void TriggerLookExit(Player player, Collider exitedTrigger)
    {
        if(_configured && isActiveAndEnabled)
        {
            OnLookExit?.Invoke(player, exitedTrigger);
        }
    }

    public delegate void OnInteractDelegate(Player player, Collider interactedTrigger);
    public event OnInteractDelegate OnInteract;
    public void TriggerInteract(Player player, Collider interactedTrigger)
    {
        if(_configured && isActiveAndEnabled)
        {
            OnInteract?.Invoke(player, interactedTrigger);
        }
    }

}