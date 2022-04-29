using Leap;
using Leap.Unity;
using UnityEngine;

// Helpful API docs here:
// https://docs.ultraleap.com/unity-api/class/class_leap_1_1_unity_1_1_hands.html1
// Named this script HandScript.cs and attached to the Service Provider

public class TestPinchingScript : MonoBehaviour
{
    void Update()
    {
        // Will select either left or right hand
        Hand hand = Hands.Left ?? Hands.Right;

        if (hand != null)
        {
            //Debug.Log(hand.PalmarAxis());
            //Debug.Log(hand.GetFistStrength());
            Debug.Log(hand.IsPinching());
        }
    }

}