using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelfWhenLow : MonoBehaviour
{
    const float LowThreshold = -10.0f;

    // Update is called once per frame
    void Update()
    {
        if(this.transform.position.y < LowThreshold)
            GameObject.Destroy(this.gameObject);
    }
}
