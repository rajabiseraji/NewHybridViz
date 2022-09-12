using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticResourceLoader
{
    public static ComputeShader BrushingComputeShader { get; private set; }
    public static Material MyRenderMaterial { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void LoadStaticAssets()
    {
        BrushingComputeShader = Resources.Load<ComputeShader>("BrushingComputeShader");
        MyRenderMaterial = Resources.Load<Material>("BrushedMaterial");
    }

}
