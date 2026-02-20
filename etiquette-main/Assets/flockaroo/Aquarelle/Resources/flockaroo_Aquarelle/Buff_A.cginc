// created by florian berger (flockaroo) - 2018
// © 2018 florian berger <flockaroo@gmail.com>

// aquarelle with pencil predraw

// prepare the image (mainly for or mipmapping)

#define Res0 vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))

#ifdef __UNITY3D__
uniform float SrcContrast;
uniform float SrcBrightness;
uniform float SrcColor;
uniform float linear2Gamma;
//uniform float SrcGamma;
#endif

void mainImage( out vec4 fragColor, vec2 fragCoord )
{
    vec2 uv = (fragCoord.xy-.5*Res1)*min(Res0.x/Res1.x,Res0.y/Res1.y)/Res0.xy+.5;
    vec4 c=texture(iChannel0,uv);
    #ifdef __UNITY3D__
    float br=dot(c.xyz,vec3i(.333));
//    c.xyz=pow(c.xyz,vec3i(SrcGamma));
    c.xyz=(c.xyz-br)*SrcColor+br;
    c.xyz=(c.xyz-.5)*SrcContrast+.5;
    c.xyz+=SrcBrightness-1.;
    if(linear2Gamma>.5) c.xyz=LinearToGammaSpace(c.xyz);
    #endif
    fragColor.xyz = c.xyz;
    fragColor.w = c.w;
}
