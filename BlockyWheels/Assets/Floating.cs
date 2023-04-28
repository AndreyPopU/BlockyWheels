using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floating : MonoBehaviour
{
    private CanvasGroup group;

    void Start()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;
        group = GetComponent<CanvasGroup>();

        StartCoroutine(Fade());
    }

    private void Update()
    {
        transform.Translate(Vector3.up * .025f);
    }

    IEnumerator Fade()
    {
        while (group.alpha < 1) { group.alpha += .1f; yield return null; }

        yield return new WaitForSeconds(1.5f);

        while (group.alpha > 0) { group.alpha -= .01f; yield return null; }

        Destroy(gameObject, 1);
    }
}
