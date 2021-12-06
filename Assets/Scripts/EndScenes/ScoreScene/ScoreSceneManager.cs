﻿
using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

public class ScoreSceneManager : NetworkBehaviour
{
    public float FirstPositionY;

    private const float markerScale = 0.3f;

    private bool _scoreFinished;
    
    private ScoreCanvas _scoreCanvas;

    private const string SCORE_CANVAS_TAG = "ScoreCanvas";
    
    private const string WIN_SCENE_NAME = "WinScene";
    
    private const string MARKER_CONTROLLER_TAG = "PointMarkerController";

    public Transform Camera;
    
    public int _playersReady;

    private ScoreController _scoreController;
    
    private PointMarkerController _pointMarkerController;


    public NetworkVariable<SerializedScorePointList> scoreList = new NetworkVariable<SerializedScorePointList>(
        new NetworkVariableSettings
        {
            ReadPermission = NetworkVariablePermission.Everyone,
            WritePermission = NetworkVariablePermission.ServerOnly
        }
    );
    public void Awake()
    {
        NetworkController.SelfPlayer.GetComponent<LobbyPosition>().EnterOnScore(Camera.position);
        
        _scoreCanvas = GameObject.FindGameObjectWithTag(SCORE_CANVAS_TAG).GetComponent<ScoreCanvas>();
        _pointMarkerController = GameObject.FindGameObjectWithTag(MARKER_CONTROLLER_TAG).GetComponent<PointMarkerController>();

        if (IsServer)
        {
            //NetworkController.OnOtherClientDisconnected +=
            _scoreFinished = false;
            _playersReady = 1;
            _scoreController = GameObject.FindGameObjectWithTag("ScoreController").GetComponent<ScoreController>();
            scoreList.Value = _scoreController.GetSerializedScore();
            StartShowPoints();
            _scoreCanvas.ShowButtonStart();
            
        }

        if (IsClient)
        {
            scoreList.OnValueChanged += StartShowPoints;
        }

    }

    private void Start()
    {
        if (IsServer)
        {
            if(_scoreController.VerifyWinner())
            {
                //Start Courotine to Win Scene
                //StartCoroutine(nameof(WinSceneCoroutine));
                GoToWinScene();
            }
        }
    }
    

    public void Update()
    {
        if (IsServer )
        {
            if (_playersReady == NetworkManager.ConnectedClientsList.Count & _scoreFinished)
            {
                _scoreCanvas.ActivateButtonStart();
            }
        }
    }

    
    private void StartShowPoints()
    {
        GenerateBeginMarkers();
        StartCoroutine(nameof(GenerateMarkersForPointType));
    }

    private void StartShowPoints(SerializedScorePointList prev, SerializedScorePointList pos)
    {
        GenerateBeginMarkers();
        StartCoroutine(nameof(GenerateMarkersForPointType));
    }

    private IEnumerator GenerateMarkersForPointType()
    {
        ScorePoints[] scorePointsList = scoreList.Value.Array;
        Dictionary<ulong, Player> localPlayers = NetworkController.GetLocalPlayers();

        bool haveScoreType;
        foreach (int scoreTypeId in ScoreConfig.ScoreTypeDictionary.Keys)
        {
            haveScoreType = false;
            //
            foreach (ScorePoints scorePoint in scorePointsList)
            {
                foreach (DescriptivePoints descriptivePoints in scorePoint.LastMatchPoints)
                {
                    if (descriptivePoints.ScoreTypeId == scoreTypeId)
                    {
                        if (!haveScoreType)
                        {
                            ScoreType scoreType = ScoreConfig.ScoreTypeDictionary[scoreTypeId];
                            _scoreCanvas.ShowScoreText(scoreType.Type, scoreType.ScoreColor.color);
                        
                            yield return new WaitForSeconds(0.5f);
                            haveScoreType = true;
                        }

                        int playerIndex = localPlayers[scorePoint.PlayerId]
                            .GetComponent<PlayerInfo>()
                            .PlayerData.Color;
                        Debug.Log("Generated " + descriptivePoints.ScoreTypeId + " Points:  " + descriptivePoints.Points + " --- at " + playerIndex);
                        
                        _pointMarkerController.SpawnMarker(playerIndex-1, descriptivePoints.ScoreTypeId, descriptivePoints.Points);
                        
                        if (IsServer)
                        {
                            _scoreController.AddToMainList(scorePoint.PlayerId, descriptivePoints);
                        }
                        
                        yield return new WaitForSeconds(.2f);
                    }
                    
                }
            }
            if(haveScoreType){
                yield return new WaitForSeconds(1f);
            }
            
        }
        
        if (IsClient & !IsHost)
        {
            _scoreCanvas.ShowButtonReady();
        }

        if (IsServer)
        {
            _scoreController.MoveToScoresToMainList();
            _scoreFinished = true;
        }

    }

    private void GenerateBeginMarkers()
    {
        ScorePoints[] scorePointsList = scoreList.Value.Array;

        float nextPositionY = 0;
        
        foreach (ScorePoints scorePoint in scorePointsList)
        {
            int playerIndex = NetworkController.GetPlayerByID(scorePoint.PlayerId).GetComponent<PlayerInfo>().PlayerData.Color; 
            foreach (DescriptivePoints descriptivePoints in scorePoint.PlayerPoints)
            {
                Debug.Log("Begin Generated " + descriptivePoints.ScoreTypeId + " Points:  " + descriptivePoints.Points + " --- at " + playerIndex);
                float halfMarkerScale = (markerScale + 0.1f * descriptivePoints.Points) / 2;
                nextPositionY += halfMarkerScale;
                _pointMarkerController.SpawnMarkerAt(playerIndex-1, descriptivePoints.ScoreTypeId, descriptivePoints.Points, FirstPositionY + nextPositionY);
                nextPositionY += halfMarkerScale;
            }
        }
    }



    public void IAmReady()
    {
        PlayerIsReady_ServerRpc();
        _scoreCanvas.InactivateButtonReady();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerIsReady_ServerRpc()
    {
        _playersReady += 1;
    }
    
    public void PlayerDisconnectedTurnIsReady()
    {
        _playersReady += 1;
    }

    public void StartNewMatch()
    {
        NetworkController.switchNetworkScene(SceneManager.MatchSceneTag);
    }

    IEnumerator WinSceneCoroutine()
    {
        yield return new WaitForSeconds(1f);
        GoToWinScene();
    }

    public void GoToWinScene()
    {
        NetworkController.switchNetworkScene(WIN_SCENE_NAME);
    }

}