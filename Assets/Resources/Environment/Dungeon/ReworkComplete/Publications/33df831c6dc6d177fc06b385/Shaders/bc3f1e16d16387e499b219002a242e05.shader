Shader "CyberExtraction/DungeonFloorOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex("Transition mask",2D)="white" {}
        _FloorTex("Painted material",2D)="white" {}
        _Period("Material period in cells",Float)=4
        [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes
            {
                float3 positionOS:POSITION;
                float4 color:COLOR;
                float2 uv:TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                half4 color:COLOR;
                float2 uv:TEXCOORD0;
                float2 materialUV:TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_FloorTex); SAMPLER(sampler_FloorTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Period;
            CBUFFER_END
            Varyings Vertex(Attributes v)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                v.positionOS=UnityFlipSprite(v.positionOS,unity_SpriteProps.xy);
                float3 world=TransformObjectToWorld(v.positionOS);
                o.positionCS=TransformWorldToHClip(world);
                o.materialUV=(world.xy/1.06+64)/max(_Period,1);
                o.uv=v.uv;
                o.color=v.color*_Color*unity_SpriteColor;
                return o;
            }
            half4 Fragment(Varyings i):SV_Target
            {
                half mask=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                half4 painted=SAMPLE_TEXTURE2D(_FloorTex,sampler_FloorTex,frac(i.materialUV));
                return half4(painted.rgb*i.color.rgb,painted.a*mask*i.color.a);
            }
            ENDHLSL
        }
    }
}
