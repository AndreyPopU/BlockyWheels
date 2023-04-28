using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateTerrain : MonoBehaviour
{
    public bool doneGenerating;
    public float gridSquare;
    public LayerMask raycastLayerMask;
    public GameObject spawnPrefab;
    public GameObject spawnPrefab2;
    public Vector2 minMaxX;
    public Vector2 minMaxZ;

    private void Start()
    {
        StartCoroutine(GenerateDetails());
    }

    private void Update()
    {
        
    }

    IEnumerator GenerateDetails()
    {
        for (float i = minMaxX.x; i < minMaxX.y; i += gridSquare) // Width
        {
            for (float j = minMaxZ.x; j < minMaxZ.y; j += gridSquare) // Height
            {
                int nothing = Random.Range(1, 3);
                if (nothing == 2) continue;

                bool raycastHit = false;

                float randomX = Random.Range(-gridSquare, gridSquare);
                float randomZ = Random.Range(-gridSquare, gridSquare);
                randomX /= 2;
                randomZ /= 2;
                Vector3 position = new Vector3(i + randomX, 100, j + randomZ);
                Quaternion randomRotation = Quaternion.Euler(new Vector3(Random.Range(-5f, 5f), Random.Range(0, 360), Random.Range(-5f, 5f)));

                RaycastHit hit;
                Ray ray = new Ray(position, Vector3.down);

                if (Physics.Raycast(ray, out hit, 250f, raycastLayerMask)) // If raycast doesn't hit anything use mouseposition with camera farClipPlane
                {
                    position.y = hit.point.y;

                    raycastHit = true;
                }

                if (!raycastHit) continue;

                int chance = Random.Range(0, 4);
                if (chance == 2) Instantiate(spawnPrefab2, position, randomRotation);
                else Instantiate(spawnPrefab, position, randomRotation);

                yield return null;
            }
        }

        doneGenerating = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void SpawnObject(float minPosX, float maxPosX, float minPosY, float maxPosY, int heightMin, int heightMax, GameObject[] list, int amount)
    {
        for (int k = 0; k < amount; k++)
        {
            bool raycastHit = false;

            float randomX = Random.Range(minPosX, maxPosX);
            float randomZ = Random.Range(minPosY, maxPosY);
            randomX /= 2;
            randomZ /= 2;
            Vector3 position = new Vector3(randomX, 100, randomZ);

            RaycastHit hit;
            Ray ray = new Ray(position, Vector3.down);

            if (Physics.Raycast(ray, out hit, 250f, raycastLayerMask)) // If raycast doesn't hit anything use mouseposition with camera farClipPlane
            {
                position.y = hit.point.y;

                raycastHit = true;
            }

            if (!raycastHit) { k--; continue; }

            if (position.y > heightMin && position.y < heightMax) Instantiate(list[Random.Range(0, list.Length)], position, Quaternion.identity);
            else { k--; continue; }
        }
    }
}
