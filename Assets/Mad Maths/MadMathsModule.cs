using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BombInfoExtensions;
using UnityEngine;

public class MadMathsModule : MonoBehaviour {

	public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public KMSelectable Add1;
	public KMSelectable Add5;
	public KMSelectable Add10;
	public KMSelectable Sub1;
	public KMSelectable Sub5;
	public KMSelectable Sub10;
	public KMSelectable Reset;
	public KMSelectable Submit;
    public TextMesh[] QuestionDisplay;
	public TextMesh SubmissionDisplay;

	bool isActivated = false;

	// Represents the number on display in the Submission Display.
	int currentSubmission = 0;

	// Represents the 'true form' of the numbers on the display. Operator is indicated by -1.
	int[] stateNumbers = new int[5];
	/* Represents the rotation of the numbers on the display. Values within 0-4, with the rotation being 100x the number in degrees.
	   Operator position is likely to be null. */
	int[] stateRotation = new int[5];
	// The offset into the stateNumbers array where the operator's position can be found.
	int operatorPosition;
	// TODO: Eventually implement mult or div, this should be changed.
	bool isAdd;

	int[] resultNumbers = new int[5];
	int finalResult;

	bool CheckSerialTargetLetters () {
		string[] targetLetters = {"X", "P", "L", "D", "E"};

		foreach (string x in targetLetters) {
			if (BombInfo.GetSerialNumber().Contains(x)) {
				return true;
			}
		}
		return false;
	}

	// Initialisaton
	void Start () {

		bool serialHasTargetLetters = CheckSerialTargetLetters();
		int numbersInSerial = BombInfo.GetSerialNumberNumbers().Count();
		int batteryCount = BombInfo.GetBatteryCount();
		int litCount = BombInfo.GetOnIndicators().Count();

		// Choose an operation
		isAdd = Random.Range(1, 3) == 1 ? true : false;
		// And place the operator (indicated by -1)
		operatorPosition = Random.Range(1, 3);

		// Generate the numbers to display
		for (var i = 0; i < 5; i++) {
			if (i == operatorPosition) {
				stateNumbers[i] = -1;
				continue;
			}

			stateNumbers[i] = Random.Range(0, 9);
			stateRotation[i] = Random.Range(0, 4);
		}

		Debug.Log("State Numbers: " + string.Join(" ", (from i in stateNumbers select i.ToString()).ToArray<string>()));
		Debug.Log("State Rotation: " + string.Join(" ", (from i in stateRotation select i.ToString()).ToArray<string>()));

		// Solve it for ourselves to find the result

		// First we process our numbers
		for (var i = 0; i < 5; i++) {
			if (stateNumbers[i] == -1) {
				resultNumbers[i] = -1;
				continue;
			}

			switch (stateRotation[i]) {
				case 0:
					resultNumbers[i] = stateNumbers[i];
					break;

				case 1:
					int x = stateNumbers[i];
					for (var j = 0; j < 3; j++) {
						x = (x % 2 == 0) ? x/2 : (x*3)+1;
					}
					resultNumbers[i] = x;
					break;
					
				case 2:
					if (stateNumbers[i] > 5) {
						resultNumbers[i] = (stateNumbers[i]+3)*(stateNumbers[i]+3);
					}
					else {
						resultNumbers[i] = stateNumbers[i] * stateNumbers[i] * stateNumbers[i];
					}
					break;

				case 3:
					if (stateNumbers[i] == batteryCount || stateNumbers[i] == litCount || stateNumbers[i] == (batteryCount+litCount)) {
						resultNumbers[i] = 9-stateNumbers[i];
					}
					else if (BombInfo.GetSerialNumberNumbers().Contains(stateNumbers[i])) {
						resultNumbers[i] = stateNumbers[i]*7;
					}
					// This has been changed to number of indicators because I cannae be assed
					else {
						resultNumbers[i] = litCount + BombInfo.GetOffIndicators().Count();
					}
					break;

				case 4:
					if (serialHasTargetLetters && BombInfo.GetStrikes() == 0) {
						resultNumbers[i] = stateNumbers[i]*11;
					}
					else {
						resultNumbers[i] = stateNumbers[i]*2 + numbersInSerial; 
					}
					break;
			}

		}

		Debug.Log("Result Numbers: " + string.Join(" ", (from i in resultNumbers select i.ToString()).ToArray<string>()));

		// Convert our number array into something we can evaluate
		int operand1 = 0;
		for (int i = 0; i < operatorPosition; i++) {
			operand1 += (resultNumbers[i] % 10) * (int)System.Math.Pow(10, (operatorPosition-i));
		}
		operand1 /= 10;

		int operand2 = 0;
		for (int i = operatorPosition+1; i < 5; i++) {
			operand2 += (resultNumbers[i] % 10) * (int)System.Math.Pow(10, (5-i));
		}
		operand2 /= 10;

		// And finally evaluate.
		finalResult = isAdd ? operand1 + operand2 : operand1 - operand2;

		Debug.Log("Operand 1: " + operand1.ToString());
		Debug.Log("Operand 2: " + operand2.ToString());
		Debug.Log("Final Result: " + finalResult.ToString());


		// We also have to add our hooks
		BombModule.OnActivate += ActivateModule;
		Reset.OnInteract += HandleReset;
		Submit.OnInteract += HandleSubmission;
		Add1.OnInteract += HandleIncrement1;
		Add5.OnInteract += HandleIncrement5;
		Add10.OnInteract += HandleIncrement10;
		Sub1.OnInteract += HandleDecrement1;
		Sub5.OnInteract += HandleDecrement5;
		Sub10.OnInteract += HandleDecrement10;
	}

	bool HandleIncrement1 () {
		if (!isActivated) {
			return false;
		}

		KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

		if (currentSubmission > 998) {
			currentSubmission = 999;
		}
		else {
			currentSubmission += 1;
		}

		SubmissionDisplay.text = currentSubmission.ToString();
		return false;
	}

	bool HandleIncrement5 () {
		if (!isActivated) {
			return false;
		}

		KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

		if (currentSubmission > 994) {
			currentSubmission = 999;
		}
		else {
			currentSubmission += 5;
		}

		SubmissionDisplay.text = currentSubmission.ToString();
		return false;
	}

	bool HandleIncrement10 () {
		if (!isActivated) {
			return false;
		}

		KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

		if (currentSubmission > 899) {
			currentSubmission = 999;
		}
		else {
			currentSubmission += 10;
		}

		SubmissionDisplay.text = currentSubmission.ToString();
		return false;
	}

	bool HandleDecrement1 () {
		if (!isActivated) {
			return false;
		}

		KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

		if (currentSubmission < -998) {
			currentSubmission = -999;
		}
		else {
			currentSubmission -= 1;
		}

		SubmissionDisplay.text = currentSubmission.ToString();
		return false;
	}

	bool HandleDecrement5 () {
		if (!isActivated) {
			return false;
		}

		KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

		if (currentSubmission < -994) {
			currentSubmission = -999;
		}
		else {
			currentSubmission -= 5;
		}

		SubmissionDisplay.text = currentSubmission.ToString();
		return false;
	}

	bool HandleDecrement10 () {
		if (!isActivated) {
			return false;
		}

		KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

		if (currentSubmission < -989) {
			currentSubmission = -999;
		}
		else {
			currentSubmission -= 10;
		}

		SubmissionDisplay.text = currentSubmission.ToString();
		return false;
	}

	protected bool HandleReset () {
		if (!isActivated) {
			return false;
		}

		KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

		currentSubmission = 0;
		SubmissionDisplay.text = currentSubmission.ToString();
		return false;
	}

	protected bool HandleSubmission () {
		if(!isActivated) {
			return false;
		}

		KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

		if (currentSubmission == finalResult) {
			BombModule.HandlePass();
		}
		else {
			BombModule.HandleStrike();
		}
		return false;
	}

	void ActivateModule () {
		isActivated = true;

		RenderDisplay();
	}

	void RenderDisplay () {
		string[] displayMap = {"Q", "1", "2", "5", "3", "4", "F", "7", "U", "9"};
		for (int i = 0; i < 5; i++) {
			if (i == operatorPosition) {
				QuestionDisplay[i].text = isAdd ? "+" : "-";
				continue;
			}

			QuestionDisplay[i].text = displayMap[stateNumbers[i]];

			// Quarternions are strange...
			float rotationAmount = stateNumbers[i] != 8 ? stateRotation[i]*-100 : (stateRotation[i]*-100)+180;
			QuestionDisplay[i].transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotationAmount);
		}
	}
}
