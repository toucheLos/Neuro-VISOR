using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Startup : MonoBehaviour
{
    public Object[] geometry;
    // Start is called before the first frame update
    void loadInVrn()
    {
        geometry = Resources.LoadAll("geometry", typeof(Object));
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        loadInVrn();
        
    }
}
