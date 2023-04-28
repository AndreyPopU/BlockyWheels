using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Checkpoint : MonoBehaviour
{
    public int index;
    public bool inRange;
    public bool canLoad;
    public bool locked = true;
    public PlayerControls controls;
    public Material red;
    public Canvas statsCanvas;
    public bool coroutineRunning;
    public Text levelText;
    public Text bestTimeText;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Gameplay.Interact.performed += ctx => LoadLevel();
    }

    private void Start()
    {
        // Canvas setup
        levelText = statsCanvas.GetComponentsInChildren<Text>()[0];
        bestTimeText = statsCanvas.GetComponentsInChildren<Text>()[1];
        levelText.text = "Level " + index.ToString();
        statsCanvas.gameObject.SetActive(false);

        if (PlayerPrefs.HasKey(SaveLoadManager.bestTimeStrings[index]))
        {
            if (PlayerPrefs.GetFloat(SaveLoadManager.bestTimeStrings[index]) > .01f)
            bestTimeText.text = "Best time: " + PlayerPrefs.GetFloat(SaveLoadManager.bestTimeStrings[index]).ToString();
            else bestTimeText.text = "Best time: DNF";
        }
        else bestTimeText.text = "Best time: DNF";

        inRange = false;
        canLoad = false;

        int highestLevel = 0;

        if (PlayerPrefs.HasKey(SaveLoadManager.campaignLevelString)) highestLevel = PlayerPrefs.GetInt(SaveLoadManager.campaignLevelString);
        else highestLevel = 1;

        if (index <= highestLevel) locked = false;
        else GetComponent<MeshRenderer>().material = red;

        //if (index == highestLevel) StartCoroutine(LoadSceneAsync());
        if (PlayerPrefs.HasKey(SaveLoadManager.lastLevelPlayedString) && index == PlayerPrefs.GetInt(SaveLoadManager.lastLevelPlayedString))
            FindObjectOfType<CampaignMovement>().transform.position = transform.position;
        else if (index == 1) FindObjectOfType<CampaignMovement>().transform.position = transform.position;
    }

    IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(index);

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f)
            {
                if (canLoad)
                {
                    //if (FadePanel.instance.panel.alpha < 1) FadePanel.instance.panel.alpha += 3 * Time.deltaTime;
                    //else { FadePanel.instance.fadeOut = true; operation.allowSceneActivation = true; }
                }
            }
            yield return null;
        }
    }

    public IEnumerator CanvasPop(bool active)
    {
        if (coroutineRunning) yield break;


        coroutineRunning = true;
        if (active)
        {
            statsCanvas.gameObject.SetActive(true);

            while (statsCanvas.transform.localScale.y < 1)
            {
                statsCanvas.transform.localScale += Vector3.up * .1f;
                yield return null;
            }

            statsCanvas.transform.localScale = Vector3.one;
        }
        else
        {
            while (statsCanvas.transform.localScale.y > 0)
            {
                statsCanvas.transform.localScale -= Vector3.up * .1f;
                yield return null;
            }

            statsCanvas.transform.localScale = new Vector3(1,0,1);
            statsCanvas.gameObject.SetActive(false);
        }
        coroutineRunning = false;
    }

    private void OnEnable() { controls.Gameplay.Enable(); }

    private void OnDisable() { controls.Gameplay.Disable(); }

    public void LoadLevel() 
    {
        if (inRange && !locked)
        {
            CampaignMovement car = FindObjectOfType<CampaignMovement>();
            car.canMove = false;
            if (car.lastInput == -1) car.currentWaypoint = car.waypoints.Length - car.currentWaypoint;

            PlayerPrefs.SetInt(SaveLoadManager.lastCheckpointString, car.currentWaypoint);
            PlayerPrefs.SetInt(SaveLoadManager.lastRotationString, (int)car.transform.rotation.eulerAngles.y);

            GameManager.instance.StartCoroutine(GameManager.instance.LoadLevel(index));
            PlayerPrefs.SetInt(SaveLoadManager.lastLevelPlayedString, index);
        }
    }

    private void OnTriggerEnter(Collider other) { if (other.gameObject.tag == "Player") { inRange = true; StartCoroutine(CanvasPop(inRange)); } }

    private void OnTriggerExit(Collider other) { if (other.gameObject.tag == "Player") { inRange = false; StartCoroutine(CanvasPop(inRange)); } }
}
