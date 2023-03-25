/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

#pragma vertex outlineVertex
#pragma fragment outlineFragment
#pragma multi_compile_local __ CONFIDENCE

#pragma prefer_hlslcc gles
#pragma exclude_renderers d3d11_9x
#pragma target 2.0

//

struct OutlineVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVertexOutput
{
    float4 vertex : SV_POSITION;
    half4 glowColor : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

OutlineVertexOutput outlineVertex(OutlineVertexInput v)
{
    OutlineVertexOutput o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    v.vertex.xyz += v.normal * _OutlineWidth;
    o.vertex = UnityObjectToClipPos(v.vertex);

    half4 maskPixelColor = tex2Dlod(_FingerGlowMask, v.texcoord);
    
#if CONFIDENCE
    int glowMaskR = maskPixelColor.r * 255;
    int jointMaskB = maskPixelColor.b * 255;

    int thumbMask = (glowMaskR >> 3) & 0x1;
    int indexMask = (glowMaskR >> 4) & 0x1;
    int middleMask = (glowMaskR >> 5) & 0x1;
    int ringMask = (glowMaskR >> 6) & 0x1;
    int pinkyMask = (glowMaskR >> 7) & 0x1;

    int joint0 = (jointMaskB >> 4) & 0x1;
    int joint1 = (jointMaskB >> 5) & 0x1;
    int joint2 = (jointMaskB >> 6) & 0x1;
    int joint3 = (jointMaskB >> 7) & 0x1;

    half jointIntensity = saturate(
        ((1 - saturate(glowMaskR)) * _JointsGlow[0])
        + thumbMask * (joint0 * _JointsGlow[1]
            + joint1 * _JointsGlow[2]
            + joint2 * _JointsGlow[3]
            + joint3 * _JointsGlow[4])
        + indexMask * (joint1 * _JointsGlow[5]
            + joint2 * _JointsGlow[6]
            + joint3 * _JointsGlow[7])
        + middleMask * (joint1 * _JointsGlow[8]
            + joint2 * _JointsGlow[9]
            + joint3 * _JointsGlow[10])
        + ringMask * (joint1 * _JointsGlow[11]
            + joint2 * _JointsGlow[12]
            + joint3 * _JointsGlow[13])
        + pinkyMask * (joint0 * _JointsGlow[14]
            + joint1 * _JointsGlow[15]
            + joint2 * _JointsGlow[16]
            + joint3 * _JointsGlow[17]));

    half4 glow = lerp(_OutlineColor, _OutlineJointColor, jointIntensity);
    o.glowColor.rgb = glow.rgb;
    o.glowColor.a = saturate(maskPixelColor.a + _WristFade) * glow.a * _OutlineOpacity;
#else
    o.glowColor.rgb = _OutlineColor;
    o.glowColor.a = saturate(maskPixelColor.a + _WristFade) * _OutlineColor.a * _OutlineOpacity;
#endif

    return o;
}

half4 outlineFragment(OutlineVertexOutput i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    return i.glowColor;
}
