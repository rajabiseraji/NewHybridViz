// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Outline Dots" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "White" {}
		_BrushedTexture("Base (RGB)", 2D) = "White" {}
		_Size ("Size", Range(0, 30)) = 0.5
		_BrushSize("BrushSize",Float) = 0.05
		_MinX("_MinX",Float) = 0
		_MaxX("_MaxX",Float) = 0
		_MinY("_MinY",Float) = 0
		_MaxY("_MaxY",Float) = 0
		_MinNormX("_MinNormX", Float) = 0
		_MaxNormX("_MaxNormX", Float) = 0
		_MinNormY("_MinNormY", Float) = 0
		_MaxNormY("_MaxNormY", Float) = 0
		_MinNormZ("_MinNormZ", Float) = 0
		_MaxNormZ("_MaxNormZ", Float) = 0
		_data_size("data_size",Float) = 0
		_tl("Top Left", Vector) = (-1,1,0,0)
		_tr("Top Right", Vector) = (1,1,0,0)
		_bl("Bottom Left", Vector) = (-1,-1,0,0)
		_br("Bottom Right", Vector) = (1,-1,0,0)
	}

	SubShader 
	{
		Pass
		{
			Name "Onscreen geometry"
			// Tags { "RenderType"="Transparent" }
			//Blend func : Blend Off : turns alpha blending off
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off 
			Lighting Off 
			LOD 200
			Offset -1, -1
			ZTest LEqual
			Zwrite On
			Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		
			CGPROGRAM
				//// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
				//#pragma exclude_renderers d3d11 gles
				#pragma target 5.0
				#pragma vertex VS_Main
				#pragma fragment FS_Main
				#pragma geometry GS_Main
				#pragma multi_compile_instancing
				#include "UnityCG.cginc" 
				#include "Distort.cginc"
				#include "Helper.cginc"

				// **************************************************************
				// Data structures												*
				// **************************************************************
				
				//float brusedIndices[65000];

		        struct VS_INPUT {
          		    float4 position : POSITION;
            		float4 color: COLOR;
					// normal.x is the index of data 
					// normal.y is the size (I think it's the normalised size value) 
					// normal.y is basically an array of all points with their respective sizes
					float3 normal:	NORMAL; 
					// Add an isfiltered to here
					float4 uv_MainTex : TEXCOORD0;

					UNITY_VERTEX_INPUT_INSTANCE_ID
        		};
				
				struct GS_INPUT
				{
					float4	pos		: POSITION;
					float3	normal	: NORMAL;
					float2  tex0	: TEXCOORD0;
					float4  color		: COLOR;
					float	isBrushed : FLOAT;

					UNITY_VERTEX_INPUT_INSTANCE_ID 
					UNITY_VERTEX_OUTPUT_STEREO
				};

				struct FS_INPUT
				{
					float4	pos		: POSITION;
					float2  tex0	: TEXCOORD0;
					//float2	tex1	: TEXCOORD1;
					float4  color		: COLOR;
					float	isBrushed : FLOAT;
					float3	normal	: NORMAL;

					UNITY_VERTEX_OUTPUT_STEREO
				};

				struct f_output
                {
                    float4 color : COLOR;
                    float depth : SV_Depth;
                };


				// **************************************************************
				// Vars															*
				// **************************************************************

				UNITY_INSTANCING_BUFFER_START(Props)
					UNITY_DEFINE_INSTANCED_PROP(float, _Size)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MinSize)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MaxSize)

                    UNITY_DEFINE_INSTANCED_PROP(float, _BrushSize)

					//*******************
					// RANGE FILTERING
					//*******************

					UNITY_DEFINE_INSTANCED_PROP(float, _MinX)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MaxX)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MinY)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MaxY)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MinZ)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MaxZ)

					// ********************
					// Normalisation ranges
					// ********************


					UNITY_DEFINE_INSTANCED_PROP(float, _MinNormX)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MaxNormX)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MinNormY)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MaxNormY)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MinNormZ)
                    UNITY_DEFINE_INSTANCED_PROP(float, _MaxNormZ)

				UNITY_INSTANCING_BUFFER_END(Props)
				
				// float _Size;
				// float _MinSize;
				// float _MaxSize;
				// float _BrushSize;
				
				

				// float _MinX;
				// float _MaxX;
				// float _MinY;
				// float _MaxY;
				// float _MinZ;
				// float _MaxZ;

				// ********************
				// Normalisation ranges
				// ********************

				// float _MinNormX;
				// float _MaxNormX;
				// float _MinNormY;
				// float _MaxNormY;
				// float _MinNormZ;
				// float _MaxNormZ;

				float4x4 _VP;
				sampler2D _MainTex;
				sampler2D _BrushedTexture;

				//SamplerState sampler_MainTex;
				
				float _DataWidth;
				float _DataHeight;

				//float[] brushedIndexes;
				
				// **************************************************************
				// Shader Programs												*
				// **************************************************************

				// Vertex Shader ------------------------------------------------
				GS_INPUT VS_Main(VS_INPUT v)
				{
					// GS_INPUT output = (GS_INPUT)0;
					GS_INPUT output;

					UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_OUTPUT(GS_INPUT, output);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
					UNITY_TRANSFER_INSTANCE_ID(v, output);

					// Access instanced variables
                    float MinNormX = UNITY_ACCESS_INSTANCED_PROP(Props, _MinNormX);
                    float MaxNormX = UNITY_ACCESS_INSTANCED_PROP(Props, _MaxNormX);
                    float MinNormY = UNITY_ACCESS_INSTANCED_PROP(Props, _MinNormY);
                    float MaxNormY = UNITY_ACCESS_INSTANCED_PROP(Props, _MaxNormY);
                    float MinNormZ = UNITY_ACCESS_INSTANCED_PROP(Props, _MinNormZ);
                    float MaxNormZ = UNITY_ACCESS_INSTANCED_PROP(Props, _MaxNormZ);
					float MinX = UNITY_ACCESS_INSTANCED_PROP(Props, _MinX);
                    float MaxX = UNITY_ACCESS_INSTANCED_PROP(Props, _MaxX);
                    float MinY = UNITY_ACCESS_INSTANCED_PROP(Props, _MinY);
                    float MaxY = UNITY_ACCESS_INSTANCED_PROP(Props, _MaxY);
                    float MinZ = UNITY_ACCESS_INSTANCED_PROP(Props, _MinZ);
                    float MaxZ = UNITY_ACCESS_INSTANCED_PROP(Props, _MaxZ);
					
					float isFiltered = v.normal.z; // This is supposed to hold the isFiltered Value for each vertex

					// so the right way to brush is to assign a texture on top of the mesh
					// this texture comes right out of our compute shader, in this texture
					// if a certain area is colored with float4(1, 0,0,0) it means that it's brushed
					// so we do a texture lookup here on that spot of the texture to see whether or not the data
					// has been brushed

					//lookup the texture to see if the vertex is brushed...
					float2 indexUV = float2((v.normal.x % _DataWidth) / _DataWidth, (v.normal.x / _DataWidth) / _DataHeight);
					//float4 brushValue = tex2Dlod(_MainTex, float4(indexUV, 0.0, 0.0));
					float4 brushValue = tex2Dlod(_BrushedTexture, float4(indexUV, 0.0, 0.0));
					// TODO: uncomment this
					 output.isBrushed = brushValue.r;

					
					//output.isBrushed = v.normal.x;

					//TODO LATER: THIS REMAPS THE RANGE OF VALUES
					float3 normalisedPosition = float3(
						normaliseValue(v.position.x, MinNormX, MaxNormX ,-0.45, 0.45),
						normaliseValue(v.position.y, MinNormY, MaxNormY ,-0.45, 0.45),
						normaliseValue(v.position.z, MinNormZ, MaxNormZ ,-0.45, 0.45));

//					output.pos = ObjectToWorldDistort3d(v.position);// normalisedPosition);
					output.pos = ObjectToWorldDistort3d(normalisedPosition);

					//the normal buffer carries the index of each vertex
					output.tex0 = float2(0, 0);

					/*if(v.normal.x == 1.0) {
						output.color = float4(1.0,0.0,0.0,1.0);
					} else {
						output.color = v.color;
					}*/
					 output.color = v.color;
			
					//filtering
					if (v.position.x <= MinX ||
					 v.position.x >= MaxX || 
					 v.position.y <= MinY || 
					 v.position.y >= MaxY || 
					 v.position.z <= MinZ || 
					 v.position.z >= MaxZ 	||

					 normalisedPosition.x < -0.5 ||
					 normalisedPosition.x > 0.5 || 
					 normalisedPosition.y < -0.5 || 
					 normalisedPosition.y > 0.5 || 
					 normalisedPosition.z < -0.5 || 
					 normalisedPosition.z > 0.5	 ||
					 isFiltered)
					{
						output.color.w = 0;
					}
					output.normal = v.normal;
					return output;
				}



				// Geometry Shader -----------------------------------------------------
				[maxvertexcount(6)]
				void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
				{
					FS_INPUT pIn;

					UNITY_SETUP_INSTANCE_ID(p[0]);
					UNITY_INITIALIZE_OUTPUT(FS_INPUT, pIn);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(p[0]);

					// Access instanced variables
                    float Size = UNITY_ACCESS_INSTANCED_PROP(Props, _Size);
                    float MinSize = UNITY_ACCESS_INSTANCED_PROP(Props, _MinSize);
                    float MaxSize = UNITY_ACCESS_INSTANCED_PROP(Props, _MaxSize);

					// float3 up = UNITY_MATRIX_IT_MV[1].xyz;
					float brushSizeFactor = 1.0;
					//if (p[0].isBrushed == 1.0) brushSizeFactor = 1.2;
					if(p[0].isBrushed > 0.0) brushSizeFactor = 1.5;

					float3 look = _WorldSpaceCameraPos - p[0].pos;
					//look.y = 0;
					look = normalize(look);

					float3 up = float3(0, 1, 0);
					float3 right = cross(up, look);
					float sizeFactor = normaliseValue(p[0].normal.y, 0.0, 1.0, MinSize, MaxSize);
					float halfS = 0.01f * Size*sizeFactor * p[0].normal.y;
							
					float4 v[4];
					
					v[0] = float4(p[0].pos + halfS * right - halfS * up, 1.0f);
					v[1] = float4(p[0].pos + halfS * right + halfS * up, 1.0f);
					v[2] = float4(p[0].pos - halfS * right - halfS * up, 1.0f);
					v[3] = float4(p[0].pos - halfS * right + halfS * up, 1.0f);

					float4x4 vp = UNITY_MATRIX_VP;
					
					
					
					pIn.isBrushed = p[0].isBrushed;
					pIn.color = p[0].color;
					pIn.normal = p[0].normal;

					// pIn.pos = mul(vp, v[0]);
					pIn.pos = mul(vp, v[0]);
					pIn.tex0 = float2(1.0f, 0.0f);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(p[0], pIn);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);

					pIn.pos = mul(vp, v[1]);
					// pIn.pos = UnityObjectToClipPos(v[1]);
					pIn.tex0 = float2(1.0f, 1.0f);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(p[0], pIn);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);

					pIn.pos =  mul(vp, v[2]);
					// pIn.pos =  UnityObjectToClipPos(v[2]);
					pIn.tex0 = float2(0.0f, 0.0f);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(p[0], pIn);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);

					pIn.pos =  mul(vp, v[3]);
					// pIn.pos =  UnityObjectToClipPos(v[3]);
					pIn.tex0 = float2(0.0f, 1.0f);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(p[0], pIn);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);
					
					triStream.RestartStrip();
				}

				// Fragment Shader -----------------------------------------------
				float4 FS_Main(FS_INPUT input) : SV_Target0
				{
					// f_output output;

					// UNITY_INITIALIZE_OUTPUT(f_output, output);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

					//FragmentOutput fo = (FragmentOutput)0;
					float4 BRUSH_COLOR = float4(1.0f, 0.0f, 0.0f, 1.0f);
					
					// Sample _MainTex for colour which creates the circular dot, changing colour depending if it is brushed
                    // if (input.isBrushed > 0 )
                    //     output.color = tex2D(_MainTex, input.tex0.xy) * BrushColor;
                    // else
                    //     output.color = tex2D(_MainTex, input.tex0.xy) * input.color;;

                    // output.depth = output.color.a > 0.5 ? input.pos.z : 0;

                    // return output;

					float dx = input.tex0.x - 0.5f;
					float dy = input.tex0.y - 0.5f;

					float dt = dx * dx + dy * dy;
					
					if(input.color.w == 0)
					{
						//if( dt <= 0.2f)
						//	return float4(0.1,0.1,0.1,1.0);
						//else
						//	if(dx * dx + dy * dy <= 0.25f)
						//	return float4(0.0, 0.0, 0.0, 1.0);
						//	else
						//	{
							discard;
							return float4(0.0, 0.0, 0.0, 0.0);
//							}
					}
					else
					{
						if( dt <= 0.2f)
						{
							if(input.isBrushed==1.0)
								return float4(1.0,0.0,0.0,1.0);
							else
								return float4(input.color.x-dt*0.15,input.color.y-dt*0.15,input.color.z-dt*0.15,0.8);
						}// float4(input.color.x-dt*0.25,input.color.y-dt*0.25,input.color.z-dt*0.25,1.0);
						else
						if(dx * dx + dy * dy <= 0.21f)
							return float4(0.0, 0.0, 0.0, 1.0);
						else
						{
							discard;	
							return float4(0.1, 0.1, 0.1, 1.0);
						}
					}
					
					//return fo;
				}

			ENDCG
	
	}

	
}
}
