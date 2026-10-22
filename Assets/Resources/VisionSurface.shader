Shader "CyberExtraction/VisionSurface"
{
    Properties
    {
        _VisibilityLookup("Angular visibility",2D)="white" {}
        _WallFaceMap("Dungeon masonry",2D)="black" {}
        _DarknessAlpha("Maximum opacity",Range(0,1))=.92
        _ObstacleDarkness("Obstacle darkness",Range(0,1))=.85
        _EdgeSoftness("Wall softness",Float)=.7
        _AngularSoftness("Angular softness",Float)=1.8
        _OrganicEdge("Organic edge variation",Float)=.35
        _Pixelated("Pixelated scene",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        // The captured world is composited here; HUD and aim render afterwards.
        Blend One Zero
        Cull Off
        ZWrite Off
        ZTest Always
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 world:TEXCOORD0; };
            TEXTURE2D(_VisibilityLookup); SAMPLER(sampler_VisibilityLookup);
            TEXTURE2D(_WallFaceMap); SAMPLER(sampler_WallFaceMap);
            TEXTURE2D_X(_CameraSortingLayerTexture); SAMPLER(sampler_CameraSortingLayerTexture);
            CBUFFER_START(UnityPerMaterial)
                float4 _VisionOrigin, _LookupOrigin;
                float _RayCount, _VisionRange, _NearRadius, _VisionHalfAngle;
                float _DarknessAlpha, _ObstacleDarkness, _EdgeSoftness, _AngularSoftness, _OrganicEdge;
                float _Pixelated;
                float4 _WallFaceBounds;
                float _WallFaceMapEnabled;
            CBUFFER_END
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS);
                output.world=TransformObjectToWorld(input.positionOS).xy;
                return output;
            }
            float Noise(float2 position)
            {
                float2 cell=floor(position), phase=frac(position);
                phase=phase*phase*(3-2*phase);
                float4 seeds=float4(dot(cell,float2(127.1,311.7)),
                    dot(cell+float2(1,0),float2(127.1,311.7)),
                    dot(cell+float2(0,1),float2(127.1,311.7)),
                    dot(cell+1,float2(127.1,311.7)));
                float4 values=frac(sin(seeds)*43758.5453);
                return lerp(lerp(values.x,values.y,phase.x),lerp(values.z,values.w,phase.x),phase.y);
            }
            float WallVisibility(float angle,float distance,float softness,float wallFace)
            {
                float uv=frac(angle+.5/_RayCount);
                float2 ranges=SAMPLE_TEXTURE2D(_VisibilityLookup,sampler_VisibilityLookup,float2(uv,.5)).rg;
                float hit=lerp(ranges.r,ranges.g,wallFace)*_VisionRange;
                // Feather within the visible side. Only half a quantization step is
                // allowed beyond the exit; this removes the bright band behind walls.
                // Keep the wall face colored through its rear edge. Floor pixels
                // retain the original first-hit cutoff and inward feather.
                return 1-smoothstep(max(0,hit-softness*(1-wallFace)),hit+_VisionRange/510,distance);
            }
            float CenterVisibility(float angle,float distance,float softness,float wallFace)
            {
                float index=frac(angle)*_RayCount;
                float current=floor(index), phase=frac(index);
                float4 a=SAMPLE_TEXTURE2D(_VisibilityLookup,sampler_VisibilityLookup,
                    float2(frac((current+.5)/_RayCount),.5));
                float4 b=SAMPLE_TEXTURE2D(_VisibilityLookup,sampler_VisibilityLookup,
                    float2(frac((current+1.5)/_RayCount),.5));
                // At a wall endpoint, one ray is unobstructed. Extending the near
                // hit across that interval creates a hard triangular spur. Let
                // the existing visibility filter feather only this silhouette.
                if(a.b<.001 || b.b<.001) return 1;
                // Exact ray/segment intersection for consecutive hits on one wall.
                // Do not use the minimum radial distance (the source of prior teeth).
                float step=2*PI/_RayCount;
                bool sameSurface=a.b>.001 && abs(a.b-b.b)<.001;
                float rangeA=lerp(a.r,a.g,wallFace),rangeB=lerp(b.r,b.g,wallFace);
                // A rear masonry band can end at different cells even on the
                // same merged collider. Be conservative at those transitions.
                sameSurface=sameSurface && wallFace<.5;
                float hit=sameSurface ? sin(step)/max(.00001,sin((1-phase)*step)/max(.00001,rangeA*_VisionRange)
                    +sin(phase*step)/max(.00001,rangeB*_VisionRange)) : min(rangeA,rangeB)*_VisionRange;
                return 1-smoothstep(max(0,hit-softness*(1-wallFace)),hit+_VisionRange/510,distance);
            }
            half4 Fragment(Varyings input):SV_Target
            {
                float2 offset=input.world-_VisionOrigin.xy;
                float distance=length(offset);
                float relativeAngle=atan2(offset.y,offset.x)-_VisionOrigin.z;
                relativeAngle=abs(atan2(sin(relativeAngle),cos(relativeAngle)));
                // Same directional footprint as GameConfig.VisionRadiusAtAngle.
                float edge=smoothstep(_VisionHalfAngle*.55,_VisionHalfAngle,relativeAngle);
                float radius=lerp(_VisionRange,_NearRadius,edge);
                // World-fixed variation only within the edge: no animated flicker
                // and no change to authoritative ranges or the clear interior.
                float grain=.65*Noise(input.world*.65)+.35*Noise(input.world*1.3);
                float edgeDistance=distance+_OrganicEdge*grain;
                float radialFog=smoothstep(radius*.65,max(.001,radius),edgeDistance);
                float2 lookupOffset=input.world-_LookupOrigin.xy;
                float lookupDistance=length(lookupOffset);
                float angle=frac(atan2(lookupOffset.y,lookupOffset.x)/(2*PI)+1);
                float step=max(1/_RayCount,_AngularSoftness/max(lookupDistance,2)/(4*PI));
                // Gentle spatial variation breaks straight filter bands at silhouettes.
                // Keep the exact center angle for the wall guard below.
                float centerAngle=angle;
                step*=.85+.3*grain;
                angle+=(grain-.5)*step*.8;
                float softness=max(.001,_EdgeSoftness*(.85+.3*grain));
                float wallFace=0;
                if(_WallFaceMapEnabled>.5)
                {
                    float2 faceUV=(input.world-_WallFaceBounds.xy)/max(_WallFaceBounds.zw,float2(.001,.001));
                    if(all(faceUV>=0) && all(faceUV<1))
                        wallFace=SAMPLE_TEXTURE2D(_WallFaceMap,sampler_WallFaceMap,faceUV).r>.5 ? 1 : 0;
                }
                // Binomial filter over visibility comparisons, not shadow distances.
                // Nine taps remove the five-tap plateaus visible in the corridor.
                float visibility=(WallVisibility(angle-4*step,lookupDistance,softness,wallFace)
                    +8*WallVisibility(angle-3*step,lookupDistance,softness,wallFace)
                    +28*WallVisibility(angle-2*step,lookupDistance,softness,wallFace)
                    +56*WallVisibility(angle-step,lookupDistance,softness,wallFace)
                    +70*WallVisibility(angle,lookupDistance,softness,wallFace)
                    +56*WallVisibility(angle+step,lookupDistance,softness,wallFace)
                    +28*WallVisibility(angle+2*step,lookupDistance,softness,wallFace)
                    +8*WallVisibility(angle+3*step,lookupDistance,softness,wallFace)
                    +WallVisibility(angle+4*step,lookupDistance,softness,wallFace))/256;
                // Lateral samples soften silhouettes but cannot relight a point
                // behind a continuous wall. Occluded opacity stays at the SAME .782.
                visibility=min(visibility,CenterVisibility(centerAngle,lookupDistance,softness,wallFace));
                float obstacleFog=(1-visibility)*_ObstacleDarkness;
                // Outside the cone and behind a wall share the approved gray level.
                float alpha=_DarknessAlpha*max(radialFog*_ObstacleDarkness,obstacleFog);
                // Renderer2D does not initialize the fullscreen-pass _ScaleBiasRt.
                // Sample the copied color in raster coordinates, without that flip.
                float2 screenUV=input.positionCS.xy/_ScreenParams.xy;
                // Full-resolution capture in both modes. Quantize only scene color;
                // HUD, visibility distances and the authoritative cone stay unchanged.
                if(_Pixelated>.5)
                {
                    float2 grid=max(float2(1,1),floor(_ScreenParams.xy*.5));
                    screenUV=(floor(screenUV*grid)+.5)/grid;
                }
                half3 scene=SAMPLE_TEXTURE2D_X(_CameraSortingLayerTexture,sampler_CameraSortingLayerTexture,screenUV).rgb;
                half luminance=dot(scene,half3(.2126,.7152,.0722));
                float monochrome=saturate(max(radialFog,1-visibility));
                half3 color=lerp(scene,luminance.xxx,monochrome);
                return half4(color*(1-alpha),1);
            }
            ENDHLSL
        }
    }
}
