using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeparatesStatic : MonoBehaviour
{
    public Transform level;

    void Start()
    {
        Separate();
    }

    void Separate()
    {
        // Create static and non static parent
        GameObject staticGO = new GameObject("Static");
        GameObject nonstaticGO = new GameObject("Non-Static");
        staticGO.transform.SetParent(level);
        nonstaticGO.transform.SetParent(level);

        // Organise everything
        int childCount = level.childCount;
        List<Transform> thingsToSort = new List<Transform>();
        
        foreach (Transform child in level)
        {
            thingsToSort.Add(child);
        }


        for (int i = 0; i < childCount; i++)
        {
            if (thingsToSort[i].name == "Static" || thingsToSort[i].name == "Non-Static") continue;
            if (thingsToSort[i].GetComponent<MapPiece>()) thingsToSort[i].transform.SetParent(staticGO.transform);
            else thingsToSort[i].transform.SetParent(nonstaticGO.transform);
        }

        
    }
}
