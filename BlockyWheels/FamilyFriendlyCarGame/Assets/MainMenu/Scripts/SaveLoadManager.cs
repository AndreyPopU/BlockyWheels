using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SaveLoadManager
{
    // Settings
    public static string qualityString = "qualityValue";
    public static string resolutionString = "resolutionValue";
    public static string sensitivityString = "sensitivity";
    public static string soundString = "sound";
    public static string musicString = "music";
    public static string fullscreenString = "fullscreen";
    public static string invertedControlsString = "invertedControls";

    // Gameplay
    public static int campaignLevel;
    public static string campaignLevelString = "CampaignLevel";

    // Best scores
    public static string[] bestTimeStrings = new string[] { "Empty", "BestTime01", "BestTime02", "BestTime03", "BestTime04", "BestTime05", "BestTime06", "BestTime07",
    "BestTime08", "BestTime09", "BestTime10", "BestTime11", "BestTime12", "BestTime13", "BestTime14", "BestTime15", "BestTime16", "BestTime17" };

    // Resolution, quality, sound, sensitivity, fullscreen
    public static void SaveSettings(float _sound, float _music, float _sensitivity, int _fullscreen, int _resolution, int _quality, bool _invertedControls)
    {
        PlayerPrefs.SetInt(qualityString, _quality);
        PlayerPrefs.SetInt(resolutionString, _resolution);
        PlayerPrefs.SetFloat(sensitivityString, _sensitivity);
        PlayerPrefs.SetFloat(soundString, _sound);
        PlayerPrefs.SetFloat(musicString, _music);
        PlayerPrefs.SetInt(fullscreenString, _fullscreen);
        PlayerPrefs.SetInt(invertedControlsString, BoolToInt(_invertedControls));
    }

    public static bool IntToBool(int convert)
    {
        if (convert == 1) return true;
        return false;
    }

    public static int BoolToInt(bool convert)
    {
        if (convert) return 1;
        return 0;
    }
}
