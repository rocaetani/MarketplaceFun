﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI;
using UnityEngine;

public class ScoreController : MonoBehaviour
{

    //This class is used to guard the score information from each player
    //for the hole tournament, there for it is only instantiated on the Server
    private Dictionary<ulong, ScorePoints> _playerPoints;

    private ScoreAuditor _scoreAuditor;

    public int PointsToWin;

    public int RoundsLoosing;

    private const int FIRST_TO_CHECK_OUT_INDEX = 5;
    private const int CHECK_OUT_INDEX = 1;
    private const int ENEMY_HIT_INDEX = 0;

    private void Awake()
    {
        if(!NetworkController.IsServer)
        {
            Destroy(gameObject);
        }

        _playerPoints = new Dictionary<ulong, ScorePoints>();
        DontDestroyOnLoad(gameObject);
        InitiatePlayerPoints(NetworkController.SelfID);

        _scoreAuditor = GetComponent<ScoreAuditor>();

        NetworkController.OnOtherClientConnected += InitiatePlayerPoints;
        NetworkController.OnOtherClientDisconnected += RemovePlayer;
        NetworkController.OnDisconnected += DestroyOnDisconnect;
    }

    private void OnDestroy()
    {
        NetworkController.OnOtherClientConnected += InitiatePlayerPoints;
        NetworkController.OnOtherClientDisconnected += RemovePlayer;
        NetworkController.OnDisconnected -= DestroyOnDisconnect;
    }

    public void InitiatePlayerPoints(ulong playerId)
    {
        if(!_playerPoints.ContainsKey(playerId))
        {
            ScorePoints scorePoints = new ScorePoints(playerId);
            _playerPoints.Add(playerId, scorePoints);
        }
    }

    private void DestroyOnDisconnect(bool wasHost, bool connectionWasLost) => Destroy(gameObject);

    public void RemovePlayer(ulong playerId)
    {
        if(_playerPoints.ContainsKey(playerId))
        {
            _playerPoints.Remove(playerId);
        }
    }

    public void AddPointsToPlayer(ulong playerId, int points, List<DescriptivePoints> playerDescriptivePoints)
    {
        if(_playerPoints.TryGetValue(playerId, out ScorePoints scorePoints))
        {
            //scorePoints.TotalPoints += points;
            scorePoints.LastMatchPoints = points;
            //scorePoints.LastMatchPoints.AddRange(playerDescriptivePoints);
            scorePoints.LastMatchDescriptivePoints = playerDescriptivePoints;

            _playerPoints.Remove(playerId);
            _playerPoints.Add(playerId, scorePoints);
        }
    }

    public void AddLostCounterToPlayer(ulong playerId)
    {
        if(_playerPoints.TryGetValue(playerId, out ScorePoints scorePoints))
        {
            scorePoints.LostCounter += 1;
            _playerPoints.Remove(playerId);
            _playerPoints.Add(playerId, scorePoints);
        }
    }

    public void ResetLostCounterToPlayer(ulong playerId)
    {
        if(_playerPoints.TryGetValue(playerId, out ScorePoints scorePoints))
        {
            scorePoints.LostCounter = 0;
            _playerPoints.Remove(playerId);
            _playerPoints.Add(playerId, scorePoints);
        }
    }

    public bool IsGameChangingAvailable(ulong playerId)
    {
        if(_playerPoints[playerId].LostCounter >= RoundsLoosing)
        {
            return true;
        }

        return false;
    }

    public SerializedScorePointList GetSerializedScore()
    {
        SerializedScorePointList serializedScorePointList = new SerializedScorePointList(_playerPoints.Values.ToArray());
        return serializedScorePointList;
    }


    public void EndMatch()
    {
        _scoreAuditor.Audit();
        //AdicionaParaTeste();
    }

    public bool VerifyWinner()
    {
        foreach(ScorePoints scorePoints in _playerPoints.Values)
        {
            if(scorePoints.TotalPoints >= PointsToWin)
            {
                return true;
            }
        }

        return false;
    }

    public ulong GetWinner()
    {
        ScorePoints winnerScorePoints = new ScorePoints(0);

        foreach(ScorePoints scorePoints in _playerPoints.Values)
        {
            if(scorePoints.TotalPoints > PointsToWin)
            {
                if(scorePoints.TotalPoints == winnerScorePoints.TotalPoints)
                {
                    winnerScorePoints = Tiebreaker(scorePoints, winnerScorePoints);
                }

                if(scorePoints.TotalPoints > winnerScorePoints.TotalPoints)
                {
                    winnerScorePoints = scorePoints;
                }
            }
        }
        return winnerScorePoints.PlayerId;
    }

    public ScorePoints Tiebreaker(ScorePoints player1Score, ScorePoints player2Score)
    {
        ulong playerId1 = player1Score.PlayerId;
        ulong playerId2 = player2Score.PlayerId;

        int firstWins_Player1 = CountNumberOfScore(playerId1, FIRST_TO_CHECK_OUT_INDEX);
        int firstWins_Player2 = CountNumberOfScore(playerId2, FIRST_TO_CHECK_OUT_INDEX);

        if(firstWins_Player1 > firstWins_Player2)
        {
            return player1Score;
        }

        if(firstWins_Player1 < firstWins_Player2)
        {
            return player2Score;
        }

        int checksOut_Player1 = CountNumberOfScore(playerId1, CHECK_OUT_INDEX);
        int checksOut_Player2 = CountNumberOfScore(playerId2, CHECK_OUT_INDEX);

        if(checksOut_Player1 > checksOut_Player2)
        {
            return player1Score;
        }

        if(checksOut_Player1 < checksOut_Player2)
        {
            return player2Score;
        }


        int enemyHits_Player1 = CountNumberOfScore(playerId1, ENEMY_HIT_INDEX);
        int enemyHits_Player2 = CountNumberOfScore(playerId2, ENEMY_HIT_INDEX);

        if(enemyHits_Player1 > enemyHits_Player2)
        {
            return player1Score;
        }

        if(enemyHits_Player1 < enemyHits_Player2)
        {
            return player2Score;
        }

        return player1Score;

    }

    public int CountNumberOfScore(ulong playerId, int scoreTypeId)
    {
        int result = 0;
        foreach(DescriptivePoints descriptivePoint in _playerPoints[playerId].PlayerPoints)
        {
            if(descriptivePoint.ScoreTypeId == scoreTypeId)
            {
                result += 1;
            }
        }

        return result;
    }

    public void ClearLastMatchPoints()
    {
        foreach(ulong playerId in _playerPoints.Keys)
        {
            _playerPoints[playerId].ClearLastMatchPoints();
        }
    }

    public void AddToMainList(ulong playerId, DescriptivePoints descriptivePoints)
    {
        _playerPoints[playerId].PlayerPoints.Add(descriptivePoints);
    }

    public void AddToTotalPoints()
    {
        List<ulong> playerIdList = _playerPoints.Keys.ToList();
        foreach (ulong playerId in playerIdList)
        {
            ScorePoints scorePoints = _playerPoints[playerId];
            scorePoints.TotalPoints += scorePoints.LastMatchPoints;
            scorePoints.LastMatchPoints = 0;
            _playerPoints.Remove(playerId);
            _playerPoints.Add(playerId, scorePoints);
        }
    }


    // REMOVER DEPOIS ---------------------------------------------------------------------------------------------------------------------------

    
    private void AdicionaParaTeste()
    {
        List<DescriptivePoints> descriptivePointsList = new List<DescriptivePoints>();
        DescriptivePoints descriptivePoints = new DescriptivePoints(0, 5);
        descriptivePointsList.Add(descriptivePoints);

        AddPointsToPlayer(0, 5, descriptivePointsList);

    }
    // REMOVER DEPOIS ---------------------------------------------------------------------------------------------------------------------------


}