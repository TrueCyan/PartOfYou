using UnityEngine;
using System.Collections;

public class Layer : MonoBehaviour
{

    public Layer(int layerID, int layerPriority)
    {
        id = layerID;
        priority = layerPriority;

    }

    public int id;
    public int priority;

}