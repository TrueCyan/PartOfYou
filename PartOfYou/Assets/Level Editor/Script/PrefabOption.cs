#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


[ExecuteInEditMode]
public class PrefabOption : MonoBehaviour
{
    //public bool rotatable;
    //public bool flippable;
    public bool showInEditor = true;

    public bool onlyOnFloorLayer;
    


    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying)
            Destroy(this);
    }
}

#endif