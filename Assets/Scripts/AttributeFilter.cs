using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;


[Serializable]
public class AttributeFilter
{
    // [Tooltip("The name of the attribute")]
    public string Attribute = "Undefined";

    // [Tooltip("The index of the attirbute in the dataSource Array")]
    public int idx = -1;

    // [Tooltip("Minimum filter value for the attribute")]
    // [Range(0.0f, 1.0f)]
    public float minFilter = 0.0f;

    // [Tooltip("Maximum filter value for the attribute")]
    // [Range(0.0f, 1.0f)]
    public float maxFilter = 1.0f;

    // [Tooltip("Minimum scaling value for the attribute")]
    // [Range(0.0f, 1.0f)]
    public float minScale = 0.0f;

    // [Tooltip("Maximum scaling value for the attribute")]
    // [Range(0.0f, 1.0f)]
    public float maxScale = 1.0f;

    public bool isGlobal = false;

    public AttributeFilter(int index, string attributeName, float minFilter, float maxFilter, float minScale, float maxScale, bool isGlobal=false) {
        this.Attribute = attributeName;
        this.idx = index;
        this.minFilter = minFilter;
        this.maxFilter = maxFilter;
        this.minScale = minScale;
        this.maxScale = maxScale;
        this.isGlobal = isGlobal;
    }

}