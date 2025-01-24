using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

class QuestionUI : MonoBehaviour {
	[SerializeField] private GameObject icons;
	private bool QuestionRuning = false;
	private float maxTime;
	private float elapsedTime;
	private QuestionData questionData;
	private readonly string[] difficultiesPtBr = { "Fácil", "Média", "Difícil" };
	private float[] timesForDifficulties;
	private readonly Color[] difficultiesBGColors = { Color.green, Color.yellow, Color.red };
	public static QuestionUI Instance;

	private void Awake() {
		Instance = this;
	}

	private void Start() {
		Hide();
		QuizManager.Instance.OnChoseQuestion += (sender, question) => Render(question);
		QuizManager.Instance.OnAnswered += (sender, question) => SetPlayerIconsVisibility((_) => false);
		SetPlayerIconsVisibility((_) => false);
	}

	private void Update() {
		if (QuestionRuning) UpdateTimer(Time.deltaTime);
	}

	private void Render(QuestionData questionData) {
		timesForDifficulties = new float[] {
			GameSettings.Instance.GetDifficultyTimer(0),
			GameSettings.Instance.GetDifficultyTimer(1),
			GameSettings.Instance.GetDifficultyTimer(2),
		};
		this.questionData = questionData;
		PrepareQuestion(this.questionData.question.question, this.questionData.questionIndex);
		PrepareAnswers(this.questionData.question.answers);
		PrepareDifficulty(this.questionData.question.difficulty);
		PrepareTimer(this.questionData.question.difficulty);
		SetPlayerIconsVisibility((color) => color == questionData.color);
		Show();
	}

	private void SetPlayerIconsVisibility(Func<GameColor?, bool> func) {
		var colors = Enum.GetValues(typeof(GameColor)).Cast<GameColor>().ToArray();
		foreach (var gColor in colors) {
			var icon = icons.transform.Find($"{gColor}Icon");
			if (icon != null) {
				icon.gameObject.SetActive(func(gColor));
			}
		}
	}

	private void PrepareQuestion(string text, int questionIndex) {
		var questionText = transform.Find("Header").Find("QuestionText").GetComponent<TextMeshProUGUI>();
		questionText.text = $"{questionIndex} - {text}";
	}

	private void PrepareAnswers(List<Answer> answers) {
		foreach ((string answerNumber, Answer answer) in answers.Select((answer, index) =>
			         ((index + 1).ToString(), answer))) {
			var answerElement = transform.Find("Answers").Find($"AnswerButton{answerNumber}");
			var buttonText = answerElement.GetComponentInChildren<TextMeshProUGUI>();
			buttonText.text = $"{answerNumber} - {answer.text}";
			answerElement.GetComponent<Button>().onClick.RemoveAllListeners();
			answerElement.GetComponent<Button>().onClick.AddListener(() => {
					Debug.Log($"Answer {buttonText.text}");
					QuizManager.Instance.SendAnswerEvent(this, new AnswerData(
						questionData.question,
						answer,
						elapsedTime,
						questionData.extraData
					));
					Hide();
				}
			);
		}
	}

	private void PrepareDifficulty(int difficulty) {
		var questionDifficulty = transform.Find("Header").Find("Difficulty");
		var difficultyText = questionDifficulty.GetComponentInChildren<TextMeshProUGUI>();
		difficultyText.text = difficultiesPtBr[difficulty];
		var difficultyImage = questionDifficulty.GetComponentInChildren<Image>();
		difficultyImage.color = difficultiesBGColors[difficulty];
	}

	private void PrepareTimer(int difficulty) {
		maxTime = timesForDifficulties[difficulty];
		elapsedTime = 0;
		var progress = transform.Find("Header").Find("Timer");
		var progressText = progress.GetComponent<TextMeshProUGUI>();
		string timeString = Math.Floor(maxTime).ToString();
		progressText.text = timeString;
		Slider timerBar = GetComponentInChildren<Slider>();
		timerBar.value = 1;
	}

	private void UpdateTimer(float timeElapsedInSecconds) {
		var progress = transform.Find("Header").Find("Timer");
		var progressText = progress.GetComponent<TextMeshProUGUI>();
		elapsedTime += timeElapsedInSecconds;
		string timeString = Math.Floor(maxTime - elapsedTime).ToString();
		progressText.text = timeString;
		Slider timerBar = GetComponentInChildren<Slider>();
		timerBar.value = (maxTime - elapsedTime) / maxTime;
		if (elapsedTime > maxTime) {
			Debug.Log("Tempo excedido");
			QuizManager.Instance.SendAnswerEvent(this, new AnswerData(
				questionData.question,
				null,
				maxTime,
				questionData.extraData));
			Hide();
		}
	}

	public void Show() {
		Debug.Log("show question");
		gameObject.SetActive(true);
		QuestionRuning = true;
	}

	public void Hide() {
		gameObject.SetActive(false);
		QuestionRuning = false;
	}
}