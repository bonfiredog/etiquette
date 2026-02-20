
#define Res1 vec2(textureSize(iChannel2,0))
#define Res (iResolution.xy)

vec4 getRand(vec2 pos)
{
    return texture(iChannel2,pos/Res1);
}

uniform float Colors;
uniform float ColorsBlur;
uniform float Outlines;
uniform float OutlinesBlur;
uniform float Noise;
uniform float Vignetting;
uniform float MasterFade;
uniform float VignWidth;
uniform float linear2Gamma;

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    fragColor=vec4i(1.);
    float sqSc=sqrt(Res.x/600.);
    float sq1Sc=sqrt(Res1.x/600.);
    fragColor-=texture(iChannel0,fragCoord/iResolution.xy,log2(OutlinesBlur*5.*sqSc)).z*Outlines*2.;
    vec4 c0=texture(iChannel1,fragCoord/iResolution.xy,1.5+log2(ColorsBlur*10.*sqSc))*.6+texture(iChannel1,fragCoord/iResolution.xy,.0+log2(ColorsBlur*10.*sqSc))*.4;
    if(linear2Gamma>.5) c0.xyz=LinearToGammaSpace(c0.xyz);
    fragColor*=mix(vec4i(1),c0,Colors);
    fragColor+=.5*Noise*(getRand(fragCoord.xy*.8).x-.5);
    // vignetting
    if(true)
    {
        vec2 scc=(fragCoord-.5*iResolution.xy)/iResolution.x;
        float vign = 1.-.3*dot(scc,scc);
        vign*=1.-.7*Vignetting*exp(-sin(fragCoord.x/iResolution.x*3.1416)/VignWidth);
        vign*=1.-.7*Vignetting*exp(-sin(fragCoord.y/iResolution.y*3.1416)*Res.y/Res.x/VignWidth);
        fragColor.xyz *= vign;
    }

    if(linear2Gamma>.5) fragColor.xyz=GammaToLinearSpace(fragColor.xyz);
    
    fragColor=mix(texture(iChannel1,fragCoord/iResolution.xy),fragColor,MasterFade);
    fragColor.w=1.;
}

