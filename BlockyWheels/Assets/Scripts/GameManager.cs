using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public float bestTime;

    public float[] carLanes;
    public float laneSize;

    public CarMovement localPlayer;
    public Text eventText;
    public bool shouldStartLevel;

    [Header("Time")]
    public float countdown = 3f;
    public Text countdownText;
    public float time = 0f;
    public Text timeText;
    public Text remainingText;

    [Header("Player values reassign")]
    public ParticleSystem speedEffect;
    public Transform statsPanel;

    [Header("Level")]
    public GameObject[] roadSegments;
    public GameObject placeholderCamera;
    public Transform finishLine;
    public ParticleSystem fireworks;

    [Header("Level Completion")]
    public GameObject[] stars;
    public GameObject carUnlockedPanel;
    public int unlockCarIndex;

    [Header("Spectate Panel")]
    public GameObject spectatePanel;
    public Text spectateName;

    [Header("Other")]
    public bool multiplayer;
    public bool started;
    public bool pause;
    public GameObject smokePrefab;
    public GameObject pausePanel;
    public PlayerControls controls;
    
    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    void Awake()
    {
        localPlayer = CarMovement.instance;
        //multiplayer = MyNetworkManager.multiplayer;
        multiplayer = true;
        instance = this;
        controls = new PlayerControls();
        if (SceneManager.GetActiveScene().buildIndex >= 1 &&
            SceneManager.GetActiveScene().buildIndex <= 16) shouldStartLevel = true;

        if (multiplayer) controls.Gameplay.Pause.performed += ctx => Leave();
        else controls.Gameplay.Pause.performed += ctx => Pause();
    }

    private void Start()
    {
        if (GameObject.FindGameObjectWithTag("Finish"))
        {
            finishLine = GameObject.FindGameObjectWithTag("Finish").transform;
            fireworks = finishLine.GetComponentInChildren<ParticleSystem>();
        }

        if (multiplayer)
        {
            timeText.text = "1st"; // Color
            timeText.color = Color.yellow;
        }

        foreach (GameObject segment in roadSegments)
            segment.SetActive(true);
    }

    private void Update()
    {
        if (!shouldStartLevel) return;

        if (localPlayer != null && finishLine != null) remainingText.text = Mathf.Ceil(Mathf.Abs(finishLine.position.x - localPlayer.transform.position.x)).ToString() + "m";

        if (countdown > 0)
        {
            countdown -= Time.deltaTime;
            countdownText.text = (Mathf.Ceil(countdown)).ToString();
        }
        else
        {
            if (!started)
            {
                Destroy(placeholderCamera);
                localPlayer.finished = false;
                if (localPlayer.dead) localPlayer.Respawn();
                localPlayer.canTurn = true;
                localPlayer.canAccelerate = true;
                localPlayer.ActivateCamera(true);
                countdownText.gameObject.SetActive(false);
                started = true;
            }

            if (localPlayer != null && !localPlayer.finished) // && localPlayer.obstacleHits > 0
            {
                if (!multiplayer)
                {
                    time += Time.deltaTime;
                    if (time < 10) timeText.text = "0" + time.ToString("F2");
                    else timeText.text = time.ToString("F2");
                }
                else
                {
                    timeText.text = "1st";
                }
            }
        }
    }

    public IEnumerator CompleteLevel(int starCount)
    {
        yield return new WaitForSeconds(1);

        if (starCount == 3 && unlockCarIndex != 0 &&
            !PlayerPrefs.HasKey(SaveLoadManager.carsUnlockedStrings[unlockCarIndex]))
        {
            PlayerPrefs.SetInt(SaveLoadManager.carsUnlockedStrings[unlockCarIndex], 1);
            StartCoroutine(CarUnlockedPopUp());
        }

        // Activate stars one by one
        for (int i = 0; i < starCount; i++)
        {
            while(stars[i].transform.localScale.x < 2) // Pop up
            {
                stars[i].transform.localScale += Vector3.one * 0.1f;
                yield return null;
            }

            stars[i].transform.GetChild(0).gameObject.SetActive(true); // Activate star

            while (stars[i].transform.localScale.x > 1f) // Pop down
            {
                stars[i].transform.localScale -= Vector3.one * 0.1f;
                yield return null;
            }

            stars[i].transform.localScale = Vector3.one;

            yield return new WaitForSeconds(.5f);
        }
    }

    public void Pause()
    {
        if (localPlayer.finished || FadePanel.instance.panel.alpha > 0) return;

        pause = !pause;
        Cursor.visible = pause;
        pausePanel.SetActive(pause);

        if (pause)
        {
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void ResetLevel()
    {
        if (instance.multiplayer) return;

        Time.timeScale = 1;
        StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex));
    }

    public void DealWithCamera(int index) 
    {
        if (localPlayer != null)
        {
            if (index == 0) // If you load scene index 0 destroy, else activate
            {
                Destroy(localPlayer.localCamera.gameObject);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                localPlayer.localCamera.StopAllCoroutines();
                localPlayer.localCamera.target = localPlayer.transform;
                localPlayer.localCamera.gameObject.SetActive(false);
                localPlayer.localCamera.transform.SetParent(localPlayer.transform);
            }
        }
    }

    public void LoadLevelVoid(int index)
    {
        StartCoroutine(LoadLevel(index));
    }

    public void LoadLevelVoid(string sceneName)
    {
        StartCoroutine(LoadLevel(sceneName));
    }

    public void LoadNextLevel()
    {
        StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex + 1));
    }

    public IEnumerator LoadLevel(string sceneName)
    {
        FadePanel.instance.StartCoroutine(FadePanel.instance.FadeIn());

        while (true)
        {
            if (FadePanel.instance.panel.alpha == 1)
            {
                if (sceneName == "MainMenu") 
                {
                    DealWithCamera(0); 
                    Leave();
                }
                else DealWithCamera(1);
                SceneManager.LoadScene(sceneName);

                yield break;
            }

            yield return null;
        }
    }

    public IEnumerator LoadLevel(int index)
    {
        FadePanel.instance.StartCoroutine(FadePanel.instance.FadeIn());
        
        while (true)
        {
            if (FadePanel.instance.panel.alpha == 1)
            {
                DealWithCamera(index);

                SceneManager.LoadScene(index);
                yield break;
            }

            yield return null;
        }
    }

    public void FinishLevel(CarMovement car)
    {
        remainingText.gameObject.SetActive(false);
        timeText.gameObject.SetActive(false);
        fireworks.Play();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        car.StartCoroutine(car.Straighten());
        car.canAccelerate = false;
        car.finished = true;

        int stars = 0;

        if (time <= bestTime + 10) stars = 3;
        else if (time <= bestTime + 20) stars = 2;
        else if (time <= bestTime + 30) stars = 1;
        else stars = 0;

        StartCoroutine(CompleteLevel(stars));
    }

    public void Leave()
    {
        localPlayer.LeaveLobby();
    }

    public IEnumerator CarUnlockedPopUp()
    {
        carUnlockedPanel.SetActive(true);

        while (carUnlockedPanel.transform.position.y < 85)
        {
            carUnlockedPanel.transform.Translate(Vector3.up * 25);
            yield return null;
        }

        yield return new WaitForSeconds(3);

        while (carUnlockedPanel.transform.position.y > -285)
        {
            carUnlockedPanel.transform.Translate(Vector3.up * -25);
            yield return null;
        }

        carUnlockedPanel.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < carLanes.Length; i++)
        {
            Gizmos.DrawWireCube(Vector3.forward * carLanes[i], Vector3.one * laneSize);
        }

        Gizmos.DrawWireCube(new Vector3(transform.position.x + 30, transform.position.y, transform.position.z), new Vector3(120, 5, 50));
    }
}
