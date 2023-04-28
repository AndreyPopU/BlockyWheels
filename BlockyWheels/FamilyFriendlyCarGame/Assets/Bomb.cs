using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public float timer;
    public float passTimer = 1;
    public ParticleSystem explosion;
    public bool passable;

    public Material[] materials;
    public CarMovement car;
    public MeshRenderer meshRenderer;

    private int index = 0;
    private float interval = 1;

    void Update()
    {
        if (passTimer > 0) passTimer -= Time.deltaTime;

        if (timer > 0)
        {
            timer -= Time.deltaTime;
            // tick tock
        }
        else Explode();
    }

    private void Explode()
    {
        car.ReduceSpeed(800);
        explosion.Play();
        gameObject.SetActive(false);
        interval = 2;
        CancelInvoke("FlickColor");
    }

    private void FlickColor()
    {
        // 5 - 1 -1 = 3 - .5 - .5 = 2 - .25 - .25

        if (index == 0)
        {
            meshRenderer.material = materials[1];
            index = 1;
        }
        else
        {
            meshRenderer.material = materials[0];
            index = 0;
            if (timer <= 5 && interval > .25f)  interval /= 2;
        }

        Invoke("FlickColor", interval);
    }

    public void Activate(float _timer)
    {
        passTimer = .5f;
        car.smoke.Play();
        passable = false;
        timer = _timer;
        interval = 1;
        Invoke("FlickColor", interval);
    }
}
