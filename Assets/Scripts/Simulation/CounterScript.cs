using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CounterScript : MonoBehaviour
{
    public Transform target = null;
    // Start is called before the first frame update
    void Start()
    {
        // get target empty from children
        target = transform.Find("target");

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
