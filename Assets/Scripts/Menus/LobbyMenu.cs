﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;

public class LobbyMenu : NetworkBehaviour
{
    // Events
    public delegate void OnEnterLobbyDelegate();
    public static event OnEnterLobbyDelegate OnEnterLobby;
    
    public delegate void OnStartMatchDelegate();
    public static event OnStartMatchDelegate OnStartMatch;

    public delegate void OnCancelMatchDelegate();
    public static event OnCancelMatchDelegate OnCancelMatch;

    //[SerializeField] private Text _playerList = null;
    [SerializeField] private Button _startGame = null;
    
    [SerializeField] public GameObject Nickname1;
    [SerializeField] public GameObject Nickname2;
    [SerializeField] public GameObject Nickname3;
    [SerializeField] public GameObject Nickname4;

    

    private void Awake()
    {
        // Opening Event
        NetworkController.OnConnected += openLobbyMenu;
    }

    private void OnDestroy()
    {
        // Opening Event
        NetworkController.OnConnected -= openLobbyMenu;
    }

    private void OnEnable()
    {
        _startGame.interactable = IsHost;
    }

    private void openLobbyMenu(bool isHost)
    {
        this.toggleMenuDelayed();
        OnEnterLobby?.Invoke();
    }


    // Button Actions
    public void startMatch()
    {
        if(IsHost)
        {
            OnStartMatch?.Invoke();
        }
    }

    public void cancelMatch() => OnCancelMatch?.Invoke();

    

}
