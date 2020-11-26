using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorLerper : MonoBehaviour
{
    public SkinnedMeshRenderer smr;

    public Color color;

    public string applyToShaderParam;
    public string readFromShaderParam;

    Color newColor;

    public void UpdateAlpha(float lerp)
    {
        newColor = color;
        newColor.a = lerp;
        smr.material.SetColor(applyToShaderParam, newColor);
    }

    public void SetColor(Color color)
    {
        this.color = color;
    }

    public void SetColorFromMaterial(Material material)
    {
        this.color = material.GetColor(readFromShaderParam);
    }
}
