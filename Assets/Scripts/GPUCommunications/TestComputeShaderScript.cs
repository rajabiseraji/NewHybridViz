using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestComputeShaderScript : MonoBehaviour
{

    //public ComputeShader testComputeShader;

    //public RenderTexture testDynamicRenderTexture;
    //// Start is called before the first frame update
    //void Start()
    //{
    //    testDynamicRenderTexture = new RenderTexture(256, 256, 24);
    //    testDynamicRenderTexture.enableRandomWrite = true;
    //    testDynamicRenderTexture.Create();

    //    testComputeShader.SetTexture(0, "Result", testDynamicRenderTexture);
    //    testComputeShader.SetFloat("Resolution", testDynamicRenderTexture.width);


    //    // first arg is which kernel to dispatch
    //    // second and third one are the number of threads to use 
    //    // it comes directly from the directive before CSMAin
    //    testComputeShader.Dispatch(0, testDynamicRenderTexture.width / 8, testDynamicRenderTexture.height / 8, 1);
    //}

    // Update is called once per frame
    void Update()
    {
        
    }
}
