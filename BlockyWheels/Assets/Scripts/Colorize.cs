using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Colorize : MonoBehaviour
{
    public Material[] colors;
    public MeshRenderer[] parts;

    void Start()
    {
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].material = colors[Random.Range(0, colors.Length)];
        }
    }
}
