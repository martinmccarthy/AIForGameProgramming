using System.Collections.Generic;
using UnityEngine;

public class EnableKeyword : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private List<Material> materials = new();
    
    void Start()
    {
        foreach(Material mat in materials)
        {
            mat.EnableKeyword("_ADDITIONAL_LIGHTS_ENABLED");
        }
    }
}
