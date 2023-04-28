using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PowerUp : NetworkBehaviour
{
    public enum Type { invincible, reverse, spiketrap, hotPotato, globalBombs, nitro, time}

    public Type type;
    public GameObject smokePrefab;
    public GameObject spikePrefab;
    public GameObject floatingCanvasPrefab;
    public float duration;
    public bool bombExists;
    public bool chosenPowerUp;

    private void Start()
    {
        //ChooseRandom(Random.Range(0, 101));
    }

    void ChooseRandom(int chance)
    {
        if (GameManager.instance.multiplayer)
        {
            if (chance <= 10) type = Type.globalBombs; // Global bombs 10%
            else if (chance > 10 && chance <= 30) type = Type.hotPotato; // HotPotato 20%
            else if (chance > 30 && chance <= 40) type = Type.reverse; // Reverse 10%
            else if (chance > 40 && chance <= 60) type = Type.spiketrap; // Spiketrap 20%
            else if (chance > 60 && chance <= 80) type = Type.nitro; // Nitro 20%
            else if (chance > 80 && chance <= 100) type = Type.invincible; // Invincible 20%
        }
        else
        {
            if (chance <= 5) type = Type.reverse; // Reverse 5%
            else if (chance > 60 && chance <= 80) type = Type.nitro; // Nitro 20%
            else if (chance > 85 && chance <= 100) type = Type.invincible; // Invincible 15%
        }

        if (chance <= 30) // If it ought to be a bomb
        {
            if (!bombExists) // If there is no bomb yet, tell other power ups that haven't been randomised yet
            {
                PowerUp[] powerUps = transform.parent.GetComponentsInChildren<PowerUp>();
                foreach (PowerUp power in powerUps)
                {
                    if (!power.chosenPowerUp) power.bombExists = true;
                }
            }
            else // If a bomb already exists, change power up
            {
                ChooseRandom(Random.Range(31, 101));
                return;
            }
        }

        chosenPowerUp = true;
    }

    private void FixedUpdate()
    {
        transform.Rotate(Vector3.one);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CarMovement car))
        {
            if (car.pickedUpPower) return;

            if (type == Type.invincible) car.CmdShield(true);
            else if (type == Type.reverse) car.Reverse(true);
            else if (type == Type.spiketrap)
            {
                GameObject obj = Instantiate(spikePrefab, transform.position + Vector3.right * 7 - Vector3.up * 1.5f, Quaternion.identity);
                NetworkServer.Spawn(obj);
            }
            else if (type == Type.hotPotato) car.CmdBomb(car, 10, true, true);
            else if (type == Type.globalBombs)
            {
                List<Transform> cars = new List<Transform>();
                for (int i = 0; i < FindObjectsOfType<CarMovement>().Length; i++)
                    cars.Add(FindObjectsOfType<CarMovement>()[i].transform);

                foreach (Transform target in cars)
                {
                    if (target.GetComponent<CarMovement>() == car) continue;

                    target.GetComponent<CarMovement>().CmdBomb(target.GetComponent<CarMovement>(), 5, true, false);
                }
            }
            else if (type == Type.nitro)
            {
                car.nitro = 250;
                car.CmdNitro(true);
            }
            else if (type == Type.time)
            {
                Instantiate(floatingCanvasPrefab, transform.position, Quaternion.Euler(30, 180, 0));
                GameManager.instance.time -= 5;
            }

            car.pickedUpPower = true;
            car.StartCoroutine(car.PowerUpRemove(duration));
            Instantiate(smokePrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
