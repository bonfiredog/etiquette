// created by florian berger (flockaroo) - 2018
// © 2018 florian berger <flockaroo@gmail.com>

// trying to resemble oil painting style

#define PI2 6.28318531

#define Res  iResolution.xy
#define Res0 vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))
#define Res2 vec2(textureSize(iChannel2,0))

uniform float linear2Gamma;

vec4 getCol(vec2 pos, float level)
{
    // preserve aspect ratio of original vid
	vec2 uv = (pos-Res*.5)*min(Res0.x/Res.x,Res0.y/Res.y)/Res0+.5;
    
	uv=clamp(uv,5./Res,1.-.5/Res);
    
    vec4 c1 = textureLod(iChannel0,uv,level);
    if(linear2Gamma>.5) c1.xyz=LinearToGammaSpace(c1.xyz);
    return c1;

    // green screen and bg
    uv = uv*vec2(-1,-1)*0.39+0.015*vec2i(sin(iTime*1.1),sin(iTime*0.271));
    // had to use .xxxw because tex on channel2 seems to be a GL_RED-only tex now (was probably GL_LUMINANCE-only before)
    vec4 c2 = vec4(0.5,0.7,1.0,1.0)*1.0*textureLod(iChannel2,uv,level).xxxw;
    float d=clamp(dot(c1.xyz,vec3(-0.5,1.0,-0.5)),0.0,1.0);
    return mix(c1,c2,1.8*d);
    return c1;
}

float getVal(vec2 pos, float level)
{
    return length(getCol(pos,level).xyz)+0.0001*length(pos-0.5*Res);
}
    
vec2 getGrad(vec2 pos,float delta)
{
    float l = log2(delta*Res0.x/Res.x);
    vec2 d=vec2(delta,0);
    return vec2(
        getVal(pos+d.xy,l)-getVal(pos-d.xy,l),
        getVal(pos+d.yx,l)-getVal(pos-d.yx,l)
    )/delta;
}

vec4 getRand(vec2 pos) 
{
    vec2 uv=pos/Res1;
    return texture(iChannel1,uv);
}

vec4 getRandBlueS(vec2 pos) 
{
    vec2 uv=pos/Res1;
    return texture(iChannel1,uv)-texture(iChannel1,uv+.5);
}

vec4 getRandBlue(vec2 pos) 
{
    vec2 uv=pos/Res1;
    vec4 c = clamp((texture(iChannel1,uv)-texture(iChannel1,uv+.5))*1.2+.5,0.,1.);
    //return c;
    return mix(c.xxxx,c,.3);
}

vec4 getPatt(vec2 pos) 
{
    vec2 uv=pos/Res1;
    vec4 s = sin(.23*pos.xyxy*PI2-vec4(0,0,PI2/4.,PI2/4.))*.5+.5;
    return vec4(
        dot(s.xy,vec2i(.5)),
        dot(s.zw,vec2i(.5)),
        dot(s.yz,vec2i(.5)),
        dot(s.wx,vec2i(.5))
        )
    ;
}

vec4 getColDist(vec2 pos)
{
	//return smoothstep(0.5,1.5,getCol(pos,0.)+getRand(pos).xxxx);
	return 1.-smoothstep(0.5,1.5,(1.-getCol(pos,0.))+pow(getRandBlue(pos),vec4i(.85))*.85);
	//return smoothstep(0.5,1.5,getCol(pos,0.)+mix(getPatt(pos),getRandBlue(pos),.75));
}

#define SampNum 32

uniform float strokeAngle;
uniform float smearStrength;
uniform float NumSamples;

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    vec2 pos0 = fragCoord;
    vec2 pos = pos0;
    vec3 col=vec3i(0);
    float cnt=0.0;
    float fact=1.0;
    float sc=sqrt(24./NumSamples);
    for(int i=0;i<SampNum;i++)
    {
        if (i>=int(NumSamples)) break;
        col+=fact*getColDist(pos).xyz;
        vec2 gr=vec2i(0);
        gr+=getGrad(pos,8.0*iResolution.x/600.);
        gr+=getGrad(pos,4.0*iResolution.x/600.);
        gr+=getGrad(pos,2.0*iResolution.x/600.);
        //gr+=getGrad(pos,1.0*iResolution.x/600.);
        
	    //float r = getRand(pos0*.03).x*2.-1.;
	    //r=-sign(r)*exp(-abs(r)/0.07);
	    float r = 0.;
	    #if 0
	    r=mix(
	        getRand(pos*.04+float(iFrame/10)*17.).x*2.-1.,
	        getRand(pos*.04+float(iFrame/10+1)*17.).x*2.-1.,
	        fract(float(iFrame)/10.)
	        );
	    r=iMouse.x/iResolution.x*exp(-abs(r)/0.05);
	    #endif
	
	    vec2 cs=cos(strokeAngle-vec2(0,1.6));
        vec2 d = cs.x*gr.yx*vec2(1,-1)+cs.y*gr;
        
        //sc=SC(getRandBlueS(pos0*.1).x);
        
	    #if 0
        if((int(iMouseData.w)&1)==1)
            pos+=iResolution.x/600.*mix(2.*d,normalize(d),.05/(.05+length(d)));
        else
        #endif
            pos+=.5*iResolution.x/600.*normalize(d)*smearStrength*sc;
        //fact*=0.87;
        cnt+=fact;
    }
    col/=fact*floor(NumSamples);
	fragColor = vec4(col,1.0);
	//fragColor.xyz = getPatt(pos0).xyz;
	//float r = getRand(pos0*.07).x*2.-1.;
	//r=exp(-r*r/0.03);
	//fragColor.xyz = vec3i(0)+r;
}

