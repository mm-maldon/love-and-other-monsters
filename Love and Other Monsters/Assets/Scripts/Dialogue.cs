using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Ink.Runtime;

//Tutorial from here https://www.youtube.com/watch?v=8oTYabhj248&ab_channel=BMo
//https://www.youtube.com/watch?v=vY0Sk93YUhA&list=PL3viUl9h9k78KsDxXoAzgQ1yRjhm7p8kl&index=2
public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public TextAsset inkFile;
    
    private Story currentStory;
    private string currentLine;

    //choices
    public GameObject [] choices;

    private TextMeshProUGUI [] choiceText;
    
    //public string[] lines;
    public float textSpeed;
    //private int index;
    public bool dialoguePlaying;

    // Start is called before the first frame update
    void Start()
    {
        textComponent.text = string.Empty;

        //get choices if there are choices
        choiceText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach(GameObject choice in choices)
        {
            choiceText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }

        StartDialogue(inkFile);
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if(currentStory.currentChoices.Count == 0 && textComponent.text == currentLine)
            {
                continueStory();
            }

            else
            {
                StopAllCoroutines();
                textComponent.text = currentLine;
            }
        }
    }

    public void StartDialogue(TextAsset inkJSON)
    {
        dialoguePlaying = true;
        currentStory = new Story(inkJSON.text);
        //StartCoroutine(TypeLine());
        continueStory();
        

        //StartCoroutine(TypeLine());
    }

    void continueStory(){
        if(currentStory.canContinue){
            textComponent.text = "";
            currentLine = currentStory.Continue();
            StartCoroutine(TypeLine());
            DisplayChoices();
        }else{
            endDialogue();
        }
    }

    void endDialogue(){
        dialoguePlaying = false;
        gameObject.SetActive(false);
        textComponent.text = "";
    }

    //Types each character out one by one
    IEnumerator TypeLine()
    {
        foreach(char c in currentLine.ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        if(currentChoices.Count > choices.Length){
            Debug.Log("Too many choices in ink story");
        }

        int index = 0;
        foreach(Choice choice in currentChoices){
            choices[index].gameObject.SetActive(true);
            choiceText[index].text = choice.text;
            index++;
        }

        for(int i = index; i < choices.Length; i++){
            choices[i].SetActive(false);
        }

    }

    public void MakeChoice(int choiceIndex){ 
        currentStory.ChooseChoiceIndex(choiceIndex);
        continueStory();
    }

    /*void NextLine()
    {
        if(index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }*/
}
