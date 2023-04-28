using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CarSelection : MonoBehaviour
{
    public int current;
    public int selected;

    public GameObject[] carVariants;
    public CarScriptableObject[] carStats;
    public Button interactButton;
    public CanvasGroup mainMenuGroup;
    public GameObject selectText;
    public GameObject selectedText;
    public GameObject lockedText;

    [Header("Stats")]
    public Slider speedSlider;
    public Slider accelerationSlider;
    public Slider speedLossSlider;

    private Vector3 mouse;

    private void Start()
    {
        if (PlayerPrefs.HasKey(SaveLoadManager.selectedCar))
        {
            selected = PlayerPrefs.GetInt(SaveLoadManager.selectedCar);
            ChangeCar(selected);
            selectText.SetActive(false);
            selectedText.SetActive(true);
        }
        else ChangeCar(0);
    }

    private void Update()
    {
        mouse = Mouse.current.position.ReadValue();

        if (mouse.x < Screen.width / 2 - Screen.width / 20)
        {
            if (!mainMenuGroup.blocksRaycasts) mainMenuGroup.blocksRaycasts = true;
        }

        if (mouse.x > Screen.width / 2 - Screen.width / 20)
        {
            if (mainMenuGroup.blocksRaycasts) mainMenuGroup.blocksRaycasts = false;
        }
    }

    public void ChangeCar(int index)
    {
        StopAllCoroutines();
        carVariants[current].SetActive(false);
        current += index;
        if (current < 0) current = carVariants.Length - 1;
        if (current >= carVariants.Length) current = 0;
        CheckIfHasCar();
        carVariants[current].transform.rotation = Quaternion.Euler(0, 135, 0);
        carVariants[current].SetActive(true);
        StartCoroutine(UpdateStats(carStats[current]));
    }

    public IEnumerator UpdateStats(CarScriptableObject car)
    {
        bool completed = false;
        while(!completed)
        {
            if (speedSlider.value < car.bonusMaxSpeed)
                speedSlider.value += 20;
            else if (speedSlider.value > car.bonusMaxSpeed)
                speedSlider.value -= 20;

            if (accelerationSlider.value < car.bonusAccelerationSpeed)
                accelerationSlider.value += .25f;
            else if (accelerationSlider.value > car.bonusAccelerationSpeed)
                accelerationSlider.value -= .25f;

            if (speedLossSlider.value < car.bonusSpeedLoss)
                speedLossSlider.value += 5;
            else if (speedLossSlider.value > car.bonusSpeedLoss)
                speedLossSlider.value -= 5;

            if (speedSlider.value == car.bonusMaxSpeed && accelerationSlider.value == car.bonusAccelerationSpeed && speedLossSlider.value == car.bonusSpeedLoss)
                completed = true;

            yield return null;
        }
    }

    public void NextCar() { ChangeCar(1); }

    public void PreviousCar() { ChangeCar(-1); }

    private void CheckIfHasCar()
    {
        interactButton.interactable = false;
        selectText.SetActive(false);
        selectedText.SetActive(false);
        lockedText.SetActive(false);

        if (current == 0)
        {
            if (selected == current)
            {
                selectedText.SetActive(true);
            }
            else
            {
                selectText.SetActive(true);
                interactButton.interactable = true;
            }
            return;
        }

        if (selected == current)
        {
            selectedText.SetActive(true);
        }
        else
        {
            interactButton.interactable = true;
            selectText.SetActive(true);
        }

        if (PlayerPrefs.HasKey(SaveLoadManager.carsUnlockedStrings[current]) &&
            PlayerPrefs.GetInt(SaveLoadManager.carsUnlockedStrings[current]) != 0) // has car
        {
            if (selected == current)
            {
                selectedText.SetActive(true);
            }
            else
            {
                interactButton.interactable = true;
                selectText.SetActive(true);
            }
        }
        else // doesn't have car
        {
            interactButton.interactable = false;
            lockedText.SetActive(true);
            selectText.SetActive(false);
        }
    }

    public void SelectCar()
    {
        interactButton.interactable = false;
        selectText.SetActive(false);
        selectedText.SetActive(true);
        selected = current;
        PlayerPrefs.SetInt("SelectedCar", selected);
    }

}
