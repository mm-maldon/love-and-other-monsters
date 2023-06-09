using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Ink.Runtime;
using UnityEngine.SceneManagement;

//Tutorial from here https://www.youtube.com/watch?v=8oTYabhj248&ab_channel=BMo
//https://www.youtube.com/watch?v=vY0Sk93YUhA&list=PL3viUl9h9k78KsDxXoAzgQ1yRjhm7p8kl&index=2
public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public TextAsset inkFile;
    
    private Story currentStory;
    private string currentLine;
    private DialogueVariables dialogueVariables;
    public bool endScene;
    public int sceneIndex;
    int canAdvance = 1;
    public Map map;
    int bgmTrack;

    //choices
    public GameObject [] choices;

    private TextMeshProUGUI [] choiceText;
    
    //public string[] lines;
    public float textSpeed;
    //private int index;
    public bool dialoguePlaying;

    //Speaker constants

    private const string SPEAKER_TAG = "speaker";
    private const string SPEAKER_LEFT_TAG = "speaker-l";
    private const string SPEAKER_C_LEFT_TAG = "speaker-cl";
    private const string SPEAKER_CENTER_TAG = "speaker-c";
    private const string SPEAKER_C_RIGHT_TAG = "speaker-cr";
    private const string SPEAKER_RIGHT_TAG = "speaker-r";
    private const string SPEAKER_TRANSITION_TAG = "speaker-transition";
    private const string SPEAKER_LEFT_TRANSITION_TAG = "speaker-l-transition";
    private const string SPEAKER_C_LEFT_TRANSITION_TAG = "speaker-cl-transition";
    private const string SPEAKER_CENTER_TRANSITION_TAG = "speaker-c-transition";
    private const string SPEAKER_C_RIGHT_TRANSITION_TAG = "speaker-cr-transition";
    private const string SPEAKER_RIGHT_TRANSITION_TAG = "speaker-r-transition";
    private const string PORTRAIT_TAG = "portrait";
    private const string FORMAT_TAG = "format";
    private const string BACKGROUND_TAG = "bg";
    private const string OPEN_MAP_TAG = "map";
    private const string MUSIC_TAG = "music";
    private const string FADE_MUSIC_TAG = "fade-music";

    public Animator speakerAnimator;
    public Animator speakerLAnimator;
    public Animator speakerCLAnimator;
    public Animator speakerCRAnimator;
    public Animator speakerRAnimator;
    
    public SpriteTransition speakerCTransition;
    public SpriteTransition speakerLTransition;
    public SpriteTransition speakerCLTransition;
    public SpriteTransition speakerCRTransition;
    public SpriteTransition speakerRTransition;

    public SpriteRenderer [] renderers;

    public Animator portraitAnimator;
    public Animator bgAnimator;

    public AudioClip[] audioSources;
    private AudioSource pageFlipSFX;
    public TownMusic townMusic;
    public AudioSource bgm;
    public AudioClip[] bgmSources;

    public AudioSource whoosh;
    public AudioSource violins;

    bool whooshHasPlayed = false;
    bool violinsHavePlayed = false;

    // Start is called before the first frame update
    void Start()
    {
        map = GameObject.Find("Map of Castelonia").GetComponent<Map>();
        map.setInvisible();
        pageFlipSFX = GetComponent<AudioSource>();
        textComponent.text = string.Empty;
        //get choices if there are choices
        choiceText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach(GameObject choice in choices)
        {
            choiceText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }
        dialogueVariables = new DialogueVariables();
        StartDialogue(inkFile);
        
    }

    // Update is called once per frame
    void Update()
    {

        if(!bgm.isPlaying && (bool)currentStory.variablesState["bgm_bool"] == false){
            Debug.Log("playing new track");
            bgm.volume = 0.5f;
            currentStory.variablesState["bgm_bool"] = true;
            bgmTrack = (int)currentStory.variablesState["bgm"];
            AudioClip a = bgmSources[bgmTrack];
            bgm.clip = a;
            bgm.Play();
        }
        if(bgm.isPlaying && (bool)currentStory.variablesState["fade_bool"] == true){
            StartCoroutine(FadeAudioSource.StartFade(bgm, 1.3f, 0));
            
        }

        if(!whoosh.isPlaying && ((bool)currentStory.variablesState["whoosh"] == true) && !whooshHasPlayed){
            Debug.Log("playing whoosh");
            whooshHasPlayed = true;
            currentStory.variablesState["whoosh"] = false;
            whoosh.Play();
        }

        if(!violins.isPlaying && ((bool)currentStory.variablesState["violins"] == true) && !violinsHavePlayed){
            Debug.Log("playing violins");
            currentStory.variablesState["violins"] = false;
            violins.Play();
        }

        if(violins.isPlaying && ((bool)currentStory.variablesState["violins_done"] == true) && violinsHavePlayed){
            StartCoroutine(FadeAudioSource.StartFade(violins, 1.3f, 0));
        }

        if(Input.GetMouseButtonDown(0) && canAdvance == 1 && Time.timeScale != 0.0)
        {
            if(currentStory.currentChoices.Count == 0 && textComponent.text == currentLine)
            {
                continueStory();
                if(gameObject.activeSelf)
                {
                    pageFlipSFX.clip = audioSources[Random.Range(0, audioSources.Length)];
                    //pageFlipSFX.Play();
                    
                }
            }

            else
            {
                if((bool)currentStory.variablesState["fade_bool"] == true){
                    bgm.Stop();
                }

                if(violins.isPlaying && (bool)currentStory.variablesState["violins"] == false){
                    violins.Stop();
                }

                foreach(SpriteRenderer r in renderers){
                    if(r.sprite.name != "none"){
                        Color tmp = r.color;
                        tmp.a = 1;
                        r.color = tmp;
                    }
                }

                StopAllCoroutines();
                textComponent.text = currentLine;
            }
        }
    }

    public void StartDialogue(TextAsset inkJSON)
    {
        dialoguePlaying = true;
        currentStory = new Story(inkJSON.text);
        dialogueVariables.StartListening(currentStory);
        continueStory();
    }

    void continueStory(){
        if(currentStory.canContinue){
            textComponent.text = "";
            currentLine = currentStory.Continue();
            StartCoroutine(TypeLine());
            DisplayChoices();
            HandleTags(currentStory.currentTags);
            HandleTags(currentStory.currentTags);
        }
        else{
            endDialogue();
        }
    }

    private void HandleTags(List<string> tags)
    {
        foreach(string tag in tags){
            string [] tagSplit = tag.Split(':');
            //Debug.Log(tagSplit.Length);

            string key = tagSplit[0].Trim();
            string val = tagSplit[1].Trim(); 

            switch(key){
                //Speakers
                case SPEAKER_TAG:
                    //Debug.Log(val);
                    speakerAnimator.Play(val);
                    break;
                case SPEAKER_CENTER_TAG:
                    //Debug.Log(val);
                    speakerAnimator.Play(val);
                    break;
                case SPEAKER_LEFT_TAG:
                    //Debug.Log(val);
                    speakerLAnimator.Play(val);
                    break;
                case SPEAKER_C_LEFT_TAG:
                    //Debug.Log(val);
                    speakerCLAnimator.Play(val);
                    break;
                case SPEAKER_C_RIGHT_TAG:
                    //Debug.Log(val);
                    speakerCRAnimator.Play(val);
                    break;
                case SPEAKER_RIGHT_TAG:
                    //Debug.Log(val);
                    speakerRAnimator.Play(val);
                    break;
                

                // Transitions
                case SPEAKER_TRANSITION_TAG:
                    //Debug.Log(val);
                    StartCoroutine(speakerCTransition.FadeIn());
                    break;
                case SPEAKER_CENTER_TRANSITION_TAG:
                    //Debug.Log(val);
                    StartCoroutine(speakerCTransition.FadeIn());
                    break;
                case SPEAKER_LEFT_TRANSITION_TAG:
                    //Debug.Log(val);
                    StartCoroutine(speakerLTransition.FadeIn());
                    break;
                case SPEAKER_C_LEFT_TRANSITION_TAG:
                    //Debug.Log(val);
                    StartCoroutine(speakerCLTransition.FadeIn());
                    break;
                case SPEAKER_C_RIGHT_TRANSITION_TAG:
                    //Debug.Log(val);
                    StartCoroutine(speakerCRTransition.FadeIn());
                    break;
                case SPEAKER_RIGHT_TRANSITION_TAG:
                    //Debug.Log(val);
                    StartCoroutine(speakerRTransition.FadeIn());
                    break;

                //Portrait
                case PORTRAIT_TAG:
                    //Debug.Log(val);
                    portraitAnimator.Play(val);
                    break;

                //Format
                case FORMAT_TAG:
                    //Debug.Log(val);
                    if(val == "italic"){
                        textComponent.fontStyle = FontStyles.Italic;
                    }else if(val == "bold"){
                        textComponent.fontStyle = FontStyles.Bold;
                    }else if(val == "none"){
                        textComponent.fontStyle &= ~FontStyles.Bold;
                        textComponent.fontStyle &= ~FontStyles.Italic;
                    }
                    break;
                
                //Background and Map
                case BACKGROUND_TAG:
                    //Debug.Log(val);
                    bgAnimator.Play(val);
                    break;
                case OPEN_MAP_TAG:
                    //Debug.Log("map should open");
                    map.setVisible();
                    gameObject.SetActive(false);
                    break;
                default:
                    Debug.Log("Tag not handled");
                    break;
            }
        }
    }

    void endDialogue(){
        dialogueVariables.StopListening(currentStory);
        dialoguePlaying = false;
        gameObject.SetActive(false);
        textComponent.text = "";
        if(endScene){
            SceneManager.LoadScene(sceneIndex);
        }
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
        //currentStory.Continue();
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

    public void setCanAdvance(){
        canAdvance *= -1;
    }

    public void returnFromMap(){
        continueStory();
    }

    void playBGM(AudioClip a){
        bgm.clip = a;
        bgm.Play();
    }
}
