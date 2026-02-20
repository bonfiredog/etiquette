// created by florian berger (flockaroo) - 2018
// © 2018 florian berger <flockaroo@gmail.com>

// oil paint brush drawing

// final mixing/vignetting/fading

#define ImageTex iChannel0

#define Res  iResolution.xy
#define Res0 vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))
#define Res2 vec2(textureSize(iChannel2,0))
#define Res3 vec2(textureSize(iChannel3,0))

uniform float Vignette;

uniform float EffectFade;

uniform float PanFade;

uniform float linear2Gamma;

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    vec2 uv = fragCoord/Res;
    fragColor = texture(ImageTex,uv);
	fragColor.w=1.;
    vec2 uv2 = (fragCoord-.5*Res)*min(Res3.y/Res.y,Res3.x/Res.x)/Res3+.5;
    vec4 col0 = texture(iChannel3,uv2);
#ifdef HDRP_GAMMA_CORRECT
    // apply gamma to src image
    // FIXME: (should be done in an extra pre-passe - but here done by hand in here and in Geom_A at getColor)
    col0.xyz=exp2(log2(col0.xyz)/HDRPGamma);
#endif
    if(true)
    {
        vec2 scc=(fragCoord-.5*iResolution.xy)/iResolution.x;
        float vign = 1.0-Vignette*dot(scc,scc)*1.3;
        //float vign = 1.0;
        vign*=1.-Vignette*exp(-sin(fragCoord.x/iResolution.x*3.1416)*40.);
        vign*=1.-Vignette*exp(-sin(fragCoord.y/iResolution.y*3.1416)*20.);
        fragColor.xyz *= vign;
    }
    if(linear2Gamma>.5) fragColor.xyz=GammaToLinearSpace(fragColor.xyz);
    //fragColor += .3*texture(iChannel2,fragCoord/Res);
    fragColor = mix(fragColor,col0,EffectFade);
#define PANFADE_W 0.025
    fragColor.xyz=mix(fragColor.xyz,col0.xyz,smoothstep((1.-PanFade)-PANFADE_W,(1.-PanFade)+PANFADE_W,((1.-fragCoord.x/iResolution.x)-.5)/(1.+2.*PANFADE_W)+.5));

    //fragColor=texture(iChannel3,uv2);
    fragColor.w = 1.;
}

