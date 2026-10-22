Shader "CyberExtraction/DungeonWater"
{
    Properties
    {
        [PerRendererData] _MainTex("Water sprite",2D)="white"{}
        _FlowTex("Continuous water detail",2D)="gray"{}
        _FlowScale("Detail scale (per world unit)",Float)=.4717
        _FlowSpeed("Surface drift (UV per second)",Range(0,.15))=.035
        _FlowBlend("Moving surface contribution",Range(0,1))=.55
        _Color("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
        _WaveScale("Wave scale",Range(.2,4))=1.4
        _WaveSpeed("Wave speed",Range(0,2))=.85
        _WaveStrength("Surface variation",Range(0,.2))=.12
        _HighlightStrength("Reflection strength",Range(0,.15))=.055
        _WaterBounds("Water bounds min / size",Vector)=(0,0,1,1)
        _ShoreWidth("Quiet shore width (world units)",Range(.05,1))=.4
        _Current("Current direction / speed",Vector)=(0,0,0,0)
        _RippleA("Drip A: world XY / interval / phase",Vector)=(0,0,0,0)
        _RippleB("Drip B: world XY / interval / phase",Vector)=(0,0,0,0)
        _RippleStrength("Drip ring strength",Range(0,.15))=.10
        _RippleRadius("Drip maximum radius",Range(.1,.45))=.38
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes
            {
                float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0;
                float2 world:TEXCOORD1; half4 color:COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_FlowTex); SAMPLER(sampler_FlowTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _WaveScale,_WaveSpeed,_WaveStrength,_HighlightStrength;
                float4 _WaterBounds,_Current;
                float _ShoreWidth;
                float4 _RippleA,_RippleB;
                float _RippleStrength,_RippleRadius;
                float _FlowScale,_FlowSpeed,_FlowBlend;
            CBUFFER_END
            Varyings Vert(Attributes i)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                i.positionOS=UnityFlipSprite(i.positionOS,unity_SpriteProps.xy);
                float3 world=TransformObjectToWorld(i.positionOS);
                o.positionCS=TransformWorldToHClip(world);
                o.world=world.xy; o.uv=i.uv; o.color=i.color*_Color*unity_SpriteColor;
                return o;
            }
            float Drip(float2 world,float4 source)
            {
                // Two fixed reusable GPU slots per surface. No particles,
                // spawning, allocations or global Unity random state.
                float interval=max(2.5,source.z);
                float cycle=floor((_Time.y+source.w)/interval);
                // Deterministic variation keeps successive drops irregular.
                float jitter=frac(sin(cycle*17.13+source.w*3.7)*43758.5453)*.65;
                float age=(_Time.y+source.w)-cycle*interval-jitter;
                float progress=saturate(age/1.65);
                float distanceToDrop=length(world-source.xy);
                float radius=lerp(.025,_RippleRadius,progress);
                float aa=max(fwidth(distanceToDrop),.004);
                float ring=1-smoothstep(.008,.018+aa,abs(distanceToDrop-radius));
                float inner=1-smoothstep(.006,.015+aa,abs(distanceToDrop-radius*.66));
                float envelope=smoothstep(0,.12,age)*(1-progress)*(1-progress);
                // Reduce energy when a ring becomes smaller than a pixel,
                // instead of turning it into a bright flickering dot.
                float coverage=saturate(.014/max(.014,aa));
                return (ring+inner*.35)*envelope*coverage*step(.01,source.z);
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // Keep original sprite UVs and alpha: no sampling across atlas
                // tiles, moving silhouettes, or deformation of stone boundaries.
                half4 surface=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
                float2 local=i.world-_WaterBounds.xy;
                float2 edge=min(local,_WaterBounds.zw-local);
                float quiet=smoothstep(0,max(.001,_ShoreWidth),min(edge.x,edge.y));
                float motion=.15+.85*quiet;
                float2 p=(i.world-_Current.xy*(_Time.y*_Current.z))*_WaveScale;
                float t=_Time.y*_WaveSpeed;
                float a=sin(dot(p,float2(1.3,.8))-t);
                float b=sin(dot(p,float2(-.7,1.7))+t*.73);
                // A separate repeatable texture uses world UVs, never sprite
                // atlas UVs. Actual surface detail drifts across tile boundaries.
                float2 drift=float2(.7,.45)*(_Time.y*_FlowSpeed)
                    +_Current.xy*(_Time.y*_Current.z*_FlowScale);
                float2 flowUV=i.world*_FlowScale-drift+float2(a,b)*.018;
                half3 flowing=SAMPLE_TEXTURE2D(_FlowTex,sampler_FlowTex,flowUV).rgb*i.color.rgb;
                surface.rgb=lerp(surface.rgb,flowing,_FlowBlend*quiet);
                float variation=(a+b)*.5;
                float crest=saturate((a*.65+b*.35-.55)*2.2);
                crest=crest*crest;
                // Small non-emissive glints; the normal world/vision composite
                // still occludes the surface, just like the original sprites.
                surface.rgb=surface.rgb*lerp(.92,1,quiet)*(1+variation*_WaveStrength*motion)
                    +half3(.48,.53,.43)*crest*_HighlightStrength*quiet;
                float ripples=Drip(i.world,_RippleA)+Drip(i.world,_RippleB);
                surface.rgb+=half3(.48,.53,.43)*ripples*_RippleStrength*quiet;
                return surface;
            }
            ENDHLSL
        }
    }
}
