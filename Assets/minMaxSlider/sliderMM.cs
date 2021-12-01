using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sliderMM : MonoBehaviour {

	public Min_Max_Slider.MinMaxSlider MinMaxSlider;

	public float minvalue;
	public float maxvalue;

	void Start () {
		
	}
	

	void Update () {

		minvalue = MinMaxSlider.minValue;
		maxvalue = MinMaxSlider.maxValue;
	}
}
