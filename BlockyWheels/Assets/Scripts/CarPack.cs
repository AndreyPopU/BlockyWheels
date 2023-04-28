using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarPack : MonoBehaviour
{

    // Position: closest car in pack.x + farthest car in the pack.x = n / 2 = m; position = car in pack.x + m;
    // -100 + 310 =  210 / 2 = 105 = -205
    //  -620 + 720 = 100 / 2 = 50 =-670

    [Tooltip("Width: (closest car in pack.x + farthest car in pack.x) / 2\nPos: closest car in pack + width")]
    public float width;
    private CarObstacle[] children;

    private void Awake()
    {
        // Get children
        children = GetComponentsInChildren<CarObstacle>();

        // Calculate width
        float closest = 9999;
        float farthest = -9999;

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].transform.position.x < closest) closest = children[i].transform.position.x;
            if (children[i].transform.position.x > farthest) farthest = children[i].transform.position.x;
        }

        width = Mathf.Abs((closest + farthest) / 2);

        foreach (CarObstacle child in children)
            child.transform.SetParent(null);

        // Calculate Position
        transform.position = new Vector3(closest + width, transform.position.y, transform.position.z);
    }

    void FixedUpdate()
    {
        float distance = Mathf.Abs(GameManager.instance.transform.position.x - transform.position.x + width);

        if (distance < 1)
        {
            foreach (CarObstacle child in children)
            {
                if (child != null) child.enabled = true;
            }

            Destroy(this);
        }
    }
}
