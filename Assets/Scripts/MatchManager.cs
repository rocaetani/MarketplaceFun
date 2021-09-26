﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    public GameObject MainPlayer;

    private void Awake()
    {
        Instance = this;
    }

    private List<GameObject> _players = new List<GameObject>(4);

    public IEnumerable<Player> Players { get => _players.Select(player => player.GetComponent<Player>()); }

    public GameObject Player1 { get => _players[0]; set => _players[0] = value; }
    public GameObject Player2 { get => _players[1]; set => _players[1] = value; }
    public GameObject Player3 { get => _players[2]; set => _players[2] = value; }
    public GameObject Player4 { get => _players[3]; set => _players[3] = value; }

    public void RegisterPlayer(Player player)
    {
        if(_players.Count <= 4)
        {
            _players.Add(player.gameObject);
            if(player.IsOwner)
            {
                MainPlayer = player.gameObject;
            }
        }
    }

    public void SpawnPlayers(Vector3 position1, Vector3 position2, Vector3 position3, Vector3 position4)
    {
        Player1.transform.position = position1;
        Player1.SetActive(true);

        Player2.transform.position = position2;
        Player2.SetActive(true);

        Player3.transform.position = position3;
        Player3.SetActive(true);

        Player4.transform.position = position4;
        Player4.SetActive(true);
    }

    public bool IsMainPlayer(GameObject player)
    {
        return MainPlayer.Equals(player);
    }

    public Player GetPlayerByClientID(ulong ID)
    {
        return Players.FirstOrDefault(player => player.OwnerClientId == ID);
    }
}
