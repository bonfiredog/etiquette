// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/demoscene_bg" {
   Properties {
      _Cube ("Environment Map", Cube) = "white" {}
      _Rand256 ("rand tex", 2D) = "noise256" {}
   }

   SubShader {
      Tags { "Queue"="Background"  }

      Pass {
         ZWrite Off 
         Cull Off

         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag

         // User-specified uniforms
         samplerCUBE _Cube;
         sampler2D _Rand256;

         #include "glsl2Cg.cginc"
         #define PI2 6.2832
         #define iChannel1 _Rand256
         #define iTime (_Time.y)

         struct vertexInput {
            float4 vertex : POSITION;
            float3 texcoord : TEXCOORD0;
         };

         struct vertexOutput {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
         };

         vertexOutput vert(vertexInput input)
         {
            vertexOutput output;
            output.vertex = UnityObjectToClipPos(input.vertex);
            output.texcoord = input.texcoord;
            return output;
         }

         float hash2(float seed)
         {
             return fract(sin(seed)*158.5453 );
         }

         vec4 noise(vec2 coord) {
             coord*=256.;
             vec2 cf=floor(coord),cc=cf+1.;
             return mix(
                 mix(hash2(cf.x+123.*cf.y),hash2(cc.x+123.*cf.y),coord.x-cf.x),
                 mix(hash2(cf.x+123.*cc.y),hash2(cc.x+123.*cc.y),coord.x-cf.x),
                 coord.y-cf.y
                       );
             return texture(iChannel1,coord);
         }

         vec4 getRand(vec2 coord)
         {
             vec4 c=vec4(0,0,0,0);

             c+=noise(coord+.003*iTime);
             c+=noise(coord/2.+.003*iTime)*2.;
             c+=noise(coord/4.+.003*iTime)*4.;
             c+=noise(coord/8.+.003*iTime)*8.;
             return c/(1.+2.+4.+8.);
         }

         #define FloorZ -.5
         vec4 myenv(vec3 pos, vec3 dir, vec3 sun, float period)
         {
             float ph=iTime*.2;
             float th=sin(iTime)*.2;
             dir.xy=mul(mat2(cos(ph),sin(ph),-sin(ph),cos(ph)),dir.xy);
             dir.yz=mul(mat2(cos(th),sin(th),-sin(th),cos(th)),dir.yz);
             vec3 colHor=vec3(.5,.55,.6);
             vec3 colSky=mix(vec3(1.5,.75,0.)*.8,vec3(.6,.8,1)*1.2,clamp(7.*dir.z,0.,1.));
             vec3 skyPos=pos+dir/abs(dir.z)*(20.-pos.z);
             float cloudPat=(.8+.6*(getRand(skyPos.xy*.001).x-.5));
             colSky*=mix(1.,cloudPat,step(0.,dir.z));
             vec3 colFloor=vec3(1.,.8,.6)*.65*.8;
             vec3 colScale=vec3(.0,.3,.5);
             vec3 floorPos=pos-dir/dir.z*(pos.z-FloorZ);
             vec2 s;
             float scale=1.;
             s=sin(floorPos.xy*PI2*.5*period);
             scale*=(1.-.3*exp(-s.x*s.x/.01))*(1.-.3*exp(-s.y*s.y/.01));
             s=sin(floorPos.xy*PI2*.5/10.*period);
             scale*=(1.-.5*exp(-s.x*s.x/.001))*(1.-.5*exp(-s.y*s.y/.001));
             colFloor=mix(colFloor,colScale,1.-scale)*(.3+getRand(floorPos.xy*.001).x);
             //vec3 dp=floorPos-vec3(pos0.xy,FloorZ);
             //colFloor*=1.-exp(-dot(dp,dp));

             //colSky*=1.+(1.-smoothstep(.01,.03,acos(dot(dir,sun))))*vec3(1.5,1.,.5);
             colSky*=1.+.3*(dot(dir,sun))*vec3(.5,1.,1.5);

             vec3 col=mix(colSky,colFloor,1.-smoothstep(-.01,.01,dir.z));
             col+=pow(max(0.,dot(normalize(dir),normalize(vec3(1,-1,1)))),30.);
             //col=colFloor;
             //col=mix(colHor,col,clamp(abs(dir.z*6.)-.1,0.,1.));
             //col*=sin(atan(dir.y,dir.z)*10.);
             //col*=sin(atan(dir.z,dir.x)*10.);
             //if(dir.z>0.) return vec4(1);
             return vec4(col,1);
         }


         fixed4 frag (vertexOutput input) : COLOR
         {
             //return float4(input.texcoord,1);
             float4 col=myenv(vec3(0,0,2.5), normalize(input.texcoord.xzy), normalize(vec3(cos(iTime*.6-.5),sin(iTime*.6-.5),.6)), 2.0);
             col.w=1.0;
             return col;
         }
         ENDCG 
      }
   } 	
}