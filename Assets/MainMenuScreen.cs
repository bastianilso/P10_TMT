﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using UnityEngine.UI;

public class MainMenuScreen : MonoBehaviour {

	[SerializeField]
	private ProfileManager profileManager;

	[SerializeField]
	private Text welcomeText;

	private string welcomeTextTemplate;

	private string currentProfileID;
	private string currentName;


	// Use this for initialization
	void Start () {
		welcomeTextTemplate = welcomeText.text;
		setTheWelcomeText ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void setTheWelcomeText() {
		currentName = profileManager.GetCurrentName();
		welcomeText.text = string.Format (welcomeTextTemplate, currentName);
	}
}