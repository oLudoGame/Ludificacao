using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Newtonsoft.Json;
using Photon.Pun;
using Photon.Realtime;
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
    public const byte QuestionAnsweredEventCode = 1;
    public const byte QuizSelectedEventCode = 2;

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
    }

    private void SendQuestionAnsweredEvent(object sender, AnswerData answerData)
    {
        ExtraData extraData = (ExtraData)answerData.extraData;
        var whoAnsweredIsThis = extraData.player.color == (GameColor)PhotonNetwork.LocalPlayer.CustomProperties["Color"];
        RaiseEventOptions options = new() { Receivers = ReceiverGroup.Others };
        if (whoAnsweredIsThis)
        {
            SimpleAnswerData simpleAnswerData = new()
            {
                elapsedTime = answerData.elapsedTime,
                question = answerData.question,
                selectedAnswer = answerData.selectedAnswer,
                diceValue = extraData.diceValue,
                playerName = extraData.player.name,
                selectedPieceName = extraData.selectedPiece.name,
            };
            var data = JsonConvert.SerializeObject(simpleAnswerData);
            PhotonNetwork.RaiseEvent(QuestionAnsweredEventCode, data, options, SendOptions.SendReliable);
        }
    }
    private void SendQuizSelectedEvent(object sender, Quiz quiz)
    {
        RaiseEventOptions options = new() { Receivers = ReceiverGroup.Others };
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.RaiseEvent(QuizSelectedEventCode, quiz.id, options, SendOptions.SendReliable);
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnNetworkEvent;
    }
    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnNetworkEvent;
    }

    private void OnNetworkEvent(EventData eventData)
    {
        byte eventCode = eventData.Code;
        if (eventCode == QuestionAnsweredEventCode)
        {
            Debug.Log("evento questão respondida recebido");
            SimpleAnswerData simpleAnswerData = JsonConvert.DeserializeObject<SimpleAnswerData>(eventData.CustomData.ToString());
            ExtraData extraData = new()
            {
                diceValue = simpleAnswerData.diceValue,
                player = Resources.FindObjectsOfTypeAll<Player>().Where((p) => p.name == simpleAnswerData.playerName).Single(),
                selectedPiece = Resources.FindObjectsOfTypeAll<Piece>().Where((p) => p.name == simpleAnswerData.selectedPieceName).Single(),
            };
            AnswerData answerData = new(simpleAnswerData.question, simpleAnswerData.selectedAnswer, simpleAnswerData.elapsedTime, extraData);
            var whoAnsweredIsNotThis = extraData.player.color != (GameColor)PhotonNetwork.LocalPlayer.CustomProperties["Color"];
            QuizManager.Instance.SendAnswerEvent(eventData.Sender, answerData);
        }
        if (eventCode == QuizSelectedEventCode)
        {
            Debug.Log("evento quiz selecionado recebido");
            QuizzesListUI.Instance.SetQuiz((string)eventData.CustomData);
        }
    }


    private void Start()
    {
        // ChangeState(GameState.SelectingPlayersQnt);
        ChangeState(GameState.SelectingQuiz);

        if (!PhotonNetwork.OfflineMode)
        {
            QuizManager.Instance.OnAnswered += SendQuestionAnsweredEvent;
            QuizManager.Instance.OnSelectedQuiz += SendQuizSelectedEvent;
        }
        QuizManager.Instance.OnAnswered += ChangeTurn;
        QuizManager.Instance.OnSelectedQuiz += (sender, quiz) => ChangeState(GameState.RollingDice);
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