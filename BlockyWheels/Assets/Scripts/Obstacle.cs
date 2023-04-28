using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Obstacle : NetworkBehaviour
{
    public float speedReduce;
    public float launchSpeed;
    public bool launchable;
    public bool launched;
    public bool unfolds;
    public Collider coreCollider;

    public Rigidbody rb;
    protected ParticleSystem explosion;
    protected NetworkIdentity identity;
    public bool sentLaunch;
    public float hitSpeed;

    public virtual void Start()
    {
        identity = GetComponent<NetworkIdentity>();
        rb = GetComponent<Rigidbody>();
        explosion = GetComponentInChildren<ParticleSystem>();

        if (!isServer) return;

        foreach (CarObstacle car in FindObjectsOfType<CarObstacle>())
            Physics.IgnoreCollision(car.coreCollider, coreCollider);

        if (unfolds) StartCoroutine(Unfold());
    }

    [Command(requiresAuthority = false)]
    public void AssignAuthority(CarMovement car)
    {
        GetComponent<NetworkIdentity>().AssignClientAuthority(car.connectionToClient);
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CarMovement car))
        {
            car.ReduceSpeed(speedReduce);

            if (unfolds) return;

            if (GameManager.instance.multiplayer)
            {
                if (launchable)
                {
                    if (isServer) RpcLaunch(car.speed);
                    else
                    {
                        Launch(car.speed);
                        AssignAuthority(car);
                        //GetComponent<ObstaclePrediction>().owner = car;
                        //GetComponent<ObstaclePrediction>().hitSpeed = car.speed;
                    }
                }
            }
            else Launch(car.speed);
        }

        if (other.GetComponent<Impact>())
        {
            if (GameManager.instance.multiplayer)
            {
                if (launchable)
                {
                    if (isServer) RpcLaunch(1200);
                    else CmdLaunch(1200);
                }
            }
            else Launch(1200);

            this.enabled = false;
        }
    }

    [Command(requiresAuthority = false)]
    public virtual void CmdLaunch(float speed)
    {
        RpcLaunch(speed);
    }

    [ClientRpc]
    public virtual void RpcLaunch(float speed)
    {
        Launch(speed);
    }

    public virtual void Launch(float speed)
    {
        if (launched) return;

        if (explosion != null)
        {
            explosion.Play();
            explosion.transform.SetParent(null);
        }

        if (TryGetComponent(out Human human))
        {
            human.StopAllCoroutines();
            human.enabled = false;
            GetComponent<Animator>().enabled = false;
        }

        rb.freezeRotation = false;
        rb.useGravity = true;
        rb.freezeRotation = false;
        rb.AddForce(Vector3.up * (launchSpeed + speed) * Time.deltaTime, ForceMode.Impulse);
        rb.AddTorque(Vector3.forward * (launchSpeed + speed) + (Vector3.right * (launchSpeed + speed / 2)) * Time.deltaTime, ForceMode.Impulse);

        Invoke("DestroyInTime", 7);

        launched = true;
    }

    private void DestroyInTime()
    {
        NetworkServer.Destroy(gameObject);
    }

    private IEnumerator Unfold()
    {
        Transform gfx = transform.GetChild(0).transform;
        gfx.localScale = new Vector3(1,1,0);

        while (gfx.localScale.z < 1)
        {
            gfx.localScale += Vector3.forward * .05f;

            yield return null;
        }

        gfx.localScale = Vector3.one;
        unfolds = false;
    }
}
