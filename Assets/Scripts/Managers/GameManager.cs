﻿using UnityEngine;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	[SerializeField]
	public LoggingManager loggingManager;

	[SerializeField]
	public Camera mainCam;

	[SerializeField]
	public ProfileManager profileManager;

    [SerializeField]
    public DataManager dataManager;

	[SerializeField]
	public Tutorial tutorial;

	public GameLevel activeLevel;
	public AssistanceManager activeLevelAssistance;

    private bool player;
    private bool gameA;
	private string gameType;
	private bool tutorialSeen = false;
    private float levelCompletionTime = -1;
	private float lastHitTime = -1;
	private bool sessionActive = true;
	private bool levelActive = false;
	private float pauseTime = 0;
	private bool intro = false;
	private int currentProgress = 0;

    public InputHandler input;

	// Counters for Statistics

	// Per-Level Counters 
	private int levelHitsTotal = 0;
    private int levelHitsLeft = 0;
    private int levelHitsRight = 0;

    private Vector2 lastHitPos;

	private int levelErrorsTotal = 0;

	private List<float> levelReactionTimesList = new List<float>();
	private List<float> levelReactionTimesLeftList = new List<float>();
	private List<float> levelReactionTimesRightList = new List<float>();
	private float levelReactionTime = 0.0f;
	private float levelReactionTimeLeft = 0.0f;
	private float levelReactionTimeRight = 0.0f;

	private float levelTimeStart = -1;		  // used for calculations (Time.time based)
	private float levelTimeEnd = -1;		  // used for calculations (Time.time based)
	private DateTime levelTimestampStart;     // used for logging (System.Datetime.now based)
	private DateTime levelTimestampEnd;       // used for logging (System.Datetime.now based)
    private DateTime sessionTimestampStart;   // used for logging (System.Datetime.now based)

	private float bestCompletionTime = -1.0f;
	private List<float> HitTimeLeft = new List<float>();
	private List<float> HitTimeRight = new List<float>();
	private int sessionHitsTotal = 0;
	private int sessionErrorTotal = 0;

	private int sessionLength;
	private float sessionTimeStart = -1;
	private float sessionTimeCurrent = -1; 		// current time formatted in seconds
	private float sessionTimeRemaining = -1;    // remaining time formatted in seconds
    private int difficultyLevel;

    // Canvas Stuff
    [SerializeField]
    private Canvas menuCanvas;
    [SerializeField]
    private Canvas setupCanvas;
    [SerializeField]
    private Canvas endLevelCanvas;
	[SerializeField]
	private Canvas endSessionCanvas;
	[SerializeField]
    private GameObject gameOverlayCanvas;
	[SerializeField]
	private GameObject gamePanel;
    [SerializeField]
    private CountAnimation endLevelTime;
    [SerializeField]
    private Text endLevelDuration;
	private string endLevelDurationTemplate;
	[SerializeField]
	private CountAnimation endLevelAmount;
	[SerializeField]
	private CountAnimation endLevelAverage;
	[SerializeField]
	private CountAnimation totalAmount;
	[SerializeField]
	private Text endSessionAmount;
	[SerializeField]
	private CountAnimation bestCompletionTimeText;
	[SerializeField]
	private CountAnimation reactionTimeRightText;
	[SerializeField]
	private CountAnimation reactionTimeLeftText;

	[SerializeField]
	public Text countDownText;
	public int countDown = 4;
	[SerializeField]
	public GameObject getReadyOverlay;


    // Since we're doing everything in one scene now, we're just adding this to figure out 
    // the state we're in. 
    public static string _CurrentScene = "";

    public LineDrawer LD;

    void Start()
    {
		GameLevel._DidInit = false;

		endLevelDurationTemplate = endLevelDuration.text;

        input = gameObject.AddComponent<InputHandler>();

        LoadPlayerPrefs();

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        lastHitPos = new Vector2(-1.0f, -1.0f);
        lastHitTime = -1.0f;
    }

    public void LoadPlayerPrefs()
    {
		string currentProfileID = profileManager.GetCurrentProfileID ();
		sessionLength = PlayerPrefs.GetInt("Settings:" + currentProfileID + ":Time", 2);
        difficultyLevel = PlayerPrefs.GetInt("Settings:" + currentProfileID + ":DifficultyLevel", 1);
		gameType = PlayerPrefs.GetString ("Settings:" + currentProfileID + ":GameType", "gameA");
		intro = PlayerPrefs.GetInt("Settings:" + currentProfileID + ":Intro", 1) == 1;
   
    }

    public void StartGame()
    {
		LoadPlayerPrefs ();
        gameOverlayCanvas.SetActive(true);

		if (intro && !tutorialSeen)
		{
			tutorial.Init();
			_CurrentScene = "Tutorial";
			tutorialSeen = true;
			string currentProfileID = profileManager.GetCurrentProfileID();

			int introVal = PlayerPrefs.GetInt("Settings:" + currentProfileID + ":Intro", -1);

			if (introVal == -1)
			{
				PlayerPrefs.SetInt("Settings:" + currentProfileID + ":Intro", tutorialSeen ? 0 : 1);
			}
		} else
		{
			gamePanel.SetActive(true);
			currentProgress += 1;

			if (!GameLevel._DidInit)
			{
				activeLevel.Init(this);
				_CurrentScene = "Level";
			}
            dataManager.NewSession();
			StartCoroutine(CountDownFirstLevel());

		}
		menuCanvas.gameObject.SetActive(false);
    }

	public void Update()
	{
		//Debug.DrawRay(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector3.forward * 10f, Color.red);
		if (_CurrentScene == "Level")
		{
			if (input.TouchDown)
			{
				LD.StartLine(input.TouchPos);
			}

			if (input.TouchActive)
			{
				//Debug.Log("touchInput: [" + mainCam.WorldToViewportPoint(input.TouchPos) + "]");
				HitType hitType = activeLevel.AttemptHit(input.TouchPos);
				LD.DrawLine(input.TouchPos, hitType);

				if (hitType == HitType.TargetHit || hitType == HitType.TargetHitLevelComplete || hitType == HitType.WrongTargetHit)
				{
					float time = Time.time;

					if (Time.time - sessionTimeStart < sessionLength * 60 && sessionActive)
					{
                        // We don't measure reaction time for the first hit because it is affected
                        // by the animation time and is hardly comparable to other times.
                        float reactionTime = -1.0f;
                        if (lastHitTime != -1.0f)
						{
                            reactionTime = Time.time - lastHitTime;
                            levelReactionTimesList.Add(reactionTime);

                            if (input.TouchPos.x > 0) {
                                HitTimeRight.Add(Time.time - lastHitTime);
                                levelReactionTimesRightList.Add(Time.time - lastHitTime);
                                levelHitsRight++;
                            } else {
                                HitTimeLeft.Add(Time.time - lastHitTime);
                                levelReactionTimesLeftList.Add(Time.time - lastHitTime);
                                levelHitsLeft++;
                            }

						}

                        // We don't measure distance of the first hit because
                        // there is no sensible start position we can measure from.
                        Vector2 hitPos = mainCam.WorldToViewportPoint(input.TouchPos);
                        float distance = -1.0f;

                        if (lastHitPos.x != -1.0f) {
                            distance = Vector2.Distance(lastHitPos, hitPos);
                        }

						levelHitsTotal += 1;

                        dataManager.AddHit(hitPos, reactionTime, distance, hitType);

						lastHitTime = Time.time;
                        lastHitPos = hitPos;
					}

					if (hitType == HitType.TargetHitLevelComplete)
					{
                        TheLevelEnded();
					}

				}

			} else if (input.TouchUp)
			{
				activeLevel.TempHit = null;
				LD.EndLine();
			}
		}

		if (Input.GetKeyDown(KeyCode.R))
		{
			GameObject.Find("GameLevel").GetComponent<GameLevel>().ReloadLevel();
		}
	}

    private void TheLevelEnded()
    {
        dataManager.printResults();
        levelActive = false;
        _CurrentScene = "LevelComplete";
		levelTimeEnd = Time.time;
		levelTimestampEnd = System.DateTime.Now;
		sessionTimeCurrent = (levelTimeEnd - sessionTimeStart);
		levelCompletionTime = levelTimeEnd - levelTimeStart;
		sessionTimeRemaining = sessionTimeRemaining - levelCompletionTime;
		sessionHitsTotal += levelHitsTotal;
		sessionErrorTotal -= levelErrorsTotal;
		levelReactionTime = Utils.GetMedian(levelReactionTimesList);
		levelReactionTimeLeft = Utils.GetMedian(levelReactionTimesLeftList);
		levelReactionTimeRight = Utils.GetMedian(levelReactionTimesRightList);
        bool assistanceWasActive = activeLevelAssistance.GetAssistanceWasActive();
        bool usedLineDrawing = LD.GetUsesLineDrawing();
        dataManager.AddLevelData(currentProgress, levelCompletionTime, sessionTimeCurrent, assistanceWasActive,
                                 levelTimestampStart, levelTimestampEnd, usedLineDrawing);
		loggingManager.WriteAggregateLog("Level " + currentProgress.ToString() + " Completed!");
		ShowTheEndLevelCanvas();
    }

	private IEnumerator CountDownFirstLevel()
	{
		getReadyOverlay.SetActive(true);
		while (countDown > 0)
		{
			countDownText.text = countDown.ToString();
			yield return new WaitForSeconds(1f);
			countDown--;
		}

		if (countDown < 1)
		{
			getReadyOverlay.SetActive(false);
			levelActive = true;
			levelTimestampStart = System.DateTime.Now;
            activeLevel.LoadNextLevel();
            sessionTimestampStart = System.DateTime.Now;
			sessionTimeStart = Time.time;
			sessionTimeRemaining = (sessionLength * 60);
			levelTimeStart = sessionTimeStart;
		}
	}

	public void ShowTheEndLevelCanvas()
    {
        Image bgPanel = endLevelCanvas.GetComponentInChildren<Image>();
        Color col = bgPanel.color;

        endLevelCanvas.gameObject.SetActive(true);
		gameOverlayCanvas.SetActive (false);
		if (Time.time - sessionTimeStart > sessionLength*60 && sessionActive) {
            dataManager.AddSessionData(gameType, difficultyLevel, sessionLength, sessionTimestampStart, intro);
            endSessionCanvas.gameObject.SetActive(true);
			loggingManager.UploadLog();
			sessionActive = false;
		}
		UpdateEndScreenClock ();
		TimerPause ();
        SetEndScreenValues();
    }

	private void UpdateEndScreenClock()
	{
		var timeSpan = TimeSpan.FromSeconds(Time.time - sessionTimeStart);
		string formattedTime = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
		endLevelDuration.text = string.Format(endLevelDurationTemplate, timeSpan.Minutes.ToString(), sessionLength.ToString());
		               
		endSessionAmount.text = formattedTime;
	}


    private void SetEndScreenValues()
    {

        if (sessionActive) {
			endLevelTime.SetTargetDecimalNumber(levelCompletionTime);
            if (bestCompletionTime < 0 || bestCompletionTime > levelCompletionTime) {
                bestCompletionTime = levelCompletionTime;
            }
			endLevelAmount.SetTargetWholeNumber(sessionHitsTotal);
			endLevelAverage.SetTargetDecimalNumber(levelReactionTime);

		} else {

			float hitTimeRightAverage;
			float hitTimeLeftAverage;

			if (currentProgress > 1) {
				hitTimeLeftAverage = HitTimeLeft.Average(item => (float)item);
				hitTimeRightAverage = HitTimeRight.Average(item => (float)item);
			} else {
				// If we only finish one level during the training time
				// we need to take a couple of dedicated measures.
				bestCompletionTime = levelCompletionTime;
				hitTimeLeftAverage = 0.00f;
				hitTimeRightAverage = 0.00f;
			}
            bestCompletionTimeText.SetTargetDecimalNumber(bestCompletionTime);

            totalAmount.SetTargetWholeNumber(sessionHitsTotal);
			reactionTimeLeftText.SetTargetDecimalNumber(hitTimeLeftAverage);
			reactionTimeRightText.SetTargetDecimalNumber(hitTimeRightAverage);
		}
    }

	private void resetLevelCounters()
	{
		levelHitsTotal = 0;
		levelHitsLeft = 0;
		levelHitsRight = 0;
		levelErrorsTotal = 0;
		levelReactionTime = 0.0f;
		levelReactionTimeLeft = 0.0f;
		levelReactionTimeRight = 0.0f;
		levelReactionTimesList.Clear();
		levelReactionTimesLeftList.Clear();
		levelReactionTimesRightList.Clear();
	}

	public void TimerPause()
	{
		pauseTime = Time.time;
		levelActive = false;
	}

	public void TimerResume()
	{
		sessionTimeStart += (Time.time - pauseTime);
		levelActive = true;
		activeLevelAssistance.LoadPlayerPrefs();
		LoadPlayerPrefs();
		activeLevel.ReloadLevel();
		//loggingManager.WriteLog ("Game Resumed");
	}

	public void ResetGame()
	{
        //loggingManager.WriteLog ("Game Reset!");
        bool shouldUpload = profileManager.GetUploadPolicy();
        bool sessionFinished = (Time.time - sessionTimeStart > sessionLength * 60);
        if (shouldUpload && loggingManager.hasLogs() && sessionFinished) {
            loggingManager.DumpCurrentLog();
            loggingManager.ClearLogEntries();
        }
        dataManager.SaveData();
		SceneManager.LoadSceneAsync("TMT_P10");
	}

	public void NextLevelButton()
	{
		resetLevelCounters();
		TimerResume();
		levelActive = true;
		gameOverlayCanvas.SetActive(true);
		_CurrentScene = "Level";
		activeLevelAssistance.resetAssistanceWasActive();
		currentProgress += 1;
		activeLevel.LoadNextLevel();
        dataManager.NewLevel();
		levelTimestampStart = System.DateTime.Now;
		levelTimeStart = Time.time;
        lastHitTime = -1.0f;
        lastHitPos = new Vector2(-1.0f, -1.0f);
	}

    void OnApplicationQuit()
    {
        dataManager.SaveData();
        // if we fail to upload before user exit, we dump the logs disk.
        bool shouldUpload = profileManager.GetUploadPolicy();
        bool sessionFinished = (Time.time - sessionTimeStart > sessionLength * 60);
        if (shouldUpload && loggingManager.hasLogs() && sessionFinished) {
            loggingManager.DumpCurrentLog();
            loggingManager.ClearLogEntries();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public string GetGameType()
    {
        return gameType;
    }
        

	public int GetLevelHitsRight()
	{
		return levelHitsLeft;
	}

	public int GetLevelHitsLeft()
	{
		return levelHitsRight;
	}

	public float GetLevelReactionTimeRight()
	{
		return levelReactionTimeLeft;
	}

	public float GetLevelReactionTimeLeft()
	{
		return levelReactionTimeRight;
	}

	public bool GetLevelActive() {
		return levelActive;
	}

    public int GetAmountOfCircles()
    {
        int targetAmount = Utils.TargetAmountFromDifficulty(difficultyLevel);
        return targetAmount;
    }
}
