using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using static Piece;
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private GameObject playerPrefab;
    public GameState state;
    public event EventHandler<GameColor> OnTurnChanged;
    public event EventHandler<UiManager.EndGameArgs> OnGameEnded;
    public Dice dice;
    public Board board;
    public static GameManager Instance;
    private Player[] players;

    #region Tester Tools

    public bool alwaysAnswerRight = false;
    public bool directMove = false;

    #endregion

    public class ExtraData
    {
        public int diceValue;
        public Piece selectedPiece;
        public Player player;
    }

    [Serializable()]
    public class SimpleAnswerData
    {
        public Question question;
        public Answer selectedAnswer;
        public float elapsedTime;
        public int diceValue;
        public string selectedPieceName;
        public string playerName;
    }

    #region Unity Life Cycle

    private void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
    }

    private void Start()
    {
        // ChangeState(GameState.SelectingPlayersQnt);
        ChangeState(GameState.SelectingQuiz);
        QuizManager.Instance.OnAnswered += ChangeTurn;
        QuizManager.Instance.OnSelectedQuiz += (sender, quiz) => ChangeState(GameState.RollingDice);
        QuizManager.Instance.OnChoseQuestion += (_, _) => ChangeState(GameState.AnsweringQuestion);
        // SetMenuVisibility(true);
        SetMenuVisibility(false);
        if (!PhotonNetwork.OfflineMode) InitGame(PhotonNetwork.PlayerList.Length);
    }

    #endregion

    #region Game Mechanics

    public void CheckPlayerWin(object sender, PieceMovedArgs args)
    {
        if (args.currTile.isFinal && args.currTile.pieces.Count == 2)
        {
            OnGameEnded?.Invoke(this, new()
            {
                players = players,
                winner = args.piece.player,
            });
        }
    }

    private void ChangeTurn(object sender, AnswerData answerData)
    {
        if (dice.Value != 6 || !answerData.selectedAnswer.correct)
        {
            ColorsManager.I.UpdateColor();
        }
        ChangeState(GameState.RollingDice);
        OnTurnChanged?.Invoke(this, ColorsManager.I.currentColor);
    }

    #endregion

    #region Misc

    private void GeneratePlayers(GameColor[] colors)
    {
        List<Player> playersList = new();
        var playersGO = new GameObject("Players");
        int i = 0;
        foreach (GameColor color in colors)
        {
            var playerGO = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            var player = playerGO.GetComponent<Player>();
            if (!PhotonNetwork.OfflineMode)
            {
                var photonPlayer = PhotonNetwork.PlayerList[i];
                Hashtable photonPlayerCustomProps = new() {
                    {"Color", color}
                };
                photonPlayer.SetCustomProperties(photonPlayerCustomProps);
                player.photonPlayer = photonPlayer;
            }
            player.color = color;
            playerGO.name = color.ToString() + "Player";
            playerGO.transform.parent = playersGO.transform;
            playersList.Add(player);
            player.InstantiatePieces();
            i++;
        }
        players = playersList.ToArray();
    }

    public void InitGame(int playersQuantity)
    {
        ChangeState(GameState.SelectingQuiz);
        ColorsManager.I = new ColorsManager(playersQuantity);
        GeneratePlayers(ColorsManager.I.colors);
        Debug.Log(PhotonNetwork.IsMasterClient);
        if (PhotonNetwork.IsMasterClient)
        {
            QuizManager.Instance.SelectQuiz();
        }
        SetMenuVisibility(false);
        OnTurnChanged?.Invoke(this, ColorsManager.I.currentColor);
    }

    private void SetMenuVisibility(bool isInMenu)
    {
        UiManager.I.SetActiveMainMenu(isInMenu);
        dice.gameObject.SetActive(!isInMenu);
        board.gameObject.SetActive(!isInMenu);
    }

    public void ChangeState(GameState newState)
    {
        state = newState;
    }

    #endregion
}