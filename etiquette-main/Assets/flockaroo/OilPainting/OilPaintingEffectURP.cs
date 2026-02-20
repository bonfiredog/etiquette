//#define USE_URP
// you might want to comment the line below for older versions of unity
#define URP_VERSION_GE_13
#if USE_URP
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using Random=UnityEngine.Random;

namespace Flockaroo
{

public class OilPaintingEffectURP : ScriptableRendererFeature {

    [System.Serializable]
    public class EffectSettings
    {
        public RenderPassEvent WhenToApply = RenderPassEvent.BeforeRenderingPostProcessing;

        [Header("Input/Output")]

        [Tooltip("take a texture as input instead of the camera")]
        public Texture inputTexture;
        [Tooltip("render to a texture instead of the screen")]
        public bool renderToTexture = false;
        [Tooltip("texture being rendered to if above is checked")]
        public RenderTexture outputTexture;
        [Tooltip("generate mipmap for output texture")]
        public bool outputMipmap = false;

        [Header("Common")]

        [Range(0.0f, 1.0f)]
        public float EffectFade = 0.0f;
        [Range(0.0f, 1.0f)]
        public float PanFade = 0.000000f;

        [Header("Source")]

        [Range(0.0f, 1.0f)]
        public float SrcBlur = 0.000000f;
        [Range(0.0f, 1.0f)]
        public float SrcBright = 1.000000f;
        [Range(0.0f, 1.0f)]
        public float SrcColor = 1.500000f;
        [Range(0.0f, 1.0f)]
        public float SrcContrast = 1.500000f;

        [Header("Effect")]

        [Tooltip("effect was originally made for gamma colorspace. if you use linear space (default in recent unity) check this box to get better results")]
        public bool linear2Gamma = true;
        [Range(0.0f, 1.0f)]
        public float BrushDetail = 0.100000f;
        [Range(0.0f, 1.0f)]
        public float BrushFill = 0.5f;
        [Range(5000,200000)]
        public int   NumStrokes=0x8000;
        [Range(0.0f, 1.0f)]
        public float Canvas = 0.400000f;
        [Range(0.0f, 1.0f)]
        public float ColorSpread = 0.500000f;
        [Range(0.0f, 100.0f)]
        public float FlickerFreq = 15.0f;
        [Range(0.0f, 1.0f)]
        public float FlickerStrength = 0.000000f;
        [Range(0.5f, 0.95f)]
        public float LayerScale = 0.800000f;
        [Range(0.0f, 360.0f)]
        public float LightAng = 135.0f;
        [Range(0.0f, 90.0f)]
        public float LightOffs = 60.000000f;
        [Range(0.0f, 1.0f)]
        public float PaintDiff = 0.150000f;
        [Range(0.0f, 2.0f)]
        public float PaintShiny = 0.500000f;
        [Range(0.0f, 1.0f)]
        public float PaintSpec = 0.150000f;
        [Range(0.0f, 360.0f)]
        public float StrokeAng = 0.000000f;
        [Range(0.0f, 1.0f)]
        public float StrokeBend = -1.000000f;
        [Range(0.0f, 1.0f)]
        public float StrokeContour = 1.000000f;
        [Range(0.0f, 1.0f)]
        public float StrokeDir = 0.000000f;
        [Range(0.0f, 120.0f)]
        public float ScreenFOV = 0.000000f;
        [Range(0.0f, 10.0f)]
        public float MultiStroke = 6.000000f;
        [Range(0.0f,100000.0f)]
        public float strokeSeed = 10.0f;
        [Range(0.0f, 1.0f)]
        public float Vignette = 1.000000f;
        [Range(0.0f,1.0f)]
        public float CanvasBg = 0.5f;
        public Color CanvasTint = new Color(1.000000f,1.000000f,1.000000f);
        //###PublicVars
        [Header("Other")]
        public bool flipY=false;
        public bool geomFlipY=false;

        [Tooltip("check this if you use linear color space in HDRP")]
        public bool HDRPGamma = false;

    }//class EffectSettings

    public EffectSettings settings = new EffectSettings();

    class EffectRenderPass: ScriptableRenderPass
    {
        string profilerTag;
        EffectSettings s;
        public RenderTargetIdentifier cameraColorTargetIdent;
#if URP_VERSION_GE_13
        RTHandle tempTexture;
        RTHandle tempTexture2;
#else
        RenderTargetHandle tempTexture;
        RenderTargetHandle tempTexture2;
#endif
        RenderTexture tmpRT1;
        RenderTexture tmpRT2;
    List <string> bufferOrder = new List <string>();
    Dictionary<string, RenderTexture> buffers  = new Dictionary<string, RenderTexture>();
    Dictionary<string, Material>     shaders  = new Dictionary<string, Material>();
    Dictionary<string, Texture>      textures = new Dictionary<string, Texture>();
    Dictionary<string, Dictionary <int,string>> textureCh = new Dictionary<string, Dictionary <int,string>>();
    Dictionary<string, Dictionary <int,bool>> textureDemandsMip = new Dictionary<string, Dictionary <int,bool>>();
    Dictionary<string, List<Mesh>>   meshes   = new Dictionary<string, List<Mesh>>();
    Dictionary<string, int>   meshNumTri   = new Dictionary<string, int>();
    Dictionary<string, int>   defineMode      = new Dictionary<string, int>();
    RenderTexture mainTex = null;
    RenderTexture mainMip = null;
    private RenderTexture mySrc = null;
    private RenderTexture mySrc2 = null;
    private RenderTexture myDst = null;
    private RenderTexture myInputTex = null;
    Regex refRegex = new Regex(@"Ref:([^:]+):Tex([0-9]+)");
    private int actWidth=0;
    private int actHeight=0;
    private Material gammaShader = null;
    private List<Mesh> screenQuadMesh = null;
    //private int NumTriangles=0;

    //FIXME: automate useMipOnMain - activate when needed
    private bool useMipOnMain = false;
    private bool DoHDRPGamma = false; // we set this to 'false' if we take care of gamma in the effect already

    public EffectRenderPass(string tag, ref EffectSettings settings, RenderPassEvent renderPassEvent)
    {
        this.profilerTag=tag;
        this.s=settings;
        this.renderPassEvent=renderPassEvent;
    }

    Material createShader(string resname)
    {
        Shader shader = Resources.Load<Shader>(resname);
        if(shader==null) return null;
        Material mat = new Material(shader);
        mat.hideFlags = HideFlags.HideAndDontSave;
        return mat;
    }

    Material createShaderOptSuff(string resname, List <string> suff)
    {
        Material mat = null;
        if(suff.Count>0)
        {
            if (mat==null) mat = createShaderOptSuff(resname+suff[0],suff.GetRange(1,suff.Count-1));
            if (mat==null) mat = createShaderOptSuff(resname,suff.GetRange(1,suff.Count-1));
        }
        if (mat==null) mat = createShader(resname);
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

    int getMeshNumTriangles(List <Mesh> list)
    {
        int tcnt=0;
        for (int i = 0; i < list.Count; i++)
        {
            tcnt+=list[i].triangles.Length/3;
        }
        return tcnt;
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
        if(s.renderToTexture)
        {
            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            if(s.outputMipmap)
            {
                rt.antiAliasing=1; // must be for mipmapping to work!!
                rt.useMipMap=true;
                rt.filterMode=FilterMode.Trilinear;
            }
            s.outputTexture=rt;
        }
        else
            s.outputTexture=null;

        actWidth=width;
        actHeight=height;
        bufferOrder.Clear();
        textureCh.Clear();
        buffers.Clear();
        shaders.Clear();
        meshes.Clear();
        if((!textures.ContainsKey("rand256")) || textures["rand256"]==null) textures["rand256"] = createRandTex(256,256);
        if((!textures.ContainsKey("rand64"))  || textures["rand64"]==null)  textures["rand64"]  = createRandTex(64,64);

        // check for define suffixes (appended to shader name - makes it possible to have different versions or "modes" of shader)
        List <string> defineModeSuffixes = new List <string>();
        foreach( var m in defineMode ){
            defineModeSuffixes.Add("__"+m.Key+"_"+m.Value);
        }

        bufferOrder.Add("Image");
        buffers["Image"] = createRenderTex();
        textureCh["Image"] = new Dictionary <int,string> ();
        textureDemandsMip["Image"] = new Dictionary <int,bool> ();
        shaders["Image"] = createShaderOptSuff("flockaroo_OilPainting/ImageHDRP",defineModeSuffixes);
        textureCh["Image"][0] = "Geom_A";
        textureCh["Image"][1] = "rand256";
        textureCh["Image"][2] = "Buff_A";
        textureCh["Image"][3] = "https://www.shaderoo.org/textures/10xfx_trailer.webm";
        bufferOrder.Add("Geom_A");
        buffers["Geom_A"] = createRenderTex();
        buffers["Geom_A"].depth = 24;
        textureCh["Geom_A"] = new Dictionary <int,string> ();
        textureDemandsMip["Geom_A"] = new Dictionary <int,bool> ();
        shaders["Geom_A"] = createShaderOptSuff("flockaroo_OilPainting/Geom_AHDRP",defineModeSuffixes);
        meshes["Geom_A"] = createMesh(0x10000);
        meshNumTri["Geom_A"] = 0x10000;
        textureCh["Geom_A"][0] = "https://www.shaderoo.org/textures/10xfx_trailer.webm";
        textureCh["Geom_A"][1] = "rand256";
        textureCh["Geom_A"][2] = "Buff_A";
        textureDemandsMip["Geom_A"][0]=true;
        bufferOrder.Add("Buff_A");
        buffers["Buff_A"] = createRenderTex();
        textureCh["Buff_A"] = new Dictionary <int,string> ();
        textureDemandsMip["Buff_A"] = new Dictionary <int,bool> ();
        shaders["Buff_A"] = createShaderOptSuff("flockaroo_OilPainting/Buff_AHDRP",defineModeSuffixes);
        textureCh["Buff_A"][0] = "none";
        textureCh["Buff_A"][1] = "rand256";
        textureCh["Buff_A"][2] = "Buff_A";
        buffers["Buff_A"].width=512;
        buffers["Buff_A"].height=512;
        buffers["Buff_A"].useMipMap=true;
        buffers["Buff_A"].filterMode=FilterMode.Trilinear;
        //###InitMarker
        // make sure image is rendered last
        int idxImage=bufferOrder.IndexOf("Image");
        if(idxImage>=0)
        {
            bufferOrder.RemoveAt(idxImage);
            bufferOrder.Add("Image");
        }
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

    //public bool IsActive() => MasterFade.value > 0f;

    public void Render(CommandBuffer cmd, RenderTexture src, RenderTexture dest) {
        src.filterMode=FilterMode.Trilinear;

        //mainTex=src;
        bool reinit=false;

        if(mySrc==null || mySrc.width!=src.width || mySrc.height!=src.height)
        {
            mySrc = new RenderTexture(src.width, src.height, 0, src.graphicsFormat);
            mySrc.filterMode=FilterMode.Bilinear;
            mySrc2 = new RenderTexture(src.width, src.height, 0, src.graphicsFormat);
            mySrc2.filterMode=FilterMode.Bilinear;
        }
        if(myDst==null || myDst.width!=dest.width || myDst.height!=dest.height)
        {
            myDst = new RenderTexture(dest.width, dest.height, 0, src.graphicsFormat);
        }
        if(gammaShader==null) gammaShader = createShader("flockaroo_OilPainting/GammaCorrectShader");

        mainTex = mySrc;

        if(s.HDRPGamma==false || DoHDRPGamma==false)
        {
            cmd.CopyTexture(src, 0, mySrc, 0);
        }
        else
        {
            cmd.CopyTexture(src, 0, mySrc2, 0);
            gammaShader.SetFloat("gamma",1.0f/2.2f);
            cmd.Blit(mySrc2,mySrc,gammaShader);
        }

        if(s.inputTexture)
        {
            if(myInputTex==null || myInputTex.width!=s.inputTexture.width || myInputTex.height!=s.inputTexture.height)
            {
                myInputTex = new RenderTexture(s.inputTexture.width, s.inputTexture.height, 0, RenderTextureFormat.ARGBFloat);
                reinit=true;
            }
            //cmd.CopyTexture(inputTexture.value, 0, myInputTex, 0);
            if(s.HDRPGamma==false || DoHDRPGamma==false)
                cmd.Blit(s.inputTexture, myInputTex);
            else
            {
                gammaShader.SetFloat("gamma",1.0f/2.2f);
                cmd.Blit(s.inputTexture, myInputTex,gammaShader);
            }
            //cmd.Blit(myInputTex,mySrc,gammaShader);
            mainTex=myInputTex;
        }

        if (s.renderToTexture  && s.outputTexture==null) { reinit=true; }
        if (!s.renderToTexture && s.outputTexture!=null) { reinit=true; }

        // reinit if any defineMode changed

        // unity kills Rendertargets, etc when paused... so we will reinit then
        if (buffers.ContainsKey("Image") && buffers["Image"]==null) reinit=true;

        if(mainTex.width!=actWidth || mainTex.height!=actHeight || reinit)
        {
            Debug.Log("OilPainting 1st init (or Resolution changed)");
            initAll(mainTex.width,mainTex.height);
        }

        //mainTex=src;
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

            mat.SetFloat("geomFlipY", s.geomFlipY?1.0f:0.0f);
            mat.SetFloat("flipY", s.flipY?1.0f:0.0f);
            mat.SetInt("_FrameCount", Time.frameCount);
            mat.SetFloat("iBufferResolutionWidth", buffers[buffName].width);
            mat.SetFloat("iBufferResolutionHeight", buffers[buffName].height);
            mat.SetFloat("iResolutionWidth", actWidth);
            mat.SetFloat("iResolutionHeight", actHeight);

            mat.SetFloat("BrushDetail",(s.BrushDetail*0.8f+0.2f)*(s.BrushDetail*0.8f+0.2f));
            mat.SetFloat("BrushSize",s.BrushFill*2.0f+0.25f);
            mat.SetFloat("Canvas",s.Canvas);
            mat.SetFloat("CanvasBg",s.CanvasBg);
            mat.SetColor("CanvasTint",s.CanvasTint);
            mat.SetFloat("ColorSpread",s.ColorSpread);
            mat.SetFloat("FlickerFreq",s.FlickerFreq);
            mat.SetFloat("FlickerStrength",s.FlickerStrength);
            mat.SetFloat("LayerScale",s.LayerScale);
            mat.SetFloat("LightAng",s.LightAng/180.0f*3.14159265359f);
            mat.SetFloat("LightOffs",s.LightOffs/180.0f*3.14159265359f);
            mat.SetFloat("EffectFade",s.EffectFade);
            mat.SetFloat("PaintDiff",s.PaintDiff);
            mat.SetFloat("PaintShiny",s.PaintShiny);
            mat.SetFloat("PaintSpec",s.PaintSpec);
            mat.SetFloat("PanFade",s.PanFade);
            mat.SetFloat("SrcBlur",s.SrcBlur);
            mat.SetFloat("SrcBright",s.SrcBright);
            mat.SetFloat("SrcColor",s.SrcColor);
            mat.SetFloat("SrcContrast",s.SrcContrast);
            mat.SetFloat("StrokeAng",s.StrokeAng/180.0f*3.14159265359f);
            mat.SetFloat("StrokeBend",s.StrokeBend);
            mat.SetFloat("StrokeContour",s.StrokeContour);
            mat.SetFloat("StrokeDir",s.StrokeDir);
            mat.SetFloat("Vignette",s.Vignette);
            mat.SetFloat("halfFOV",s.ScreenFOV/180.0f*3.14159265359f*0.5f);
            mat.SetVector("strokeNumXY",new Vector4(s.MultiStroke,s.MultiStroke,0.0f,0.0f));
            //###MatParams
            mat.SetFloat("iBufferWidth", buffers[buffName].width);
            mat.SetFloat("iBufferHeight", buffers[buffName].height);
            mat.SetFloat("HDRPGamma",s.HDRPGamma?2.2f:1.0f);
            mat.SetFloat("linear2Gamma", s.linear2Gamma?1.0f:0.0f);

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
                //GL.Clear(true, true, Color.clear);
                cmd.ClearRenderTarget(true, true, Color.clear);
                if(mat!=null) mat.SetPass(0);
                if(meshNumTri.ContainsKey(buffName))
                {
                    meshNumTri[buffName]=(int)(Math.Floor((float)s.NumStrokes)*2.0f);
                    int actTriNum=meshNumTri[buffName];
                    if(getMeshNumTriangles(meshes[buffName])!=actTriNum)
                    {
                        Debug.Log("resizeing mesh to "+actTriNum);
                        meshes[buffName]=createMesh(actTriNum);
                    }
                    mat.SetFloat("iNumTriangles",meshNumTri[buffName]);
                    mat.SetFloat("NumTriangles",meshNumTri[buffName]);
                }
                foreach(Mesh mesh in meshes[buffName])
                {
                    //cmd.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
                    cmd.DrawMesh(mesh, Matrix4x4.identity, mat, 0, -1, null);
                }
            }
            else
            {
                if(mat!=null)
                {
                    if(buffName=="Image")
                    {
                        mat.SetFloat("HDRPGamma",s.HDRPGamma?2.2f:1.0f);
                        if(s.outputTexture)
                        {
                            //cmd.Blit(mainTex, outputTexture, mat);
                            cmd.SetRenderTarget(s.outputTexture);
                            cmd.DrawMesh(screenQuadMesh[0], Matrix4x4.identity, mat, 0, -1, null);
                            // default blit of screen - no effect
                            cmd.Blit(mainTex, dest);
                        }
                        else
                        {
                            //cmd.Blit(mainTex, dest, mat);
                            //cmd.Blit(mainTex, mySrc2, mat);
                            //gammaShader.SetFloat("gamma",2.2f);
                            //cmd.Blit(mySrc2, dest, gammaShader);
                            //cmd.SetRenderTarget(dest);
                            cmd.SetRenderTarget(myDst);
                            cmd.DrawMesh(screenQuadMesh[0], Matrix4x4.identity, mat, 0, -1, null);
                            cmd.CopyTexture(myDst, 0, dest, 0);
                        }
                    }
                    else
                    {
                        //if(mat!=null) cmd.Blit(mainTex, buffers[buffName], mat);
                        cmd.SetRenderTarget(buffers[buffName]);
                        cmd.DrawMesh(screenQuadMesh[0], Matrix4x4.identity, mat, 0, -1, null);
                    }
                }
            }
        }

    }

    //------------------ RenderPass override funtions
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // create a temporary render texture that matches the camera
        //cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
        //cmd.GetTemporaryRT(tempTexture2.id, cameraTextureDescriptor);
        if(tmpRT1==null) tmpRT1=RenderTexture.GetTemporary(cameraTextureDescriptor);
        if(tmpRT2==null) tmpRT2=RenderTexture.GetTemporary(cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
        //cmd.Blit(cameraColorTargetIdent,tempTexture2.Identifier());
        //Render(cmd,tempTexture2,tempTexture);
        //cmd.Blit(tempTexture.Identifier(), cameraColorTargetIdent);
        cmd.Blit(cameraColorTargetIdent,tmpRT1);
        Render(cmd,tmpRT1,tmpRT2);
        cmd.Blit(tmpRT2, cameraColorTargetIdent);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        //RenderTexture.ReleaseTemporary(tmpRT1);
        //RenderTexture.ReleaseTemporary(tmpRT2);
    }
    } //class EffectRenderPass

    EffectRenderPass m_ScriptablePass = null;

    public override void Create()
    {
        if(m_ScriptablePass == null)
            m_ScriptablePass = new EffectRenderPass("FlockarooOilPainting",ref settings,settings.WhenToApply);
        //m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //if (!settings.IsEnabled)
        //{
        //    // we can do nothing this frame if we want
        //    return;
        //}
    
        // Gather up and pass any extra information our pass will need.
        // In this case we're getting the camera's color buffer target
#if URP_VERSION_GE_13
#else
        var cameraColorTargetIdent = renderer.cameraColorTarget;
        m_ScriptablePass.cameraColorTargetIdent=cameraColorTargetIdent;
#endif

        renderer.EnqueuePass(m_ScriptablePass);
    }
#if URP_VERSION_GE_13
    public override void SetupRenderPasses(ScriptableRenderer renderer,
                                          in RenderingData renderingData)
    {
        // The target is used after allocation
        //m_ScriptablePass.Setup(renderer.cameraColorTargetHandle);
        m_ScriptablePass.cameraColorTargetIdent=renderer.cameraColorTargetHandle;
    }
#endif
} // class

} // namespace
#endif
