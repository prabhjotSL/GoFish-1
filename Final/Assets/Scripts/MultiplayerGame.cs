﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SWNetwork;

namespace GoFish
{
    public class MultiplayerGame : Game
    {
        NetCode netCode;

        protected new void Awake()
        {
            base.Awake();
            remotePlayer.IsAI = false;

            netCode = FindObjectOfType<NetCode>();

            NetworkClient.Lobby.GetPlayersInRoom((successful, reply, error) =>
            {
                if (successful)
                {
                    foreach(SWPlayer swPlayer in reply.players)
                    {
                        string playerName = swPlayer.GetCustomDataString();
                        string playerId = swPlayer.id;

                        if (playerId.Equals(NetworkClient.Instance.PlayerId))
                        {
                            localPlayer.PlayerId = playerId;
                            localPlayer.PlayerName = playerName;
                        }
                        else
                        {
                            remotePlayer.PlayerId = playerId;
                            remotePlayer.PlayerName = playerName;
                        }
                    }

                    gameDataManager = new GameDataManager(localPlayer, remotePlayer);
                    netCode.EnableRoomPropertyAgent();
                }
                else
                {
                    Debug.Log("Failed to get players in room.");
                }

            });
        }

        protected new void Start()
        {
            Debug.Log("Multiplayer Game Start");
        }

        //****************** Game Flow *********************//
        public override void GameFlow()
        {
            Debug.LogError("Should never be here");
        }

        protected override void OnGameStarted()
        {
            if (NetworkClient.Instance.IsHost)
            {
                gameDataManager.Shuffle();
                gameDataManager.DealCardValuesToPlayer(localPlayer, Constants.PLAYER_INITIAL_CARDS);
                gameDataManager.DealCardValuesToPlayer(remotePlayer, Constants.PLAYER_INITIAL_CARDS);

                gameState = GameState.TurnStarted;

                gameDataManager.SetGameState(gameState);
                netCode.ModifyGameData(gameDataManager.EncryptedData());
            }

            cardAnimator.DealDisplayingCards(localPlayer, Constants.PLAYER_INITIAL_CARDS);
            cardAnimator.DealDisplayingCards(remotePlayer, Constants.PLAYER_INITIAL_CARDS);
        }

        protected override void OnTurnStarted()
        {
            if (NetworkClient.Instance.IsHost)
            {
                SwitchTurn();
                gameState = GameState.TurnSelectingNumber;

                gameDataManager.SetCurrentTurnPlayer(currentTurnPlayer);
                gameDataManager.SetGameState(gameState);

                netCode.ModifyGameData(gameDataManager.EncryptedData());
                netCode.NotifyOtherPlayersGameStateChanged();
            }
        }

        protected override void OnTurnConfirmedSelectedNumber()
        {
            if (currentTurnPlayer == localPlayer)
            {
                SetMessage($"Asking {currentTurnTargetPlayer.PlayerName} for {selectedRank}s...");
            }
            else
            {
                SetMessage($"{currentTurnPlayer.PlayerName} is asking for {selectedRank}s...");
            }

            if (NetworkClient.Instance.IsHost)
            {
                gameState = GameState.TurnWaitingForOpponentConfirmation;
                gameDataManager.SetGameState(gameState);

                netCode.ModifyGameData(gameDataManager.EncryptedData());
                netCode.NotifyOtherPlayersGameStateChanged();
            }
        }

        public override void AllAnimationsFinished()
        {
            if (NetworkClient.Instance.IsHost)
            {
                netCode.NotifyOtherPlayersGameStateChanged();
            }
        }

        //****************** User Interaction *********************//
        public override void OnOkSelected()
        {
            if (gameState == GameState.TurnSelectingNumber && localPlayer == currentTurnPlayer)
            {
                if (selectedCard != null)
                {
                    netCode.NotifyHostPlayerRankSelected((int)selectedCard.Rank);
                }
            }
            else if (gameState == GameState.TurnWaitingForOpponentConfirmation && localPlayer == currentTurnTargetPlayer)
            {

            }
        }

        //****************** NetCode Events *********************//
        public void OnGameDataReady(EncryptedData encryptedData)
        {
            if (NetworkClient.Instance.IsHost)
            {
                gameState = GameState.GameStarted;
                gameDataManager.SetGameState(gameState);

                netCode.ModifyGameData(gameDataManager.EncryptedData());

                netCode.NotifyOtherPlayersGameStateChanged();
            }
        }

        public void OnGameDataChanged(EncryptedData encryptedData)
        {
            gameDataManager.ApplyEncrptedData(encryptedData);
            gameState = gameDataManager.GetGameState();
            currentTurnPlayer = gameDataManager.GetCurrentTurnPlayer();
            currentTurnTargetPlayer = gameDataManager.GetCurrentTurnTargetPlayer();
            selectedRank = gameDataManager.GetSelectedRank();
        }

        public void OnGameStateChanged()
        {
            base.GameFlow();
        }

        public void OnRankSelected(Ranks rank)
        {
            selectedRank = rank;
            gameState = GameState.TurnConfirmedSelectedNumber;

            gameDataManager.SetSelectedRank(selectedRank);
            gameDataManager.SetGameState(gameState);

            netCode.ModifyGameData(gameDataManager.EncryptedData());
            netCode.NotifyOtherPlayersGameStateChanged();
        }
    }
}
