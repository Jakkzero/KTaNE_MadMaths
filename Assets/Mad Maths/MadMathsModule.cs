using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BombInfoExtensions;
using UnityEngine;
using UnityEngine.Networking;

public class MadMathsModule : MonoBehaviour
{

    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public KMSelectable Add1;
    public KMSelectable Add5;
    public KMSelectable Sub1;
    public KMSelectable Sub5;
    public KMSelectable ButtonA;
    public KMSelectable ButtonB;
    public KMSelectable Reset;
    public KMSelectable Submit;
    public TextMesh[] QuestionDisplay;
    public TextMesh SubmissionDisplay;
    public TextMesh ButtonALabel;
    public TextMesh ButtonBLabel;

    static int ModuleIdCounter = 1;
    int ModuleId;

    bool isActivated = false;
    bool isSolved = false;

    // Represents the number on display in the Submission Display.
    int currentSubmission = 0;

    // Represents the 'true form' of the numbers on the display. Negative numbers correspond to operators: -1 is +, -2 is -, -3 is ×.
    int[] stateNumbers = new int[5];
    /* Represents the rotation of the numbers on the display. Values within 0-4, with the rotation being 100x the number in degrees clockwise.
	   Operators (+,-,×) always have 0 rotation. */
    int[] stateRotation = new int[5];
    int[] resultNumbers = new int[5];
    int finalResult;
    int buttonA;
    int buttonB;

    void Awake ()
    {
        ModuleId = ModuleIdCounter++;
    }

    void SetSmallDigit(int index)
    {
        //List of numbers that will always result in a digit of 5 or less. Arrays are in the form {number,rotation}.
        int[,] smallNumbers = {
        {0,1},{0,4},{1,1},{1,2},{2,1},{4,1},{4,2},{4,4},{5,1},{5,2},{5,3},{5,4},{6,1},{6,2},{6,3},{7,1},{7,2},{8,1},{8,2},{9,2},{9,3}
        };
        int n = Random.Range(0, smallNumbers.GetLength(0));
        stateNumbers[index] = smallNumbers[n, 0];
        stateRotation[index] = smallNumbers[n, 1];
        return;
    }

    void SetReallySmallDigit(int index)
    {
        //List of numbers that will always result in a digit of 1 or less. Arrays are in the form {number,rotation}
        int[,] reallySmallNumbers = {
        {0,1},{1,1},{1,2},{6,2},{7,2},{8,1},{8,2}
        };
        int n = Random.Range(0, reallySmallNumbers.GetLength(0));
        stateNumbers[index] = reallySmallNumbers[n, 0];
        stateRotation[index] = reallySmallNumbers[n, 1];
        return;
    }

    bool CheckSerialTargetLetters()
    {
        string[] targetLetters = { "X", "P", "L", "D", "E" };

        foreach (string x in targetLetters)
        {
            if (BombInfo.GetSerialNumber().Contains(x))
            {
                return true;
            }
        }
        return false;
    }

    int Compute(int n1, int n2, int sign)
    {
        switch (sign * -1)
        {
            case 1:
                return (n1 + n2);
            case 2:
                return (n1 - n2);
            case 3:
                return (n1 * n2);
        }
        return -999;
    }
    
    void ArrayToLog(string description, int[] array, bool outputDisplayedDigit = false)
    {
        string[] stringArray = new string[array.Length];
        string[] signs = { "+", "-", "×" };
        string[] displayDigits = { "Q", "1", "2", "5", "3", "4", "F", "7", "U", "9" };

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] >= 0)
            {
                if (!outputDisplayedDigit)
                {
                    stringArray[i] = array[i].ToString();
                }
                else
                {
                    stringArray[i] = displayDigits[array[i]];
                }
            }
            else
            {
                stringArray[i] = signs[array[i] * -1 - 1];
            } 
        }

        string output = string.Join(" ", stringArray);
        
        Debug.LogFormat("[Mad Maths #{0}] {1}: {2}", ModuleId, description, output);
    } 

    //Finds two integer buttons that will allow the answer to be reached in the number of presses or less.
    int[] CalculateButtons(int answer, int presses, int cycle)
    {        
        int answerQuotient = Mathf.Abs(answer) / (presses+1);
        int answerRemainder = Mathf.Abs(answer) % (presses+1);
        List<int> factors = new List<int>{1};

        for (int i = 2; i < presses; i++)
        {
            if ((answerQuotient + answerRemainder) % i == 0)
            {
                factors.Add(i);
            }
        }
        
        int aPresses = factors[Random.Range(0, factors.Count)];
        int bPresses = presses - aPresses;
        int aButton = 1;
        int bButton = -1;

        //Ensures buttons can't be +1 or -1
        while (Mathf.Abs(aButton) == 1 || Mathf.Abs(bButton) == 1) 
        {
            aButton = answerQuotient + (answerQuotient + answerRemainder) / aPresses + cycle * bPresses;
            bButton = answerQuotient - cycle * aPresses;
            cycle += 1;
        }

        if (answer < 0)
        {
            aButton = aButton * -1;
            bButton = bButton * -1;
        }
        
        //Changes a button if it's 0
        if (aButton == 0)
        {
            aPresses = 0;
            aButton = Random.Range(2,10);
            if (Random.Range(0,2) == 0)
            {
                aButton = aButton * -1;
            }
        }

        if (bButton == 0)
        {
            bPresses = 0;
            bButton = Random.Range(2, 10);
            if (Random.Range(0, 2) == 0)
            {
                bButton = bButton * -1;
            }
        }

        //Makes a button negative if both are the same
        if (aButton == bButton)
        {
            aButton = aButton * -1;
            bPresses = aPresses + bPresses;
            aPresses = 0;
        }

        Debug.LogFormat("[Mad Maths #{0}] First button adds {1}, second button adds {2}. Press the first button {3} times and the second button {4} times, then submit to solve.", ModuleId, aButton, bButton, aPresses, bPresses);

        int[] buttons = {aButton, bButton};
        return buttons;
    }

    // Initialisaton
    void Start()
    {

        // Generate the numbers to display
        for (var i = 0; i < 5; i++)
        {
            stateNumbers[i] = Random.Range(0, 10);
            stateRotation[i] = Random.Range(1, 5);
        }

        // Replace some number(s) with operation(s)
        if (Random.Range(0, 2) == 0)
        {
            stateNumbers[2] = Random.Range(-2, 0);
            stateRotation[2] = 0;

            if (stateNumbers[2] == -1)
            {
                //limits size of numbers if addition is selected
                if (Random.Range(0, 2) == 0)
                {
                    SetSmallDigit(0);
                    SetSmallDigit(3);
                }
                else
                {
                    int i = Random.Range(0, 2) * 3;
                    SetReallySmallDigit(i);
                }
            }
        }
        else
        {
            stateNumbers[1] = Random.Range(-3, 0);
            stateRotation[1] = 0;
            stateNumbers[3] = Random.Range(-3, 0);
            stateRotation[3] = 0;

            if (stateNumbers[1] == -3 && stateNumbers[3] == -3)
            {
                //limits size of numbers if multiplication is selected twice
                if (Random.Range(0, 2) == 0)
                {
                    SetSmallDigit(0);
                    SetSmallDigit(2);
                    SetSmallDigit(4);
                }
                else
                {
                    int i = Random.Range(0, 3) * 2;
                    SetReallySmallDigit(i);
                }
            }
        }

        ArrayToLog("Displayed digits", stateNumbers, true);
        ArrayToLog("Actual digits", stateNumbers);
        ArrayToLog("Rotations (in hundreds of degrees clockwise)", stateRotation);

        bool serialHasTargetLetters = CheckSerialTargetLetters();
        int numbersInSerial = BombInfo.GetSerialNumberNumbers().Count();
        int batteryCount = BombInfo.GetBatteryCount();
        int litCount = BombInfo.GetOnIndicators().Count();

        // Solve it for ourselves to find the result

        // First we process our numbers
        for (var i = 0; i < 5; i++)
        {
            if (stateNumbers[i] == -1)
            {
                resultNumbers[i] = -1;
                continue;
            }

            switch (stateRotation[i])
            {
                case 0:
                    resultNumbers[i] = stateNumbers[i];
                    break;

                case 1:
                    int x = stateNumbers[i];
                    for (var j = 0; j < 3; j++)
                    {
                        x = (x % 2 == 0) ? x / 2 : (x * 3) + 1;
                    }
                    resultNumbers[i] = x;
                    break;

                case 2:
                    if (stateNumbers[i] > 5)
                    {
                        resultNumbers[i] = (stateNumbers[i] + 3) * (stateNumbers[i] + 3);
                    }
                    else
                    {
                        resultNumbers[i] = stateNumbers[i] * stateNumbers[i] * stateNumbers[i];
                    }
                    break;

                case 3:
                    if (stateNumbers[i] == batteryCount || stateNumbers[i] == litCount || stateNumbers[i] == (batteryCount + litCount))
                    {
                        resultNumbers[i] = 9 - stateNumbers[i];
                    }
                    else if (BombInfo.GetSerialNumberNumbers().Contains(stateNumbers[i]))
                    {
                        resultNumbers[i] = stateNumbers[i] * 7;
                    }
                    // This has been changed to number of indicators because I cannae be assed
                    else
                    {
                        resultNumbers[i] = litCount + BombInfo.GetOffIndicators().Count();
                    }
                    break;

                case 4:
                    //Removed "and has 0 strikes" as you would sometimes need to change the buttons if the bomb gets a strike.
                    if (serialHasTargetLetters)
                    {
                        resultNumbers[i] = stateNumbers[i] * 11;
                    }
                    else
                    {
                        resultNumbers[i] = stateNumbers[i] * 2 + numbersInSerial;
                    }
                    break;
            }

        }

        ArrayToLog("Numbers after calculation", resultNumbers);

        //Makes all results single digits
        for (int i = 0; i < 5; i++)
        {
            if (resultNumbers[i] > 9)
            {
                resultNumbers[i] = resultNumbers[i] % 10;
            }
        }

        ArrayToLog("After becoming single digit", resultNumbers);

        /// Calculates the answer.
        if (resultNumbers[2] < 0)
        {
            int value1 = 10 * resultNumbers[0] + resultNumbers[1];
            int value2 = 10 * resultNumbers[3] + resultNumbers[4];
            finalResult = Compute(value1, value2, resultNumbers[2]);
        }
        else if (resultNumbers[3] < -2 && resultNumbers[1] < 0 && resultNumbers[1] >= -2)
        {
            int value2 = Compute(resultNumbers[2], resultNumbers[4], resultNumbers[3]);
            finalResult = Compute(resultNumbers[0], value2, resultNumbers[1]);
        }
        else if (resultNumbers[1] < 0 && resultNumbers[3] < 0)
        {
            int value1 = Compute(resultNumbers[0], resultNumbers[2], resultNumbers[1]);
            finalResult = Compute(value1, resultNumbers[4], resultNumbers[3]);
        }

        Debug.LogFormat("[Mad Maths #{0}] Answer: {1}", ModuleId, finalResult);

        //Find a set of buttons that will allow the answer to be entered

        int[] buttons = CalculateButtons(finalResult, Random.Range(3, 7), Random.Range(-3, 3));
        buttonA = buttons[0];
        buttonB = buttons[1];

        // We also have to add our hooks
        BombModule.OnActivate += ActivateModule;
        Reset.OnInteract += HandleReset;
        Submit.OnInteract += HandleSubmission;
        //Add1.OnInteract += HandleIncrement1;
        //Add5.OnInteract += HandleIncrement5;
        //Sub1.OnInteract += HandleDecrement1;
        //Sub5.OnInteract += HandleDecrement5;
        ButtonA.OnInteract += HandleIncrementA;
        ButtonB.OnInteract += HandleIncrementB;
    }

    //bool HandleIncrement1()
    //{
    //    if (!isActivated)
    //    {
    //        return false;
    //    }

    //    KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

    //    if (currentSubmission > 998)
    //    {
    //        currentSubmission = 999;
    //    }
    //    else
    //    {
    //        currentSubmission += 1;
    //    }

    //    SubmissionDisplay.text = currentSubmission.ToString();
    //    return false;
    //}

    //bool HandleIncrement5()
    //{
    //    if (!isActivated)
    //    {
    //        return false;
    //    }

    //    KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

    //    if (currentSubmission > 994)
    //    {
    //        currentSubmission = 999;
    //    }
    //    else
    //    {
    //        currentSubmission += 5;
    //    }

    //    SubmissionDisplay.text = currentSubmission.ToString();
    //    return false;
    //}

    bool HandleIncrementA()
    {
        if (!isActivated || isSolved)
        {
            return false;
        }

        KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

        if (Mathf.Abs(currentSubmission + buttonA) < 999)
        {
            currentSubmission += buttonA;
            Debug.LogFormat("[Mad Maths #{0}] Pressed first button (adds {1}), submission is now {2}.", ModuleId, buttonA, currentSubmission);
        }
        else
        {
            Debug.LogFormat("[Mad Maths #{0}] Pressed first button (adds {1}), current submission can not be incremented/decremented further as that would require 4 digits of display so it remains at {2}.", ModuleId, buttonA, currentSubmission);
        }

        SubmissionDisplay.text = currentSubmission.ToString();
        return false;
    }

    //bool HandleDecrement1()
    //{
    //    if (!isActivated)
    //    {
    //        return false;
    //    }

    //    KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

    //    if (currentSubmission < -998)
    //    {
    //        currentSubmission = -999;
    //    }
    //    else
    //    {
    //        currentSubmission -= 1;
    //    }

    //    SubmissionDisplay.text = currentSubmission.ToString();
    //    return false;
    //}

    //bool HandleDecrement5()
    //{
    //    if (!isActivated)
    //    {
    //        return false;
    //    }

    //    KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

    //    if (currentSubmission < -994)
    //    {
    //        currentSubmission = -999;
    //    }
    //    else
    //    {
    //        currentSubmission -= 5;
    //    }

    //    SubmissionDisplay.text = currentSubmission.ToString();
    //    return false;
    //}

    bool HandleIncrementB()
    {
        if (!isActivated || isSolved)
        {
            return false;
        }

        KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

        if (Mathf.Abs(currentSubmission + buttonB) < 999)
        {
            currentSubmission += buttonB;
            Debug.LogFormat("[Mad Maths #{0}] Pressed second button (adds {1}), submission is now {2}.", ModuleId, buttonB, currentSubmission);
        }
        else
        {
            Debug.LogFormat("[Mad Maths #{0}] Pressed first button (adds {1}), current submission can not be incremented/decremented further as that would require 4 digits of display so it remains at {2}.", ModuleId, buttonB, currentSubmission);
        }

        SubmissionDisplay.text = currentSubmission.ToString();
        return false;
    }

    protected bool HandleReset()
    {
        if (!isActivated || isSolved)
        {
            return false;
        }

        KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

        currentSubmission = 0;
        SubmissionDisplay.text = currentSubmission.ToString();

        Debug.LogFormat("[Mad Maths #{0}] Pressed (R)eset, submission is now {1}.", ModuleId, currentSubmission);
        return false;
    }

    protected bool HandleSubmission()
    {
        if (!isActivated || isSolved)
        {
            return false;
        }

        KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

        if (currentSubmission == finalResult)
        {
            BombModule.HandlePass();
            isSolved = true;
            Debug.LogFormat("[Mad Maths #{0}] Pressed (S)ubmit, submission is {1}, answer is {2}. Correct, module solved!", ModuleId, currentSubmission, finalResult);
        }
        else
        {
            BombModule.HandleStrike();
            Debug.LogFormat("[Mad Maths #{0}] Pressed (S)ubmit, submission is {1}, answer is {2}. Incorrect, strike given!", ModuleId, currentSubmission, finalResult);
        }
        return false;
    }

    void ActivateModule()
    {
        isActivated = true;
        SubmissionDisplay.text = "0";
        RenderDisplay();
        RenderButtons();
    }

    void RenderDisplay()
    {
        string[] displayMap = { "Q", "1", "2", "5", "3", "4", "F", "7", "U", "9", "+", "-", "×" };
        for (int i = 0; i < 5; i++)
        {
            if (stateNumbers[i] < 0)
            {
                QuestionDisplay[i].text = displayMap[stateNumbers[i]*-1 + 9];
                continue;
            }

            QuestionDisplay[i].text = displayMap[stateNumbers[i]];

            // Quarternions are strange...
            float rotationAmount = stateNumbers[i] != 8 ? stateRotation[i] * -100 : (stateRotation[i] * -100) + 180;
            QuestionDisplay[i].transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotationAmount);
        }
    }

    void RenderButtons()
    {
        if (buttonA > 0)
        {
            ButtonALabel.text = "+" + buttonA.ToString();
        }
        else
        {
            ButtonALabel.text = buttonA.ToString();
        }
        
        if (buttonB > 0)
        {
            ButtonBLabel.text = "+" + buttonB.ToString();
        }
        else
        {
            ButtonBLabel.text = buttonB.ToString();
        }
    }
}