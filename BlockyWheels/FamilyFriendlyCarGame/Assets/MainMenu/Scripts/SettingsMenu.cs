using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public Dropdown resolutionDropdown;
    public Dropdown qualityDropdown;
    public Slider sensitivitySlider;
    public Slider soundSlider;
    public Slider musicSlider;
    public Toggle controlsToggle;
    public GameObject mainMenu;
    public GameObject lobbyMenu;
    public GameObject settingsMenu;
    public CanvasGroup fadePanel;
    public Button[] transitionButtons;
    public Color activeColor;
    public Vector3 outOfScreen;
    public Texture2D cursorSprite;
    public AudioMixer soundMixer;
    public AudioMixer musicMixer;

    public float transitionSpeed;
    Resolution[] resolutions;

    private bool isFullscreen;

    void Start()
    {
        fadePanel.gameObject.SetActive(true);

        LoadSettings();

        //Vector2 hotSpot = new Vector2(cursorSprite.width / 2f, cursorSprite.height / 2f);
        //Cursor.SetCursor(cursorSprite, hotSpot, CursorMode.Auto);

        StartCoroutine(Fade());
    }

    public void FindResolution(bool saved)
    {
        int currentResolution = 0;
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].width + " x " + resolutions[i].height + " " + resolutions[i].refreshRate + "Hz");
            if (resolutions[i].width == Screen.currentResolution.width
                && resolutions[i].height == Screen.currentResolution.height
                && resolutions[i].refreshRate == Screen.currentResolution.refreshRate) currentResolution = i;
        }

        options.Add("nothing to see here");
        if (saved) currentResolution = resolutionDropdown.value = PlayerPrefs.GetInt(SaveLoadManager.resolutionString);

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolution;
        resolutionDropdown.RefreshShownValue();

        SetResolution(currentResolution);
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey(SaveLoadManager.qualityString)) qualityDropdown.value = PlayerPrefs.GetInt(SaveLoadManager.qualityString);
        else qualityDropdown.value = 0;
        qualityDropdown.RefreshShownValue();
        
        FindResolution(PlayerPrefs.HasKey(SaveLoadManager.resolutionString));

        if (PlayerPrefs.HasKey(SaveLoadManager.sensitivityString)) sensitivitySlider.value = PlayerPrefs.GetFloat(SaveLoadManager.sensitivityString);
        else sensitivitySlider.value = 2;

        if (PlayerPrefs.HasKey(SaveLoadManager.soundString)) soundSlider.value = PlayerPrefs.GetFloat(SaveLoadManager.soundString);
        else soundSlider.value = -40;

        if (PlayerPrefs.HasKey(SaveLoadManager.musicString)) musicSlider.value = PlayerPrefs.GetFloat(SaveLoadManager.musicString);
        else musicSlider.value = -40;

        if (PlayerPrefs.HasKey(SaveLoadManager.fullscreenString)) isFullscreen = SaveLoadManager.IntToBool(PlayerPrefs.GetInt(SaveLoadManager.fullscreenString));
        else isFullscreen = Screen.fullScreen;
        Fullscreen();

        if (PlayerPrefs.HasKey(SaveLoadManager.invertedControlsString)) controlsToggle.isOn = SaveLoadManager.IntToBool(PlayerPrefs.GetInt(SaveLoadManager.invertedControlsString));
        else controlsToggle.isOn = false;
    }

    public void OpenMenuVoid(Transform menu)  { StartCoroutine(OpenMenu(menu)); }

    public void CloseMenuRight(Transform menu) 
    {
        SaveLoadManager.SaveSettings(soundSlider.value, musicSlider.value, sensitivitySlider.value, SaveLoadManager.BoolToInt(isFullscreen),
            resolutionDropdown.value, qualityDropdown.value, controlsToggle.isOn);
        StartCoroutine(CloseMenu(menu, false)); 
    }
    public void CloseMenuLeft(Transform menu) { StartCoroutine(CloseMenu(menu, true)); }

    public void CloseButtonSet(Transform set)
    {
        StartCoroutine(ButtonSet(set, true));
    }

    public void OpenButtonSet(Transform set)
    {
        StartCoroutine(ButtonSet(set, false));
    }

    public IEnumerator OpenMenu(Transform menu)
    {
        foreach (Button button in transitionButtons)
            button.interactable = false;

        while (Mathf.Abs(menu.localPosition.x) > 2)
        {
            menu.localPosition = Vector3.MoveTowards(menu.localPosition, Vector3.zero, transitionSpeed * Time.deltaTime);
            yield return null;
        }

        menu.localPosition = Vector3.zero;

        foreach (Button button in transitionButtons)
            button.interactable = true;
    }

    public IEnumerator CloseMenu(Transform menu, bool left)
    {
        Vector3 pos = menu.localPosition;

        if (left)
        {
            pos -= Vector3.right * 2000;

            while (menu.localPosition.x - pos.x > 5)
            {
                menu.localPosition = Vector3.MoveTowards(menu.localPosition, pos, transitionSpeed * Time.deltaTime);
                yield return null;
            }
        }
        else
        {
            pos += Vector3.right * 2000;

            while (menu.localPosition.x - Mathf.Abs(pos.x) < -5)
            {
                menu.localPosition = Vector3.MoveTowards(menu.localPosition, pos, transitionSpeed * Time.deltaTime);
                yield return null;
            }
        }

        menu.localPosition = pos;
    }

    public IEnumerator ButtonSet(Transform buttonParent, bool left)
    {
        Vector3 pos = buttonParent.localPosition;

        if (left)
        {
            pos -= Vector3.right * 1000;

            while (buttonParent.localPosition.x - pos.x > 5)
            {
                buttonParent.localPosition = Vector3.MoveTowards(buttonParent.localPosition, pos, (transitionSpeed / 2) * Time.deltaTime);
                yield return null;
            }
        }
        
        if (!left)
        {
            pos += Vector3.right * 1000;

            while (buttonParent.localPosition.x - pos.x < -5)
            {
                buttonParent.localPosition = Vector3.MoveTowards(buttonParent.localPosition, pos, (transitionSpeed / 2) * Time.deltaTime);
                yield return null;
            }
        }

        buttonParent.transform.localPosition = pos;
    }

    public void Play()
    {
        SceneManager.LoadScene("CampaignScene");
    }

    // Quality
    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    // Fullscreen Mode
    public void FullscreenMode(int index)
    {
        FullscreenMode(index);
    }

    // Resolution
    public void SetResolution(int index)
    {
        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, isFullscreen);
    }

    // Sound
    public void SetSound(float sound)
    {
        soundMixer.SetFloat("sound", sound);
    }

    // Sound
    public void SetMusic(float sound)
    {
        musicMixer.SetFloat("sound", sound);
    }

    // Fullscreen
    public void Fullscreen()
    {
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            Text buttonText = EventSystem.current.currentSelectedGameObject.GetComponent<Button>().GetComponentInChildren<Text>();

            if (isFullscreen) { isFullscreen = false; buttonText.color = Color.white; }
            else { isFullscreen = true; buttonText.color = activeColor; }
        }

        Screen.fullScreen = isFullscreen;
    }

    public IEnumerator Fade()
    {
        if (fadePanel.alpha > 0)
        {
            while (fadePanel.alpha > 0)
            {
                fadePanel.alpha -= Time.deltaTime;
                // Fade out music
                yield return new WaitForSeconds(.02f);
            }
        }
        else if (fadePanel.alpha < 1)
        {
            Cursor.visible = false;
            while (fadePanel.alpha < 1)
            {
                fadePanel.alpha += 1.5f * Time.deltaTime;
                // Fade in music
                yield return new WaitForSeconds(.02f);
            }
        }
    }

    public void ExitGame() { StartCoroutine(Exit()); }

    public IEnumerator Exit()
    {
        StartCoroutine(Fade());

        while (fadePanel.alpha < 1) { yield return new WaitForSeconds(.02f); }

        print("quit");
        Application.Quit();
    }
}
