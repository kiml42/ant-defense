using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtectMe : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UiPlane.RegisterProtectMe(this);
    }
}
