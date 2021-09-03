﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

public class ItemGenerator : NetworkBehaviour
{
    private GameObject _itemPrefab;
    public int ItemTypeCode;
    private Action<GameObject> _onItemGenerated = null;
    private Interactable _interactScript = null;

    private void Awake()
    {
        _itemPrefab = ItemTypeList.ItemList[ItemTypeCode].ItemPrefab;

        _interactScript = gameObject.GetComponentInChildren<Interactable>();

        if(_interactScript == null)
        {
            return;
        }

        _interactScript.OnLookEnter += showButtonPrompt;
        _interactScript.OnLookExit += hideButtonPrompt;
        _interactScript.OnInteract += generateItem;
    }

    private void OnDestroy()
    {
        if(_interactScript == null)
        {
            return;
        }

        _interactScript.OnLookEnter -= showButtonPrompt;
        _interactScript.OnLookExit -= hideButtonPrompt;
        _interactScript.OnInteract -= generateItem;
    }

    private void showButtonPrompt(GameObject player)
    {
        Debug.Log($"[{gameObject.name}]: Showing button prompt");
    }

    private void hideButtonPrompt(GameObject player)
    {
        Debug.Log($"[{gameObject.name}]: Hiding button prompt");
    }

    private void generateItem(GameObject player)
    {
        Debug.Log($"[{gameObject.name}]: Trigger generate item");
    }

    public void GetItem(Action<GameObject> onItemGenerated)
    {
        _onItemGenerated = onItemGenerated;
        GenerateItem_ServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void GenerateItem_ServerRpc(ServerRpcParams rpcReceiveParams = default)
    {
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] {rpcReceiveParams.Receive.SenderClientId}
            }
        };

        GameObject generatedItem = Instantiate(_itemPrefab, Vector3.zero, Quaternion.identity);

        var item = generatedItem.GetComponent<Item>();

        item.ItemTypeCode = ItemTypeCode;

        var itemNetworkObject = generatedItem.GetComponent<NetworkObject>();

        itemNetworkObject.SpawnWithOwnership(rpcReceiveParams.Receive.SenderClientId, destroyWithScene: true);

        GenerateItem_ClientRpc(itemNetworkObject.PrefabHash, itemNetworkObject.NetworkObjectId, ItemTypeCode, clientRpcParams);
    }


    [ClientRpc]
    private void GenerateItem_ClientRpc(ulong prefabHash, ulong id, int itemTypeCode, ClientRpcParams clientRpcParams = default)
    {
        GameObject itemGenerated = NetworkItemManager.GetNetworkItem(prefabHash, id);

        if (itemGenerated != null)
        {
            itemGenerated.GetComponent<Item>().ItemTypeCode = itemTypeCode;
            _onItemGenerated?.Invoke(itemGenerated);
            _onItemGenerated = null;
        }
    }

}