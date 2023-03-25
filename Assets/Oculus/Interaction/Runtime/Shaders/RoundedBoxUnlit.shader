﻿/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Interaction/RoundedBoxUnlit"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)

        _BorderColor("BorderColor", Color) = (0, 0, 0, 1)

        // width, height, border radius, unused
        _Dimensions("Dimensions", Vector) = (0, 0, 0, 0)

        // radius corners
        _Radii("Radii", Vector) = (0, 0, 0, 0)

        // defaults to LEqual
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest [_ZTest]
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "../ThirdParty/Box2DSignedDistance.cginc"

            struct appdata
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                UNITY_VERTEX_OUTPUT_STEREO
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                fixed4 borderColor : TEXCOORD1;
                fixed4 dimensions : TEXCOORD2;
                fixed4 radii : TEXCOORD3;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _BorderColor)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Dimensions)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Radii)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.radii = UNITY_ACCESS_INSTANCED_PROP(Props, _Radii);
                o.dimensions = UNITY_ACCESS_INSTANCED_PROP(Props, _Dimensions);
                o.borderColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BorderColor);
                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = (v.uv-float2(.5f,.5f))*2.0f*o.dimensions.xy;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = sdRoundBox(i.uv, i.dimensions.xy - i.dimensions.ww * 2.0f, i.radii);
                float2 ddDist = float2(ddx(dist), ddy(dist));
                float ddDistLen = length(ddDist);

                float outerRadius = i.dimensions.w;
                float innerRadius = i.dimensions.z;

                float borderMask = (outerRadius > 0.0f | innerRadius > 0.0f)? 1.0 : 0.0;

                float outerDist = dist - outerRadius * 2.0;
                float outerDistOverLen = outerDist / ddDistLen;
                clip(1.0 - outerDistOverLen < 0.1f ? -1:1);

                float innerDist = dist + innerRadius * 2.0;
                float innerDistOverLen = innerDist / ddDistLen;

                float colorLerpParam = saturate(innerDistOverLen) * borderMask;
                float4 fragColor = lerp(i.color, i.borderColor, colorLerpParam);
                fragColor.a *= (1.0 - saturate(outerDistOverLen));
                return fragColor;
            }
            ENDCG
        }
    }
}