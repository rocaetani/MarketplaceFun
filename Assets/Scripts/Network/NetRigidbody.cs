﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Prototyping;


public class NetRigidbody : NetworkBehaviour
{
    private Rigidbody _rigidbody;
    private NetworkTransform _netTransform;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _netTransform = GetComponent<NetworkTransform>();

        #if UNITY_EDITOR
            if(_rigidbody == null)
            {
                Debug.LogError($"[{gameObject.name}::NetworkRigidbody]: Rigidbody not found!");
            }

            if(_netTransform == null)
            {
                Debug.LogError($"[{gameObject.name}::NetworkRigidbody]: NetworkTransform not found!");
            }
        #endif

        NetworkObject.DontDestroyWithOwner = true;
    }

    private void OnCollisionEnter(Collision other)
    {
        // Only run this if you are the player that collided with the object
        if(isActiveAndEnabled && !IsOwner && other.gameObject == NetworkController.SelfPlayer.gameObject)
        {
            // Disable Network transform while we wait for Onwership confirmation
            // so that the rigidbody reacts immediately
            _netTransform.enabled = false;
            RequestObjectOwnership_ServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestObjectOwnership_ServerRpc(ServerRpcParams rpcReceiveParams = default)
    {
        // print($"Onwership request, {rpcReceiveParams.Receive.SenderClientId}");

        NetworkObject.ChangeOwnership(rpcReceiveParams.Receive.SenderClientId);
        RespondObjectOwnership_ClientRpc(rpcReceiveParams.ReturnRpcToSender());
    }

    [ClientRpc]
    private void RespondObjectOwnership_ClientRpc(ClientRpcParams clientRpcParams = default)
    {
        _netTransform.enabled = true;
    }
}
