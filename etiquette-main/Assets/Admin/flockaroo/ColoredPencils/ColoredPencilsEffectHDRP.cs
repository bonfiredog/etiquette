//#define USE_HDRP
#if USE_HDRP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using Random=UnityEngine.Random;
//using UnityEditor;

namespace Flockaroo
{
//[AddComponentMenu("Image Effects/Artistic/ColoredPencils")]
[Serializable, VolumeComponentMenu("Post-processing/Custom/Flockaroo/ColoredPencils")]
public sealed class ColoredPencilsEffectHDRP : CustomPostProcessVolumeComponent, IPostProcessComponent {
    private Shader shader = null;
    private Shader gradShader = null;
    private Texture2D RandTex = null;
    private Material mat = null;
    private Material gradMat = null;
    private int actWidth = 0;
    private int actHeight = 0;

    [Header("Input/Output")]

    [Tooltip("take a texture as input instead of the camera")]
    public TextureParameter inputTexture = new TextureParameter(null);
    [Tooltip("render to a texture instead of the screen")]
    public BoolParameter renderToTexture = new BoolParameter(false);
    [Tooltip("texture being rendered to if above is checked")]
    public RenderTextureParameter outputTexture = new RenderTextureParameter(null);
    [Tooltip("generate mipmap for output texture")]
    public BoolParameter outputMipmap = new BoolParameter(false);

    [Header("Effect Masking")]
    [Tooltip("take a texture as effect mask (effect only activated where mask is white)")]
    public TextureParameter effectMaskTexture = new TextureParameter(null);
    //public BoolParameter effectMaskEnable=true;
    public ClampedFloatParameter effectMaskFade  = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
    //public float effectMaskOffs=0.0f;
    //public float effectMaskClampMin=0.0f;
    //public float effectMaskClampMax=1.0f;

    [Header("Main faders")]
    public ClampedFloatParameter fade = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
    public ClampedFloatParameter panFade = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
    [Header("Source")]
    public ClampedFloatParameter brightness = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    public ClampedFloatParameter contrast = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    public ClampedFloatParameter color = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    [Header("Effect")]
    [Tooltip("effect was originally made for gamma colorspace. if you use linear space (default in recent unity) check this box to get better results")]
    public BoolParameter linear2Gamma = new BoolParameter(true);
    public ClampedIntParameter shaderMethod = new ClampedIntParameter(0, 0, 2);
    private int shaderMethodOld=0;
    public ClampedFloatParameter outlines = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    public ColorParameter outlineColor  = new ColorParameter(new Color(0.0f,0.0f,0.0f));
    public ClampedFloatParameter hatches = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    public ClampedFloatParameter outlineError = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    public ClampedFloatParameter flicker = new ClampedFloatParameter(0.3f, 0.0f, 1.0f);
    public ClampedFloatParameter flickerFreq = new ClampedFloatParameter(10.0f, 0.0f, 100.0f);
    public ClampedFloatParameter fixedHatchDir = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
    public BoolParameter precalcGradient = new BoolParameter(false);
    public BoolParameter precalcGradientFlipY = new BoolParameter(false);
    public ClampedFloatParameter hatchScale  = new ClampedFloatParameter(1.0f, 0.7f, 1.5f);
    public ClampedFloatParameter hatchAngle = new ClampedFloatParameter(0.0f, 0.0f, 10.0f);
    public ClampedFloatParameter hatchLength = new ClampedFloatParameter(10.0f, 0.001f, 200.0f);
    public ClampedFloatParameter mipLevel = new ClampedFloatParameter(0.0f, 0.0f, 3.0f);
    private bool useMipmaps=false;
    public ClampedFloatParameter vignetting = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
    public ClampedFloatParameter contentVignetting = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
    [Header("Background")]
    public ColorParameter paperTint  = new ColorParameter(new Color(1.0f,0.97f,0.85f));
    public ClampedFloatParameter paperRoughness  = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    public TextureParameter paperTex = new TextureParameter(null);
    [Header("Other")]
    public BoolParameter flipY = new BoolParameter(false);
    [Tooltip("check this if you use linear color space in HDRP")]
    public BoolParameter HDRPGamma = new BoolParameter(false);
    private RenderTexture rtmip = null;
    private RenderTexture rtgrad = null;
    private RenderTexture inputrt = null;
    private Mesh mesh;
    private bool isShaderooGeom = false;
    private RenderTexture mySrc = null;
    List<Mesh> meshes;
        
        // Use this for initialization

        /*protected Material mat
        {
            get
            {
                if (m_Material == null)
                {
                    m_Material = new Material(shader);
                    m_Material.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_Material;
            }
        }*/

        void initShader()
        {

            if (shader == null)
            {
                if     (shaderMethod.value==0)
                    shader = Resources.Load<Shader>("flockaroo_ColoredPencils/imageEffShaderHDRP");
                else 
                    shader = Resources.Load<Shader>("flockaroo_ColoredPencils/imageEffShader"+shaderMethod.value+"HDRP");
                /*else if(shaderMethod==10)
                    shader = Resources.Load<Shader>("flockaroo_ColoredPencils/imageEffShader10");
                else
                    shader = Resources.Load<Shader>("flockaroo_ColoredPencils/imageEffShader");*/
            }
            if (gradShader == null)
            {
                gradShader = Resources.Load<Shader>("flockaroo_ColoredPencils/gradientPrecalc");
            }
            //if (shader == null)
            //    shader = Resources.Load<Shader>("Assets/pencil-effect/imageEffShader");
        }

        RenderTexture createRenderTex(int w = -1, int h = -1, bool mip = false, int aa = 1)
        {
            RenderTexture rt;
            //if(w==-1) w=Screen.width;
            //if(h==-1) h=Screen.height;
            if(w==-1) w=actWidth;
            if(h==-1) h=actHeight;
            rt = new RenderTexture(w, h,0,RenderTextureFormat.ARGBFloat);
            rt.antiAliasing=aa; // must be 1 for mipmapping to work!!
            rt.useMipMap=mip;
            return rt;
        }

        void initRandTex()
        {
            //if (RandTex == null)
            //    RandTex = Resources.Load<Texture2D>("rand256");
            if (RandTex == null)
            {
                //RandTex = new Texture2D(256, 256, TextureFormat.RGBAFloat, true);
                //RandTex = new Texture2D(256, 256, TextureFormat.RGBAHalf, true);
                if(shaderMethod.value==10)
                    RandTex = new Texture2D(64, 64, TextureFormat.RGBA32, true);
                else
                    RandTex = new Texture2D(256, 256, TextureFormat.RGBA32, true);

                for (int x = 0; x < RandTex.width; x++)
                {
                    for (int y = 0; y < RandTex.height; y++)
                    {
                        float r = Random.Range(0.0f, 1.0f);
                        float g = Random.Range(0.0f, 1.0f);
                        float b = Random.Range(0.0f, 1.0f);
                        float a = Random.Range(0.0f, 1.0f);
                        RandTex.SetPixel(x, y, new Color(r, g, b, a) );
                    }
                }

                RandTex.Apply();
            }
        }
		
		void initMipmapRenderTexture(RenderTexture src)
		{
                    if(rtmip == null || rtmip.width!=src.width || rtmip.height!=src.height)
                    {
                        rtmip = createRenderTex(src.width, src.height, true, 1);
                        //new RenderTexture(src.width, src.height,0,RenderTextureFormat.ARGB32);
#if UNITY_5_5_OR_NEWER
                        //rtmip.autoGenerateMips=false;
#endif
                    }

		}

		void initGradRenderTexture(RenderTexture src)
		{
                    int W=src.width/2;
                    int H=src.height/2;
		    if(rtgrad == null || rtgrad.width!=W || rtgrad.height!=H)
                    {
                        rtgrad = createRenderTex(W,H);
                             //new RenderTexture(src.width/4, src.height/4,0,RenderTextureFormat.ARGB32);
#if UNITY_5_5_OR_NEWER
                        //rtmip.autoGenerateMips=false;
#endif
                    }
		}

        void myStart () {
            //Camera cam = GetComponent<Camera>();
            //cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
            initShader();
            initRandTex();
            if (isShaderooGeom)
            {
              meshes = new List<Mesh>();
              int trinum = 300000;
              int maxMeshSize = 0x10000/3*3;
              int mnum = (trinum*3+maxMeshSize-1)/maxMeshSize;
              for(int j=0;j<mnum;j++)
              {
	        mesh = new Mesh();
                meshes.Add(mesh);
                mesh.Clear();
                //GetComponent<MeshFilter>().mesh = mesh;
                int vnum = maxMeshSize;
                Vector3[] verts = new Vector3 [vnum];
                //Vector2[] uvs   = new Vector2 [vnum];
                int[] tris  = new int [vnum];
                for(int i=0;i<vnum;i++)
                {
                    verts[i].x=i+j*maxMeshSize;
                    verts[i].y=1;
                    verts[i].z=2;
                    //uvs[i].x=i;
                    //uvs[i].y=0;
                    tris[i]=i;
                }
	        mesh.vertices = verts;
                //mesh.uv = uvs;
	        mesh.triangles = tris;
              }  
            }
	}
	
	// Update is called once per frame
	void myUpdate () {
		
	}
        
public bool IsActive() => fade.value < 1f;

// Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > HDRP Default Settings).
public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

const string kShaderName = "Hidden/Shader/ImageHDRP";

public override void Setup()
{
    myStart();
    /*if (Shader.Find(kShaderName) != null)
        m_Material = new Material(Shader.Find(kShaderName));
    else
        Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume New Post Process Volume is unable to load.");*/
}

public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle src_h, RTHandle dest_h) {
   RenderTexture src=src_h.rt;
   RenderTexture dest=dest_h.rt;
   src.filterMode=FilterMode.Trilinear;

   
            actWidth = src.width;
            actHeight = src.height;

            //RenderTexture mySrc=createRenderTex(src.width, src.height);
                //RenderTexture mySrc = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.RGB111110Float);
            //if(mySrc = new RenderTexture(src.width, src.height, 0, src.graphicsFormat);
            //cmd.CopyTexture(src, 0, mySrc1, 0);
            //RenderTexture mySrc1 = createRenderTex(src.width, src.height, true);
            if(mySrc==null || mySrc.width!=src.width || mySrc.height!=src.height)
                  mySrc = new RenderTexture(src.width, src.height, 0, src.graphicsFormat);
                  mySrc.filterMode=FilterMode.Bilinear;
            cmd.CopyTexture(src, 0, mySrc, 0);
            //cmd.Blit(mySrc1, mySrc);
            if(inputTexture.value)
            {
                if(inputrt==null || inputrt.width!=inputTexture.value.width || inputrt.height!=inputTexture.value.height)
                    inputrt =  createRenderTex(inputTexture.value.width, inputTexture.value.height);
                mySrc = inputrt;
                    //new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGBFloat);
                cmd.Blit(inputTexture.value, mySrc);
            }

            if(renderToTexture.value)
            {
                if(outputTexture.value==null
                   || outputTexture.value.width!=mySrc.width
                   || outputTexture.value.height!=mySrc.height)
                {
                    if(inputTexture.value) {
                        outputTexture.value = createRenderTex(inputTexture.value.width, inputTexture.value.height );
                    } else {
                        outputTexture.value = createRenderTex(mySrc.width, mySrc.height );
                    }
                    //outputTexture = new RenderTexture(mySrc.width, mySrc.height, 0, RenderTextureFormat.ARGBFloat);
                    if(outputMipmap.value)
                    {
                        outputTexture.value.antiAliasing=1; // must be for mipmapping to work!!
                        outputTexture.value.useMipMap=true;
                        outputTexture.value.filterMode=FilterMode.Trilinear;
                    }
                }
            }
            else
                outputTexture.value = null;

            if(shaderMethodOld!=shaderMethod.value) { shader=null; mat=null; RandTex=null; shaderMethodOld=shaderMethod.value; }

            initShader();
            initRandTex();

            if(precalcGradient.value)
            {
                initGradRenderTexture(src);
                //if ( gradShader!=null )
                //{
                    if (gradMat == null)
                    {
                        gradMat = new Material(gradShader);
                        gradMat.hideFlags = HideFlags.HideAndDontSave;
                    }
                    gradMat.SetTexture("_MainTex", mySrc);
		    gradMat.SetFloat("flipY", precalcGradientFlipY.value?1.0f:0.0f);
                    gradMat.SetFloat("iResolutionWidth", actWidth);
                    gradMat.SetFloat("iResolutionHeight", actHeight);
                    cmd.Blit(src, rtgrad, gradMat);
                //}
            }

            if (mat == null)
            {
                mat = new Material(shader);
                mat.hideFlags = HideFlags.HideAndDontSave;
            }

            mat.SetFloat("iResolutionWidth", actWidth);
            mat.SetFloat("iResolutionHeight", actHeight);

            mat.SetTexture("_PaperTex", paperTex.value);
            mat.SetTexture("_RandTex", RandTex);
            mat.SetTexture("_MaskTex", effectMaskTexture.value);
            if(precalcGradient.value)
                mat.SetTexture("_GradTex", rtgrad);
            mat.SetFloat("precalcGradient", precalcGradient.value?1.0f:0.0f);
            mat.SetFloat("effectFade", fade.value);
            mat.SetFloat("panFade", panFade.value);
            mat.SetFloat("brightness", brightness.value);
            mat.SetFloat("contrast", contrast.value);
            mat.SetFloat("colorStrength", color.value);
            mat.SetFloat("flicker", flicker.value);
            mat.SetFloat("flickerFreq", flickerFreq.value);
            mat.SetFloat("fixedHatchDir", fixedHatchDir.value);
            mat.SetFloat("outlines", outlines.value);
            mat.SetColor("outlineColor", outlineColor.value);
            mat.SetFloat("hatches", hatches.value);
            mat.SetFloat("outlineRand", outlineError.value);
            mat.SetFloat("vignetting", vignetting.value);
            mat.SetFloat("contentWhiteVign", contentVignetting.value);
            mat.SetFloat("hatchScale", hatchScale.value);
            mat.SetFloat("hatchLen", hatchLength.value);
            mat.SetFloat("hatchAngle", hatchAngle.value);
            mat.SetFloat("mipLevel", mipLevel.value);
            mat.SetFloat("flipY", flipY.value?1.0f:0.0f);
            mat.SetColor("paperTint", paperTint.value);
            mat.SetFloat("paperRough", paperRoughness.value);
            mat.SetFloat("paperTexFade", (paperTex.value==null)?0.0f:1.0f);

            mat.SetFloat("effectMaskFade",    effectMaskFade.value);
            mat.SetFloat("gammaHDRP", HDRPGamma.value?2.2f:1.0f);
            mat.SetFloat("linear2Gamma", linear2Gamma.value?1.0f:0.0f);
            //mat.SetFloat("effectMaskOffs",     effectMaskOffs);
            //mat.SetFloat("effectMaskClampMin", effectMaskClampMin);
            //mat.SetFloat("effectMaskClampMax", effectMaskClampMax);

            mat.SetInt("_FrameCount", Time.frameCount);
            useMipmaps=(mipLevel.value>0.0001f);

            if (useMipmaps)
            {
                initMipmapRenderTexture(mySrc);
                cmd.Blit(mySrc, rtmip);
#if UNITY_5_5_OR_NEWER
                //rtmip.GenerateMips();
#endif
                cmd.SetRenderTarget(dest);
                mat.SetTexture("_MainTex", rtmip);
		cmd.Blit(rtmip, dest, mat);
                //rtmip.filterMode = FilterMode.Trilinear;
            }
            else
            {
                mat.SetTexture("_MainTex", mySrc);

                if(isShaderooGeom)
                {
					//initMipmapRenderTexture(src);
                    cmd.Blit(src, dest);
                    //Graphics.SetRenderTarget(rtmip);
                    //rtmip.DiscardContents(true,true);
                    //Graphics.SetRenderTarget(dest);
                    mat.SetPass(0);
                    foreach(Mesh mesh in meshes)
                    {
                        Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
                    }
                    //rtmip.GenerateMips();
                    //Graphics.SetRenderTarget(dest);
                    //Graphics.Blit(rtmip, dest);
                }
                else
                {
                    if(outputTexture.value)
                    {
                        cmd.Blit(mySrc, outputTexture.value, mat);
                        // default blit of screen - no effect
                        cmd.Blit(src, dest);
                    }
                    else
                    {
                        cmd.Blit(mySrc, dest, mat);
                    }
                }
            }
        }
        /*public void OnPostRender() {
            if (mat == null)
            {
                mat = new Material(shader);
                mat.hideFlags = HideFlags.HideAndDontSave;
            }
            mat.SetPass(0);
            Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
        }*/

    public override void Cleanup()
    {
        //CoreUtils.Destroy(m_Material);
    }

}

/*
[CustomEditor(typeof(ColoredPencilsEffect))]
public class ColoredPencilsEffectEditor : Editor
{
    override public void OnInspectorGUI()
    {
        List<string> excludedProperties = new List<string>();
        var myScript = target as ColoredPencilsEffect;
        if(myScript.shaderMethod==10)
        {
            excludedProperties.Add("outlines");
            excludedProperties.Add("fixedHatchDir");
            excludedProperties.Add("MipLevel");
            excludedProperties.Add("vignetting");
            excludedProperties.Add("contentVignetting");
            excludedProperties.Add("paperTint");
            excludedProperties.Add("paperRoughness");
            excludedProperties.Add("paperTex");
        }
        DrawPropertiesExcluding(serializedObject,excludedProperties.ToArray());
        if (GUI.changed) {
            EditorUtility.SetDirty(myScript);
        }
    }
}
*/
}
#endif
