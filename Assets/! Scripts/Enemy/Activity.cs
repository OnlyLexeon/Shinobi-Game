using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Activity
{
    public enum ActivityType { GoTo, Idle }
    public ActivityType Type;

    [Header("GoTo")]
    public List<GameObject> pathPoints;
    [Header("Idle")]
    public GameObject idlePoint;
    public GameObject idleLookAt;
    public float idleTime;
}
