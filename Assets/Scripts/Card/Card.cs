using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public void Initialize(Texture2D texture, Color color)
    {
        //TODO:: Find a better way to get the child object. getting by name is not entirely reliable.
        Transform planeTransform = transform.Find("CardPlane");
        if (planeTransform != null)
        {
            Renderer planeRenderer = planeTransform.GetComponent<Renderer>();
            if (planeRenderer != null)
            {
                Material mat = planeRenderer.material;
                mat.SetColor("_Color", color);
                
                if (texture != null)
                {
                    mat.SetTexture("_Texture", texture);
                }
            }
        }
    }

    void Update()
    {
        
    }
}
