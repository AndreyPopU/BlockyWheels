using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Test : NetworkBehaviour
{
    // assigned in inspector
    PlayerControls controls;
    public GameObject cubePrefab;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
        controls.Gameplay.Reset.performed += ctx => CmdDropCube();
    }

    [Command]
    void CmdDropCube()
    {
        if (cubePrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 2;
            Quaternion spawnRot = transform.rotation;
            GameObject cube = Instantiate(cubePrefab, spawnPos, spawnRot);
            NetworkServer.Spawn(cube);
        }
    }
}
