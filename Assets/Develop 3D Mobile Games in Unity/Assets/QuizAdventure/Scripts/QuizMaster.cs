using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class QuizMaster : MonoBehaviour {
    
    PointOfInterest[] pOIs;        //an array of POIs  This is set in the Start method 
    PointOfInterest chosenPOI;     //a randomly chosen POI from the POIs array
    bool bPlayerFoundInfo = false; //has the player traveled to the chosen POI yet
    GameObject portal;             // a reference to the end level Portal object  This is set in the Start method

    [SerializeField]
    [Tooltip("Put the Text from the matching Panel here")]
    Text introTextBox, questionTextBox, responseTextBox;   //The text objects from the Panels in the Quizmaster Canvas

    [SerializeField]
    [Tooltip("How many seconds will we give the Player to read the Quizmasters text")]
    float timeToPause = 0.5f;   //How many seconds will we give the Player to read the Quizmasters text

    [SerializeField]
    [Tooltip("Put the Canvas that is on the Quizmaster here")]
    Canvas pOICanvas;  //The Canvas on the Quizmaster

    [SerializeField]
    [Tooltip("Put the Panel from the Canvas here")]
    GameObject introPanel, questionPanel, responsePanel;  //The Panel objects from the Quizmaster Canvas

    [SerializeField]
    [Tooltip("Put the 4 button texts from the Question Panel in Canvas here")]
    Text[] answerButtonTextFields;   //The buttons in the Question Panel in the Quizmaster Canvas

    [SerializeField]
    [Tooltip("What is the response the Quzmaster will give?")]
    string responseForCorrectAnswer, responseForWrongAnswer;    //The responses for correct and incorrect answers

    [Header("Mode Instruksi Langsung (Tanpa Kuis POI)")]
    [SerializeField]
    private bool directInstructionMode = true;

    [SerializeField]
    [TextArea(2, 4)]
    private string directInstructionText = "Halo! Aku Quizmaster Steve. Gerbang portal di trotoar sebelah kanan sudah kubuka, pergilah ke sana untuk menuju ke level berikutnya!";

    [SerializeField]
    private bool useOverlayDialogue = true;

    private bool bIsPortalOpen = false;  //Has the Player already won?

    // Use this for initialization
    void Start () {
        bIsPortalOpen = false;

        // Cari referensi ExitPortal terlebih dahulu (termasuk inactive)
        if (portal == null)
        {
            portal = GameObject.Find("ExitPortal");
            if (portal == null)
            {
                PortalExit pe = FindAnyObjectByType<PortalExit>(FindObjectsInactive.Include);
                if (pe != null) portal = pe.gameObject;
            }
        }
        if (portal != null)
        {
            portal.SetActive(false); // Sembunyikan portal di awal permainan
        }

        pOIs = FindObjectsByType<PointOfInterest>(FindObjectsInactive.Exclude);
        if (pOIs != null && pOIs.Length > 0 && !directInstructionMode)
        {
            chosenPOI = pOIs[UnityEngine.Random.Range(0, pOIs.Length)];
            if (chosenPOI != null)
            {
                chosenPOI.UITriggeredEvent += POITriggered;
                if (introTextBox != null)
                {
                    introTextBox.text = "Hi I'm Quizmaster Steve. \n Go to the " + chosenPOI.pOIName + " and learn about it.  Then come back and answer a question about the " + chosenPOI.pOIName + " correctly and I'll teleport you to a new location!";
                }
                SetupQuestionUI();
            }
        }
        else
        {
            directInstructionMode = true; // Fallback otomatis ke instruksi langsung jika scene tanpa POI
            if (pOICanvas != null)
            {
                pOICanvas.gameObject.SetActive(false); // Sembunyikan kanvas miring agar kota bersih
            }
        }
    }

    void SetupQuestionUI()  //we will cycle through the availible answers on the POI and add the answer text to a random button in this objects canvas  This will make each playthrough have a different order of answers so the Player cannot memorize the answer location
    {
        List<string> answerList = new List<string>();                        //Create a list for the possible answers from the chosenPOI and add each possible answer
        answerList.Add(chosenPOI.answerA);                                   
        answerList.Add(chosenPOI.answerB);
        answerList.Add(chosenPOI.answerC);
        answerList.Add(chosenPOI.correctAnswer);
        for (int i = 0; i < answerButtonTextFields.Length; i++)             //Cycle through the list
        {
            int randomNum = UnityEngine.Random.Range(0, answerList.Count);  //get a random number from 0 to the final availible answer in the list
            answerButtonTextFields[i].text = answerList[randomNum];         //add the text from the list at the random location to the next answerButtonTextField
            answerList.Remove(answerList[randomNum]);                       //remove the used answer from the list
        }
        questionTextBox.text = chosenPOI.quizQuestion;                      //assign the question from the POI to the questionText in the Quizmaster Canvas
        pOICanvas.enabled = false;                                          //disable the Canvas
    }

    public void CheckAnswer(int answer)
    {
        EnablePanels(false, false, true, true);                                     //Enable the response text panel
        if (answerButtonTextFields[answer].text == chosenPOI.correctAnswer) //check to see if the button clicked contains the correct answer
        {
            responseTextBox.text = responseForCorrectAnswer;                //display the Quizmasters congratulations text
            TurnOnPortal();                                                 //enable the Portal object
        }

        else                                                                //if the answer is wrong
        {
            responseTextBox.text = responseForWrongAnswer;                  //display the Quizmasters try again text
        }
       
    }

    public void EnablePanels(bool iPanel = false, bool qPanel = false, bool rPanel = false, bool canvasSwitch = false)
    {
        pOICanvas.enabled = canvasSwitch;                                            //enable the Quizmaster Canvas
        introPanel.SetActive(iPanel);                                        //Disable the intro text panel
        questionPanel.SetActive(qPanel);                                     //Disable the question text panel
        responsePanel.SetActive(rPanel);                                     //Enable the response text panel
        StartCoroutine(PauseForText(timeToPause));                                      //start a pause where the player cannot click to move so they have time to read the text
    }

    IEnumerator PauseForText(float pauseTime)
    {
        InputManager.bCanPlayerMove = false;                                //disable the players ability to move
        yield return new WaitForSeconds(pauseTime);                       //wait for the required seconds
        if(questionPanel.activeSelf == false)
        {
            InputManager.bCanPlayerMove = true;                                 //enable the players ability to move  
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer = other.CompareTag("Player") ||
                        (other.transform.root != null && other.transform.root.CompareTag("Player")) ||
                        other.GetComponentInParent<PlayerController>() != null ||
                        other.GetComponent<PlayerController>() != null ||
                        other.gameObject.name.Contains("Trucker") ||
                        other.gameObject.name.Contains("Player");

        if (isPlayer)
        {
            if (directInstructionMode)
            {
                TurnOnPortal();
                if (useOverlayDialogue)
                {
                    NPCDialogueOverlay.ShowDialogue(transform, "Quizmaster Steve", directInstructionText, true, 0.20f);
                }
                if (pOICanvas != null)
                {
                    pOICanvas.gameObject.SetActive(false);
                }
                return;
            }

            if (!bIsPortalOpen)
            {
                if (!bPlayerFoundInfo)
                {
                    EnablePanels(true, false, false, true);
                }
                else
                {
                    InputManager.bCanPlayerMove = false;
                    EnablePanels(false, true, false, true);
                }
            }
            else
            {
                EnablePanels(false, false, true, true);
                if (responseTextBox != null) responseTextBox.text = responseForCorrectAnswer;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        bool isPlayer = other.CompareTag("Player") ||
                        (other.transform.root != null && other.transform.root.CompareTag("Player")) ||
                        other.GetComponentInParent<PlayerController>() != null ||
                        other.GetComponent<PlayerController>() != null ||
                        other.gameObject.name.Contains("Trucker") ||
                        other.gameObject.name.Contains("Player");

        if (isPlayer)
        {
            if (useOverlayDialogue)
            {
                NPCDialogueOverlay.ShowDialogue(transform, "Quizmaster Steve", "", false);
            }
            EnablePanels();
        }
    }

    public void POITriggered(object source, EventArgs e)
    {
        bPlayerFoundInfo = true;
    }

    private void TurnOnPortal()
    {
        bIsPortalOpen = true;
        if (portal == null)
        {
            PortalExit pe = FindAnyObjectByType<PortalExit>(FindObjectsInactive.Include);
            if (pe != null) portal = pe.gameObject;
            if (portal == null) portal = GameObject.Find("ExitPortal");
        }
        if (portal != null)
        {
            portal.SetActive(true);
            Debug.Log("[QuizMaster] ExitPortal di trotoar sebelah kanan berhasil diaktifkan!");
        }
    }
}
