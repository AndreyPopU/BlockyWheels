using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Car", menuName = "New Car")]
public class CarScriptableObject : ScriptableObject
{
    public GameObject GFX;
    public Vector3 colliderCenter;
    public Vector3 colliderDimensions;
    public float shieldScale;
    public float bonusMaxSpeed;
    public float bonusAccelerationSpeed;
    public float bonusSpeedLoss;
    public float bonusAccelerationDelay; 
}
