﻿using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
//using TMPro;
using UnityEngine.UI;

namespace Min_Max_Slider
{
	[RequireComponent(typeof(RectTransform))]
	public class MinMaxSlider : Selectable, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private enum DragState
		{
			Both,
			Min,
			Max
		}

		/// <summary>
		/// Floating point tolerance
		/// </summary>
		private const float FLOAT_TOL = 0.01f;

		[Header("UI Controls")]
		[SerializeField] private Camera customCamera = null;
		[SerializeField] private RectTransform sliderBounds = null;
		[SerializeField] private RectTransform minHandle = null;
		[SerializeField] private RectTransform maxHandle = null;
		[SerializeField] private RectTransform middleGraphic = null;

		// text components (optional)
		[Header("Display Text (Optional)")]
		//[SerializeField] private TextMeshProUGUI minText = null;
		//[SerializeField] private TextMeshProUGUI maxText = null;
		[SerializeField] private string minText = null;
		[SerializeField] private GameObject minTextGo = null;
		[SerializeField] private GameObject maxTextGo = null;
		[SerializeField] private string maxText = null;
		[SerializeField] private string textFormat = "0";

		// values
		[Header("Limits")]
		[SerializeField] private float minLimit = 0;
		[SerializeField] private float maxLimit = 100;

		[Header("Values")]
		public bool wholeNumbers;
		[SerializeField] public float minValue = 0;
		[SerializeField] public float maxValue = 100;

		public MinMaxValues Values { get { return new MinMaxValues(minValue, maxValue, minLimit, maxLimit); } }

		/// <summary>
		/// Event invoked when either slider value has changed
		/// <para></para>
		/// T0 = min, T1 = max
		/// </summary>
		[Serializable]
		public class SliderEvent : UnityEvent<float, float>
		{
		}

		public SliderEvent onValueChanged = new SliderEvent();

		private Vector3 dragStartPosition;
		private float dragStartMinValue01;
		private float dragStartMaxValue01;
		private DragState dragState;
		private readonly Vector3[] worldCorners = new Vector3[4];
		private readonly Vector3[] localCorners = new Vector3[4];
		private bool passDragEvents; // this allows drag events to be passed through to scrollers

		private Camera mainCamera;
		private Canvas parentCanvas;
		private bool isOverlayCanvas;

		protected override void Start()
		{
			base.Start();

			if (!sliderBounds)
				sliderBounds = transform as RectTransform;

			Camera uiCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
			
			parentCanvas = GetComponentInParent<Canvas>();
			isOverlayCanvas = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
			mainCamera = customCamera != null ? customCamera : uiCamera ? uiCamera : Camera.main;
		}

		public float[] GetPercentageValues() {
			var retArr = new float[2];
			retArr[0] = GetPercentage(minLimit, maxLimit, minValue);
			retArr[1] = GetPercentage(minLimit, maxLimit, maxValue);
			return retArr;
		}

		public void SetLimits(float minLimit, float maxLimit)
		{
			this.minLimit = wholeNumbers ? Mathf.RoundToInt(minLimit) : minLimit;
			this.maxLimit = wholeNumbers ? Mathf.RoundToInt(maxLimit) : maxLimit;
		}

		public void SetValues(MinMaxValues values)
		{
			SetValues(values.minValue, values.maxValue, values.minLimit, values.maxLimit);
		}

		public void SetValues(float minValue, float maxValue)
		{
			SetValues(minValue, maxValue, minLimit, maxLimit);
		}

		public void SetValues(float minValue, float maxValue, float minLimit, float maxLimit)
		{
			this.minValue = wholeNumbers ? Mathf.RoundToInt(minValue) : minValue;
			this.maxValue = wholeNumbers ? Mathf.RoundToInt(maxValue) : maxValue;
			SetLimits(minLimit, maxLimit);

			RefreshSliders();
			UpdateText();
			UpdateMiddleGraphic();

			// event
			onValueChanged.Invoke(this.minValue, this.maxValue);
		}

		public void SetValueLowImpact(float minValue, float maxValue) {
			this.minValue = wholeNumbers ? Mathf.RoundToInt(minValue) : minValue;
			this.maxValue = wholeNumbers ? Mathf.RoundToInt(maxValue) : maxValue;

			UpdateText();
			UpdateMiddleGraphic();

			// event
			onValueChanged.Invoke(this.minValue, this.maxValue);
		}

		private void RefreshSliders()
		{
			SetSliderAnchors();

			float clampedMin = Mathf.Clamp(minValue, minLimit, maxLimit);
			SetHandleValue01(minHandle, GetPercentage(minLimit, maxLimit, clampedMin));

			float clampedMax = Mathf.Clamp(maxValue, minLimit, maxLimit);
			SetHandleValue01(maxHandle, GetPercentage(minLimit, maxLimit, clampedMax));
		}

		private void SetSliderAnchors()
		{
			minHandle.anchorMin = new Vector2(0, 0.5f);
			minHandle.anchorMax = new Vector2(0, 0.5f);
			minHandle.pivot = new Vector2(0.5f, 0.5f);

			maxHandle.anchorMin = new Vector2(1, 0.5f);
			maxHandle.anchorMax = new Vector2(1, 0.5f);
			maxHandle.pivot = new Vector2(0.5f, 0.5f);
		}

		private void UpdateText()
		{

			minText = minValue.ToString(textFormat);
			maxText = maxValue.ToString(textFormat);
			
			if (minTextGo) minTextGo.GetComponent<Text>().text = minText;
			if (maxTextGo) maxTextGo.GetComponent<Text>().text = maxText;
			
		}

		private void UpdateMiddleGraphic()
		{
			if (!middleGraphic)
				return;

			middleGraphic.anchorMin = Vector2.zero;
			middleGraphic.anchorMax = Vector2.one;
			middleGraphic.offsetMin = new Vector2(minHandle.anchoredPosition.x, 0);
			middleGraphic.offsetMax = new Vector2(maxHandle.anchoredPosition.x, 0);
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			Vector3 hitPosition3D = new Vector3(eventData.position.x, eventData.position.y, eventData.pointerCurrentRaycast.gameObject.transform.position.z);
			var clickPosition = isOverlayCanvas
				? (Vector3) eventData.position
				: eventData.pointerCurrentRaycast.worldPosition;
			// clickPosition.z = eventData.pointerCurrentRaycast.gameObject.transform.position.z;
			var localClickPosition = transform.InverseTransformPoint(clickPosition);
			// Debug.Log("HIT OBJECT IS: " + eventData.pointerCurrentRaycast.gameObject.name);
			// GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			// sphere.transform.localScale = Vector3.one * 0.02f;
			// sphere.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
			// sphere.transform.position = clickPosition;

			// Debug.Log("clicked point is: " + clickPosition.x);
			// Debug.Log("local clicked point is: " + localClickPosition.x);
			// Debug.Log("min handle point is: " + minHandle.transform.localPosition.x);
			// Debug.Log("max handle point is: " + maxHandle.transform.localPosition.x);
			passDragEvents = Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y);

			if (passDragEvents)
			{
				PassDragEvents<IBeginDragHandler>(x => x.OnBeginDrag(eventData));
			}
			else
			{
				dragStartPosition = localClickPosition;
				// dragStartMinValue01 = GetValue01(minHandle.position.x);
				// dragStartMaxValue01 = GetValue01(maxHandle.position.x);
				dragStartMinValue01 = GetValue01Local(minHandle.localPosition.x);
				dragStartMaxValue01 = GetValue01Local(maxHandle.localPosition.x);
				// Debug.Log("d start: " + dragStartPosition);
				// Debug.Log("dstart min value: " + dragStartMinValue01);
				// Debug.Log("dstart max value: " + dragStartMaxValue01);
				// Debug.Log("IS WITHIN min RECT " + IsWithinRectLocal(minHandle, dragStartPosition));
				// Debug.Log("IS WITHIN max RECT " + IsWithinRectLocal(maxHandle, dragStartPosition));
				

				// set drag state
				if (dragStartPosition.x < minHandle.localPosition.x || IsWithinRectLocal(minHandle, dragStartPosition))
				{
					dragState = DragState.Min;
					minHandle.SetAsLastSibling();
				}
				else if (dragStartPosition.x > maxHandle.localPosition.x || IsWithinRectLocal(maxHandle, dragStartPosition))
				{
					dragState = DragState.Max;
					maxHandle.SetAsLastSibling();
				}
				else
					dragState = DragState.Both;
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
			var clickPosition = isOverlayCanvas
				? (Vector3) eventData.position
				: eventData.pointerCurrentRaycast.worldPosition;
			
			var localClickPosition = transform.InverseTransformPoint(clickPosition);

			if (passDragEvents)
			{
				PassDragEvents<IDragHandler>(x => x.OnDrag(eventData));
			}
			else if (minHandle && maxHandle)
			{
				SetSliderAnchors();

				if (dragState == DragState.Min || dragState == DragState.Max)
				{
					float dragPosition01 = GetValue01Local(localClickPosition.x);
					float minHandleValue = GetValue01Local(minHandle.localPosition.x);
					float maxHandleValue = GetValue01Local(maxHandle.localPosition.x);

					if (dragState == DragState.Min) {
						SetHandleValue01(minHandle, Mathf.Clamp(dragPosition01, 0, maxHandleValue));
						float minChanged = Mathf.Lerp(minLimit, maxLimit, GetValue01Local(minHandle.localPosition.x));
						SetValueLowImpact(minChanged, maxValue);
					}
					else if (dragState == DragState.Max) {
						SetHandleValue01(maxHandle, Mathf.Clamp(dragPosition01, minHandleValue, 1));
						float maxChanged = Mathf.Lerp(minLimit, maxLimit, GetValue01Local(maxHandle.localPosition.x));
						SetValueLowImpact(minValue, maxChanged);
					}
				}
				// else
				// {
				// 	var sliderBoundsRect = sliderBounds.rect;
				// 	var rectStart = sliderBoundsRect.position;
				// 	var rectEnd = rectStart;
				// 	rectEnd.x += sliderBoundsRect.width;

				// 	Vector3 rectEnd3d = new Vector3(rectEnd.x, rectEnd.y, eventData.pointerCurrentRaycast.gameObject.transform.position.z);
				// 	Vector3 rectStart3d = new Vector3(rectStart.x, rectStart.y, eventData.pointerCurrentRaycast.gameObject.transform.position.z);


				// 	var worldWidth = isOverlayCanvas
				// 		? sliderBoundsRect.width
				// 		: mainCamera.ScreenToWorldPoint(rectEnd3d).x - mainCamera.ScreenToWorldPoint(rectStart3d).x;

				// 	float distancePercent = (clickPosition.x - dragStartPosition.x) / worldWidth;
				// 	Debug.Log("IN DRAGing rect  drag positino is: " + rectStart3d);
				// 	Debug.Log("IN DRAGing rect end value: " + rectEnd3d);
				// 	Debug.Log("IN DRAGing rect world width value: " + worldWidth + " percent: " + distancePercent);
				// 	SetHandleValue01(minHandle, dragStartMinValue01 + distancePercent);
				// 	SetHandleValue01(maxHandle, dragStartMaxValue01 + distancePercent);
					
				// 	// set values
				// 	float min = Mathf.Lerp(minLimit, maxLimit, GetValue01(minHandle.position.x));
				// 	float max = Mathf.Lerp(minLimit, maxLimit, GetValue01(maxHandle.position.x));
					
				// 	SetValueLowImpact(min, max);

				// }


				UpdateText();
				UpdateMiddleGraphic();
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (passDragEvents)
			{
				PassDragEvents<IEndDragHandler>(x => x.OnEndDrag(eventData));
			}
			else
			{
				float minHandleValue = GetValue01Local(minHandle.localPosition.x);
				float maxHandleValue = GetValue01Local(maxHandle.localPosition.x);

				// this safe guards a possible situation where the slides can get stuck
				if (Math.Abs(minHandleValue) < FLOAT_TOL && Math.Abs(maxHandleValue) < FLOAT_TOL)
					maxHandle.SetAsLastSibling();
				else if (Math.Abs(minHandleValue - 1) < FLOAT_TOL && Math.Abs(maxHandleValue - 1) < FLOAT_TOL)
					minHandle.SetAsLastSibling();
			}
		}

		private void PassDragEvents<T>(Action<T> callback) where T : IEventSystemHandler
		{
			Transform parent = transform.parent;

			while (parent != null)
			{
				foreach (var component in parent.GetComponents<Component>())
				{
					if (!(component is T))
						continue;

					callback.Invoke((T) (IEventSystemHandler) component);
					return;
				}

				parent = parent.parent;
			}
		}

		/// <summary>
		/// Generates rectTransforms world corners
		/// </summary>
		private void GetWorldCorners()
		{
			sliderBounds.GetWorldCorners(worldCorners);
		}
		private void GetLocalCorners()
		{
			sliderBounds.GetLocalCorners(localCorners);
		}

		/// <summary>
		/// Sets handles position 
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="value01"></param>
		private void SetHandleValue01(Transform handle, float value01)
		{
			// GetWorldCorners();
			GetLocalCorners();
			
			Vector2 pos = new Vector2(
				Mathf.Lerp(localCorners[0].x, localCorners[2].x, value01),
				localCorners[0].y + (localCorners[1].y - localCorners[0].y) / 2f);
			handle.localPosition = new Vector3(pos.x, pos.y, handle.localPosition.z);
		}

		/// <summary>
		/// Returns a values from 0-1 based on this rects world corners
		/// </summary>
		/// <param name="worldPositionX"></param>
		/// <returns></returns>
		private float GetValue01(float worldPositionX)
		{
			GetWorldCorners();
			float posX = Mathf.Clamp(worldPositionX, worldCorners[0].x, worldCorners[2].x);
			return GetPercentage(worldCorners[0].x, worldCorners[2].x, posX);
		}
		private float GetValue01Local(float localPositionX)
		{
			GetLocalCorners();
			float posX = Mathf.Clamp(localPositionX, localCorners[0].x, localCorners[2].x);
			return GetPercentage(localCorners[0].x, localCorners[2].x, posX);
		}

		/// <summary>
		/// Returns percentage of input based on min and max values
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		private static float GetPercentage(float min, float max, float input)
		{
			return (input - min) / (max - min);
		}

		private static bool IsWithinRect(RectTransform rect, Vector2 worldPosition)
		{
			Vector3[] corners = new Vector3[4];
			rect.GetWorldCorners(corners);
			// Debug.Log("From ISWHITHIN RECT: world positino x is: " + worldPosition.x);
			// Debug.Log("From ISWHITHIN RECT: CORNERS ARE: " + corners[0].x  + " corner 2 " + corners[2].x);
			return worldPosition.x > corners[0].x - 0.001f && worldPosition.x < corners[2].x + 0.001f;
		}
		private static bool IsWithinRectLocal(RectTransform rect, Vector2 localPosition)
		{
			Vector3[] corners = new Vector3[4];
			rect.GetLocalCorners(corners);
			// Debug.Log("From ISWHITHIN RECT: local positino x is: " + localPosition.x);
			// Debug.Log("From ISWHITHIN RECT: CORNERS ARE: " + corners[0].x  + " corner 2 " + corners[2].x);
			return localPosition.x > corners[0].x - 0.001f && localPosition.x < corners[2].x + 0.001f;
		}

		[Serializable]
		public struct MinMaxValues
		{
			public float minValue, maxValue, minLimit, maxLimit;
			public static MinMaxValues DEFUALT = new MinMaxValues(25, 75, 0, 100);

			public MinMaxValues(float minValue, float maxValue, float minLimit, float maxLimit)
			{
				this.minValue = minValue;
				this.maxValue = maxValue;
				this.minLimit = minLimit;
				this.maxLimit = maxLimit;
			}
			
			/// <summary>
            /// Constructor for when values equal limits
            /// </summary>
            /// <param name="minValue"></param>
            /// <param name="maxValue"></param>
            public MinMaxValues(float minValue, float maxValue)
            {
            	this.minValue = minValue;
            	this.maxValue = maxValue;
            	this.minLimit = minValue;
            	this.maxLimit = maxValue;
            }
            
            public bool IsAtMinAndMax()
            {
            	return Math.Abs(minValue - minLimit) < FLOAT_TOL && Math.Abs(maxValue - maxLimit) < FLOAT_TOL;
            }

			public override string ToString()
			{
				//return $"Values(min:{minValue}, max:{maxValue}) | Limits(min:{minLimit}, max:{maxLimit})";
				return string.Format ("Values(min:{minValue}, max:{maxValue}) | Limits(min:{minLimit}, max:{maxLimit})");
			}
		}
	}
}