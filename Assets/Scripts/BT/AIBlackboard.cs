using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AIBlackboard
{
    public bool moveChase;
    public bool moveEvade;
    public bool attack;
    public Transform target;
}
