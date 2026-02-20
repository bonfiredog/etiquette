//#define USE_HDRP
#if USE_HDRP
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using Random=UnityEngine.Random;

namespace Flockaroo
{


//[AddComponentMenu("Image Effects/Artistic/OilPainting")]
[Serializable, VolumeComponentMenu("Post-processing/Custom/Flockaroo/OilPainting")]
public sealed class OilPaintingEffectHDRP : CustomPostProcessVolumeComponent, IPostProcessComponent {

    List <string> bufferOrder = new List <string>();
    Dictionary<string, RenderTexture> buffers  = new Dictionary<string, RenderTexture>();
    Dictionary<string, Material>     shaders  = new Dictionary<string, Material>();
    Dictionary<string, Texture>      textures = new Dictionary<string, Texture>();
    Dictionary<string, Dictionary <int,string>> textureCh = new Dictionary<string, Dictionary <int,string>>();
    Dictionary<string, Dictionary <int,bool>> textureDemandsMip = new Dictionary<string, Dictionary <int,bool>>();
    Dictionary<string, List<Mesh>>   meshes   = new Dictionary<string, List<Mesh>>();
    RenderTexture mainTex = null;
    RenderTexture mainMip = null;
    private RenderTexture mySrc = null;
    private RenderTexture mySrc2 = null;
    Regex refRegex = null; //new Regex(@"Ref:([^:]+):Tex([0-9]+)");
    private int actWidth=0;
    private int actHeight=0;
    private int NumTriangles=0;
    private List<Mesh> screenQuadMesh = null;

    //FIXME: automate useMipOnMain - activate when needed
    private bool useMipOnMain = false;

    [Header("Input/Output")]

    [Tooltip("take a texture as input instead of the camera")]
    public Texture inputTexture;
    [Tooltip("render to a texture instead of the screen")]
    public BoolParameter renderToTexture = new BoolParameter(false);
    [Tooltip("texture being rendered to if above is checked")]
    public RenderTexture outputTexture;
    [Tooltip("generate mipmap for output texture")]
    public BoolParameter outputMipmap = new BoolParameter(false);

    [Header("Common")]

    public ClampedFloatParameter EffectFade  = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
    public ClampedFloatParameter PanFade  = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

    [Header("Source")]

    public ClampedFloatParameter SrcBright  = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    public ClampedFloatParameter SrcContrast  = new ClampedFloatParameter(1.4f, 0.0f, 2.0f);
    public ClampedFloatParameter SrcColor  = new ClampedFloatParameter(1.0f, 0.0f, 4.0f);
    public ClampedFloatParameter SrcBlur  = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

    [Header("Effect")]

    [Tooltip("effect was originally made for gamma colorspace. if you use linear space (default in recent unity) check this box to get better results")]
    public BoolParameter linear2Gamma = new BoolParameter(true);
    public ClampedFloatParameter BrushDetail  = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);
    public ClampedFloatParameter BrushFill  = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
    public ClampedIntParameter   NumStrokes  = new ClampedIntParameter(0x8000, 5000, 200000);
    public ClampedFloatParameter LayerScale  = new ClampedFloatParameter(0.8f, 0.5f, 0.95f);
    public ClampedFloatParameter Canvas  = new ClampedFloatParameter(0.4f, 0.0f, 1.0f);
    public ClampedFloatParameter FlickerFreq  = new ClampedFloatParameter(15.0f, 0.0f, 100.0f);
    public ClampedFloatParameter FlickerStrength  = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
    public ClampedFloatParameter LightAng  = new ClampedFloatParameter(135.0f, 0.0f, 360.0f);
    public ClampedFloatParameter LightOffs  = new ClampedFloatParameter(60.0f, 0.0f, 90.0f);
    public ClampedFloatParameter PaintDiff  = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);
    public ClampedFloatParameter PaintSpec  = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);
    public ClampedFloatParameter PaintShiny  = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
    public ClampedFloatParameter ColorSpread  = new ClampedFloatParameter(0.3f, 0.0f, 2.0f);
    public ClampedFloatParameter ScreenFOV  = new ClampedFloatParameter(0.0f, 0.0f, 120.0f);
    public ClampedFloatParameter StrokeAng  = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
    public ClampedFloatParameter StrokeBend  = new ClampedFloatParameter(-1.0f, -1.0f, 1.0f);
    public ClampedFloatParameter StrokeDir  = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
    public ClampedFloatParameter StrokeContour  = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
    public ClampedFloatParameter MultiStroke  = new ClampedFloatParameter(6.0f, 0.0f, 16.0f);
    public ClampedFloatParameter strokeSeed  = new ClampedFloatParameter(10.0f, 0.0f, 100000.0f);
    public ClampedFloatParameter Vignette  = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
    public ClampedFloatParameter CanvasBg  = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
    public ColorParameter CanvasTint  = new ColorParameter(new Color(1.0f,0.97f,0.85f));
    //###PublicVars
    [Header("Other")]
    public BoolParameter flipY = new BoolParameter(false);
    [Tooltip("check this if you use linear color space in HDRP")]
    public BoolParameter HDRPGamma = new BoolParameter(false);
    public BoolParameter geomFlipY = new BoolParameter(false);

    Material createShader(string resname)
    {
        Shader shader = Resources.Load<Shader>(resname);
        Material mat = new Material(shader);
        mat.hideFlags = HideFlags.HideAndDontSave;
        return mat;
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
        if(mip)
        rt.filterMode=FilterMode.Trilinear;
        return rt;
    }

    Texture2D createRandTex(int w, int h)
    {
        //if (RandTex == null)
        //    RandTex = Resources.Load<Texture2D>("rand256");
        Texture2D RandTex;
        {
            RandTex = new Texture2D(w, h, TextureFormat.RGBAFloat, true);
            //RandTex = new Texture2D(w, h, TextureFormat.RGBAHalf, true);
            //RandTex = new Texture2D(w, h, TextureFormat.RGBA32, true);

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
        RandTex.filterMode=FilterMode.Trilinear;
        return RandTex;
    }

    List<Mesh> createMeshOld(int trinum = 0x10000)
    {
        List<Mesh> meshes = new List<Mesh>();
          int maxMeshSize = 0x10000/3*3;
          int mnum = (trinum*3+maxMeshSize-1)/maxMeshSize;
          for(int j=0;j<mnum;j++)
          {
            Mesh mesh = new Mesh();
            meshes.Add(mesh);
            mesh.Clear();
            int vnum = maxMeshSize;
            Vector3[] verts = new Vector3 [vnum];
            int[] tris  = new int [vnum];
            for(int i=0;i<vnum;i++)
            {
                verts[i].x=i+j*maxMeshSize;
                verts[i].y=1;
                verts[i].z=2;
                tris[i]=i;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
          }
          return meshes;
    }

    List<Mesh> createMesh(int trinum = 0x10000)
    {
        int maxMeshSize=0x10000/3*3;
        List<Mesh> meshes = new List<Mesh>();
          int num=trinum*3;
          for(int j=0;num>0;j++)
          {
            Mesh mesh = new Mesh();
            mesh.Clear();
            int vnum = Math.Min(num,maxMeshSize);
            Vector3[] verts = new Vector3 [vnum];
            int[] tris  = new int [vnum];
            for(int i=0;i<vnum;i++)
            {
                verts[i].x=i+j*maxMeshSize;
                verts[i].y=1;
                verts[i].z=2;
                tris[i]=i;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            num-=vnum;
            meshes.Add(mesh);
          }
          return meshes;
    }

    void initMainMipmapRenderTexture(RenderTexture src)
    {
        if(mainMip == null)
        {
            mainMip = new RenderTexture(src.width, src.height,0,RenderTextureFormat.ARGB32);
            mainMip.antiAliasing=1; // must be for mipmapping to work!!
            mainMip.useMipMap=true;
            mainMip.filterMode=FilterMode.Trilinear;
#if UNITY_5_5_OR_NEWER
            //rtmip.autoGenerateMips=false;
#endif
        }

    }

    void initAll(int width, int height)
    {
        if(renderToTexture.value)
        {
            outputTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            if(outputMipmap.value)
            {
                outputTexture.antiAliasing=1; // must be for mipmapping to work!!
                outputTexture.useMipMap=true;
                outputTexture.filterMode=FilterMode.Trilinear;
            }
        }
        else
            outputTexture = null;

        actWidth=width;
        actHeight=height;
        bufferOrder.Clear();
        textureCh.Clear();
        buffers.Clear();
        shaders.Clear();
        meshes.Clear();
        if(!textures.ContainsKey("rand256")) textures["rand256"] = createRandTex(256,256);
        if(!textures.ContainsKey("rand64"))  textures["rand64"]  = createRandTex(64,64);

        bufferOrder.Add("Buff_A");
        buffers["Buff_A"] = createRenderTex();
        textureCh["Buff_A"] = new Dictionary <int,string> ();
        textureDemandsMip["Buff_A"] = new Dictionary <int,bool> ();
        shaders["Buff_A"] = createShader("flockaroo_OilPainting/Buff_AHDRP");
        //textureCh["Buff_A"][0] = "Buff_A";
        textureCh["Buff_A"][0] = "none";
        textureCh["Buff_A"][1] = "rand256";
        textureCh["Buff_A"][2] = "Buff_A";
        buffers["Buff_A"].width=512;
        buffers["Buff_A"].height=512;
        buffers["Buff_A"].useMipMap=true;
        buffers["Buff_A"].filterMode=FilterMode.Trilinear;
        bufferOrder.Add("Geom_A");
        buffers["Geom_A"] = createRenderTex();
        buffers["Geom_A"].depth = 24;
        textureCh["Geom_A"] = new Dictionary <int,string> ();
        textureDemandsMip["Geom_A"] = new Dictionary <int,bool> ();
        shaders["Geom_A"] = createShader("flockaroo_OilPainting/Geom_AHDRP");
        meshes["Geom_A"] = createMesh(NumStrokes.value*2);
        NumTriangles=NumStrokes.value*2;
        textureCh["Geom_A"][0] = "https://ak1.picdn.net/shutterstock/videos/15325111/preview/stock-footage-beautiful-young-woman-enjoying-her-vacation-on-santorini-happy-tourist-is-wearing-sunhat.webm";
        textureCh["Geom_A"][1] = "rand256";
        textureCh["Geom_A"][2] = "Buff_A";
        textureDemandsMip["Geom_A"][0]=true;
        bufferOrder.Add("Image");
        buffers["Image"] = createRenderTex();
        textureCh["Image"] = new Dictionary <int,string> ();
        textureDemandsMip["Image"] = new Dictionary <int,bool> ();
        shaders["Image"] = createShader("flockaroo_OilPainting/ImageHDRP");
        textureCh["Image"][0] = "Geom_A";
        textureCh["Image"][1] = "rand256";
        textureCh["Image"][2] = "Buff_A";
        textureCh["Image"][3] = "https://ak1.picdn.net/shutterstock/videos/15325111/preview/stock-footage-beautiful-young-woman-enjoying-her-vacation-on-santorini-happy-tourist-is-wearing-sunhat.webm";

        //###InitMarker
        screenQuadMesh=createMesh(2);
    }

    void myStart () {
        //initAll(Screen.width,Screen.height);
    }
    
    // Update is called once per frame
    void myUpdate () {
    	
    }

    Texture getTexture(string name)
    {
        if(name.StartsWith("Ref:")) {
            if (refRegex==null) refRegex = new Regex(@"Ref:([^:]+):Tex([0-9]+)"/*,RegexOptions.Compiled*/);
            Match match = refRegex.Match(name);
            if (match.Success)
            {
                string buff = match.Groups[1].Value;
                int chan = int.Parse(match.Groups[2].Value);
                return getTexture(textureCh[buff][chan]);
            }
            return null;
        }
        if(buffers.ContainsKey(name))  return buffers[name];
        if(textures.ContainsKey(name)) return textures[name];
        if(name.EndsWith(".mp4"))      return mainTex;
        if(name.EndsWith(".webm"))     return mainTex;
        return mainTex;
        // FIXME: alloc textures if not present
        //return textures["rand256"];
    }

    
public bool IsActive() => EffectFade.value < 1f;

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

        mainTex=src;
        bool reinit=false;

        if(mySrc==null || mySrc.width!=src.width || mySrc.height!=src.height)
        {
            mySrc = new RenderTexture(src.width, src.height, 0, src.graphicsFormat);
            mySrc.filterMode=FilterMode.Bilinear;
            //mySrc2 = new RenderTexture(src.width, src.height, 0, src.graphicsFormat);
            //mySrc2.filterMode=FilterMode.Bilinear;
        }
        mainTex = mySrc;
        cmd.CopyTexture(src, 0, mySrc, 0);
        //cmd.Blit(mySrc2, mySrc);
        if(inputTexture)
        {
            mainTex = new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGBFloat);
            cmd.Blit(inputTexture, mainTex);
        }

        if (renderToTexture.value  && outputTexture==null) { reinit=true; }
        if (!renderToTexture.value && outputTexture!=null) { reinit=true; }

        if( mainTex.width!=actWidth || mainTex.height!=actHeight || reinit )
        {
            Debug.Log("OilPainting 1st init (or Resolution changed)");
            initAll(mainTex.width,mainTex.height);
        }

        if(NumTriangles!=NumStrokes.value*2)
        {
            meshes["Geom_A"] = createMesh(NumStrokes.value*2);
            NumTriangles=NumStrokes.value*2;
        }

        if(useMipOnMain)
        {
            initMainMipmapRenderTexture(mainTex);
            cmd.Blit(mainTex, mainMip);
            mainTex = mainMip;
        }

        foreach( string buffName in bufferOrder )
        {
            Material mat = null;
            if(shaders.ContainsKey(buffName)) mat = shaders[buffName];
            if(mat==null) { continue; }

            mat.SetFloat("geomFlipY", geomFlipY.value?1.0f:0.0f);
            mat.SetFloat("flipY", flipY.value?1.0f:0.0f);
            mat.SetInt("_FrameCount", Time.frameCount);
            mat.SetFloat("iResolutionWidth", actWidth);
            mat.SetFloat("iResolutionHeight", actHeight);

            mat.SetFloat("BrushDetail",(BrushDetail.value*0.8f+0.2f)*(BrushDetail.value*0.8f+0.2f));
            mat.SetFloat("BrushSize",BrushFill.value*2.0f+0.25f);
            mat.SetFloat("Canvas",Canvas.value);
            mat.SetFloat("EffectFade",EffectFade.value);
            mat.SetFloat("FlickerFreq",FlickerFreq.value);
            mat.SetFloat("FlickerStrength",FlickerStrength.value);
            mat.SetFloat("halfFOV",ScreenFOV.value/180.0f*3.14159265359f*0.5f);
            mat.SetFloat("LayerScale",LayerScale.value);
            mat.SetFloat("LightAng",LightAng.value/180.0f*3.14159265359f);
            mat.SetFloat("LightOffs",LightOffs.value/180.0f*3.14159265359f);
            mat.SetFloat("NumTriangles",NumStrokes.value*2);
            mat.SetFloat("PaintDiff",PaintDiff.value);
            mat.SetFloat("PaintSpec",PaintSpec.value);
            mat.SetFloat("PaintShiny",PaintShiny.value);
            mat.SetFloat("PanFade",PanFade.value);
            mat.SetFloat("SrcBlur",SrcBlur.value);
            mat.SetFloat("SrcBright",SrcBright.value);
            mat.SetFloat("SrcContrast",SrcContrast.value);
            mat.SetFloat("SrcColor",SrcColor.value);
            mat.SetFloat("ColorSpread",ColorSpread.value);
            mat.SetFloat("StrokeAng",StrokeAng.value/180.0f*3.14159265359f);
            mat.SetFloat("StrokeBend",StrokeBend.value);
            mat.SetFloat("StrokeContour",StrokeContour.value);
            mat.SetFloat("StrokeDir",StrokeDir.value);
            mat.SetVector("strokeNumXY",new Vector4(MultiStroke.value,MultiStroke.value,0.0f,0.0f));
            mat.SetFloat("strokeSeed",strokeSeed.value);
            mat.SetFloat("Vignette",Vignette.value);
            mat.SetColor("CanvasTint", CanvasTint.value);
            mat.SetFloat("CanvasBg", CanvasBg.value);
            mat.SetFloat("HDRPGamma", HDRPGamma.value?2.2f:1.0f);
            mat.SetFloat("linear2Gamma", linear2Gamma.value?1.0f:0.0f);
            //###MatParams
            mat.SetFloat("iBufferWidth", buffers[buffName].width);
            mat.SetFloat("iBufferHeight", buffers[buffName].height);

            for(int i=0;i<8;i++)
            {
                if(textureCh.ContainsKey(buffName) &&
                   textureCh[buffName].ContainsKey(i))
                {
                    Texture tex = getTexture(textureCh[buffName][i]);
                    if(mat!=null) mat.SetTexture("iChannel"+i, tex);
                    if(textureDemandsMip.ContainsKey(buffName) &&
                       textureDemandsMip[buffName].ContainsKey(i) &&
                       textureDemandsMip[buffName][i])
                    {
                        if(tex==mainTex) useMipOnMain=true;
                        else if(tex is RenderTexture) ((RenderTexture)tex).useMipMap=true;
                    }
                }
           }

            if(meshes.ContainsKey(buffName))
            {
                cmd.SetRenderTarget(buffers[buffName]);
                GL.Clear(true, true, Color.clear);
                if(mat!=null) mat.SetPass(0);
                foreach(Mesh mesh in meshes[buffName])
                {
                    //cmd.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
                    cmd.DrawMesh(mesh, Matrix4x4.identity, mat, 0, -1, null);
                }
            }
            else
            {
                if(buffName=="Image")
                {
                    if(mat!=null)
                        if(outputTexture)
                        {
                            cmd.SetRenderTarget(outputTexture/*.value*/);
                            cmd.DrawMesh(screenQuadMesh[0], Matrix4x4.identity, mat, 0, -1, null);
                            //cmd.Blit(mainTex, outputTexture, mat);
                            // default blit of screen - no effect
                            cmd.Blit(mainTex, dest);
                        }
                        else
                        {
                            //cmd.Blit(mainTex, dest, mat);
                            cmd.SetRenderTarget(dest);
                            cmd.DrawMesh(screenQuadMesh[0], Matrix4x4.identity, mat, 0, -1, null);
                        }
                }
                else
                {
                    if(mat!=null)
                    {
                        cmd.SetRenderTarget(buffers[buffName]);
                        cmd.DrawMesh(screenQuadMesh[0], Matrix4x4.identity, mat, 0, -1, null);
                        //cmd.Blit(mainTex, buffers[buffName], mat);
                    }
                }
            }
        }

    }
    public override void Cleanup(){}
/*public void OnPostRender() {
    }*/

}
}
#endif
