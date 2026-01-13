using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [Header("Card Settings")]
    public float flipSpeed = 180f;

    private Guid cardId = Guid.Empty;
    
    public void Initialize(Texture2D texture, Color color, Guid Id)
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
        
        cardId = Id;
    }

    public void FlipCard()
    {
        StartCoroutine(Flip());
    }

    IEnumerator Flip()
    {
        float rotated = 0f;
        while (rotated < 180f)
        {
            float step = flipSpeed * Time.deltaTime;
            transform.Rotate(0, 0, step);
            rotated += step;
            yield return null;
        }
    }

    void Update()
    {
        
    }
}
