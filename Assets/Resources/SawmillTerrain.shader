Shader "CyberExtraction/SawmillTerrain"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _SurfaceTex("Surface",2D)="white"{}
        _WetTex("Wet soil",2D)="white"{}
        _MaskTex("Pilot mask",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
        _Region("Region min / size",Vector)=(0,0,128,128)
        _Road("Road",Float)=0
        _Legacy("Legacy road",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_SurfaceTex); SAMPLER(sampler_SurfaceTex);
            TEXTURE2D(_WetTex); SAMPLER(sampler_WetTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; float4 _Region; float _Road, _Legacy;
            CBUFFER_END
            Varyings Vert(Attributes i)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                i.positionOS=UnityFlipSprite(i.positionOS,unity_SpriteProps.xy);
                float3 world=TransformObjectToWorld(i.positionOS);
                o.positionCS=TransformWorldToHClip(world); o.world=world.xy; o.uv=i.uv; o.color=i.color*_Color*unity_SpriteColor; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 maskUV=(i.world-_Region.xy)/_Region.zw;
                half inside=step(0,maskUV.x)*step(0,maskUV.y)*step(maskUV.x,1)*step(maskUV.y,1);
                half4 mask=SAMPLE_TEXTURE2D(_MaskTex,sampler_MaskTex,maskUV);
                half coverage=mask.a*inside;
                if(_Legacy>.5)
                {
                    half4 old=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
                    old.a*=1-coverage; return old;
                }
                half4 surface=SAMPLE_TEXTURE2D(_SurfaceTex,sampler_SurfaceTex,i.world/8);
                half4 wet=SAMPLE_TEXTURE2D(_WetTex,sampler_WetTex,i.world/8);
                half wetness=mask.b*(_Road>.5 ? .12 : .5);
                half3 rgb=lerp(surface.rgb,wet.rgb,wetness);
                // Restrain fine contrast; broad variation is stored in the mask.
                half value=dot(rgb,half3(.299,.587,.114));
                rgb=lerp(value.xxx,rgb,.7)*(.82+mask.b*.12);
                half alpha=coverage*mask.g*(_Road>.5 ? mask.r : 1);
                clip(alpha-.003);
                return half4(rgb*i.color.rgb,alpha*i.color.a);
            }
            ENDHLSL
        }
    }
}
