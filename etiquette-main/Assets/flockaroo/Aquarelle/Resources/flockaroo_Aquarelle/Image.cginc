// created by florian berger (flockaroo) - 2018
// © 2018 florian berger <flockaroo@gmail.com>

// aquarelle with pencil predraw

// final mixing and some paper-ish noise

#define BLACK_VIGN
#define Res vec2i(iResolution.xy)

vec4 getRand(vec2 pos)
{
    vec2 tres = vec2(textureSize(iChannel1,0));
    vec4 r=texture(iChannel1,pos/tres/sqrt(iResolution.x/600.)*vec2(1,1));
    return r;
}


vec4 paintCol(vec2 uv)
{
    //return vec4i(1);
    vec3 c=texture(iChannel2,uv).xyz;
    float m = dot(vec3i(.333),c);
    c=(c-m)*1.5+m;
    c=clamp(.3+.8*c,0.,1.);
    return vec4(c,1);
}

vec4 lineCol(vec2 uv)
{
    vec4 col=vec4i(0);
    col+=clamp(.5*texture(iChannel0,uv,0.),0.,2.);
    col+=.5*texture(iChannel0,uv,3.5);
    return col;
}

uniform float PredrawStrength;
uniform float MasterFade;
uniform vec3 PaperTint;
uniform float PaperRough;
uniform float Vignette;
uniform float BGAlpha;
uniform float UseMask;
uniform float linear2Gamma;

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    //vec4 r = getRand(fragCoord*1.3)-.5;
    vec4 r = getRand(fragCoord*1.1)-getRand(fragCoord*1.1+vec2(1,-1)*1.5);
    vec4 r2 = getRand(fragCoord*.015)-.5+getRand(fragCoord*.008)-.5;
    vec4 pc=paintCol(fragCoord/iResolution.xy);
    vec4 lc=lineCol(fragCoord/iResolution.xy).xxxx*(.75+.25*(1.-r.y));
    vec4 c = 1.-.15*(1.-0.*pow(dot(pc.xyz,vec3i(.333)),1.))*lc*PredrawStrength;
    c*=pc;
    c.xyz*=PaperTint;
    c.xyz*=mix(vec3i(1),.95+.06*r.xxx+.06*r.xyz,PaperRough*2.);
    //c=1.-.3*lc;
    //float s=sin(fragCoord.y/iResolution.y*3.1416*15.);
    //c-=.15*exp(-s*s*300.);
    fragColor = c;
    //fragColor = c;
    vec2 sc=(fragCoord-.5*iResolution.xy)/iResolution.x;
    float vign = 1.-.3*dot(sc,sc);
    //vign-=dot(exp(-sin(fragCoord/iResolution.xy*3.14)*vec2(20,10)),vec2(1,1));
    vign*=1.-.7*exp(-sin(fragCoord.x/iResolution.x*3.1416)*40.);
    vign*=1.-.7*exp(-sin(fragCoord.y/iResolution.y*3.1416)*20.);
    //fragColor.xyz=vec3i(dot(vec3(.33),fragColor.xyz))*vec3(0.7,0.8,1.)*1.2;
    
    fragColor.w=1.-BGAlpha*dot(fragColor.xyz,vec3i(.333));
#ifdef BLACK_VIGN
    fragColor.xyz *= mix(1.,vign,Vignette);
#endif
    vec4 srcColor=texture(iChannel3,fragCoord/iResolution.xy);
    fragColor.w=srcColor.w;
    float effectMask=mix(1.,texture(iChannel4,fragCoord/iResolution.xy).x,UseMask);
    if(linear2Gamma>.5) fragColor.xyz=GammaToLinearSpace(fragColor.xyz);
    fragColor=mix(srcColor,fragColor,MasterFade*effectMask);
    //fragColor*=sqrt(fragColor);
    //fragColor.w=1.;
    //fragColor=mix(1.-lc*.25,pc,0.3);
    //fragColor=lineCol(fragCoord/iResolution.xy);
}

