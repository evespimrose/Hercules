using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AIBlackboard
{
    public bool moveChase;
    public bool moveEvade;
    public bool moveWander;
    public bool attack;
    public Transform target;

    // Wander params/state
    public Vector2 wanderAnchor;
    public float wanderMaxRange = 1.5f;
    public Vector2 wanderDestination;
}
