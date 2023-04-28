using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour
{
    public float speed;
    public ParticleSystem explosion;
    public BoxCollider mark;
    private float startupTime = .5f;

    void Update()
    {
        if (startupTime > 0) startupTime -= Time.deltaTime;
        else
        {
            if (transform.parent.parent != null) transform.parent.transform.SetParent(null);
            transform.position -= Vector3.up * Time.deltaTime * speed;
            transform.Rotate(Vector3.up, 5);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == mark)
        {
            explosion.Play();
            ParticleSystem fire = GetComponentInChildren<ParticleSystem>();
            fire.transform.SetParent(transform.parent);
            var main = fire.main;
            main.loop = false;
            transform.parent.GetComponent<SphereCollider>().enabled = true;
            Invoke("DisableImpact", .02f);
            Destroy(transform.parent.gameObject, 5);
            mark.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    private void DisableImpact() { transform.parent.GetComponent<SphereCollider>().enabled = false; }
}
