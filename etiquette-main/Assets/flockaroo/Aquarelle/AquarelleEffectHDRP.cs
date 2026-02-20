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

//[AddComponentMenu("Image Effects/Artistic/Aquarelle")]
[Serializable, VolumeComponentMenu("Post-processing/Custom/Flockaroo/Aquarelle")]
public sealed class AquarelleEffectHDRP : CustomPostProcessVolumeComponent, IPostProcessComponent {

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
    private RenderTexture myInputTex = null;
    Regex refRegex = null;
    private int actWidth=0;
    private int actHeight=0;
    private Material gammaShader = null;
    private List<Mesh> screenQuadMesh = null;
    //private int NumTriangles=0;

    //FIXME: automate useMipOnMain - activate when needed
    private bool useMipOnMain = false;
    private bool DoHDRPGamma = true; // we set this to 'false' if we take care of gamma in the effect already

    [Header("Input/Output")]

    [Tooltip("take a texture as input instead of the camera")]
    public TextureParameter inputTexture = new TextureParameter(null);
    [Tooltip("render to a texture instead of the screen")]
    public BoolParameter renderToTexture = new BoolParameter(false);
    [Tooltip("texture being rendered to if above is checked")]
    public RenderTextureParameter outputTexture = new RenderTextureParameter(null);
    [Tooltip("generate mipmap for output texture")]
    public BoolParameter outputMipmap = new BoolParameter(false);

    [Header("Source Adjustments")]
    public ClampedFloatParameter SrcBrightness = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    public ClampedFloatParameter SrcContrast = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
    public ClampedFloatParameter SrcColor = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);


    [Header("Effect")]

    [Tooltip("effect was originally made for gamma colorspace. if you use linear space (default in recent unity) check this box to get better results")]
    public BoolParameter linear2Gamma = new BoolParameter(true);
    public ClampedFloatParameter ContentVignAspect = new ClampedFloatParameter(0.500000f, 0.0f, 1.0f);
    public ClampedFloatParameter ContentVignSharp = new ClampedFloatParameter(0.500000f, 0.0f, 1.0f);
    public ClampedFloatParameter ContentVignSize = new ClampedFloatParameter(0.650000f, 0.0f, 1.0f);
    public ClampedFloatParameter MasterFade = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
    public ClampedFloatParameter PaperRough = new ClampedFloatParameter(0.500000f, 0.0f, 1.0f);
    public ColorParameter PaperTint = new ColorParameter(new Color(1.000000f,1.000000f,1.000000f) );
    public ClampedFloatParameter PredrawStrength = new ClampedFloatParameter(0.850000f, 0.0f, 1.0f);
    public ClampedFloatParameter PredrawAmount = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
    [Tooltip("fast gradient optimization")]
    public BoolParameter fastGrad = new BoolParameter(true);
    public ClampedFloatParameter NumSamples = new ClampedFloatParameter(24.0f, 2.0f, 64.0f);
    public ClampedFloatParameter Spread = new ClampedFloatParameter(0.500000f, 0.0f, 1.0f);
    public ClampedFloatParameter ColorSpread = new ClampedFloatParameter(0.0500000f, 0.0f, 1.0f);
    public ClampedFloatParameter StrokeScale = new ClampedFloatParameter(1.000f, 0.0f, 2.0f);
    public ClampedFloatParameter RimDry = new ClampedFloatParameter(0.200000f, 0.0f, 1.0f);
    public ClampedFloatParameter Vignette = new ClampedFloatParameter(0.600000f, 0.0f, 1.0f);
    [Tooltip("texture as effect mask")]
    public TextureParameter maskTexture = new TextureParameter(null);
    public ClampedFloatParameter UseMask = new ClampedFloatParameter(0.00000f, 0.0f, 1.0f);

    //###PublicVars
    [Header("Other")]
    public BoolParameter flipY = new BoolParameter(false);
    [Tooltip("check this if you use linear color space in HDRP")]
    public BoolParameter HDRPGamma = new BoolParameter(false);
    public BoolParameter geomFlipY = new BoolParameter(false);

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
        rt = new RenderTexture(w, h,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
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
        if(renderToTexture.value)
        {
            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            if(outputMipmap.value)
            {
                rt.antiAliasing=1; // must be for mipmapping to work!!
                rt.useMipMap=true;
                rt.filterMode=FilterMode.Trilinear;
            }
            outputTexture.value=rt;
        }
        else
            outputTexture.value=null;

        actWidth=width;
        actHeight=height;
        bufferOrder.Clear();
        textureCh.Clear();
        buffers.Clear();
        shaders.Clear();
        meshes.Clear();
        if(!textures.ContainsKey("rand256")) textures["rand256"] = createRandTex(256,256);
        if(!textures.ContainsKey("rand64"))  textures["rand64"]  = createRandTex(64,64);

        // check for define suffixes (appended to shader name - makes it possible to have different versions or "modes" of shader)
        List <string> defineModeSuffixes = new List <string>();
        foreach( var m in defineMode ){
            defineModeSuffixes.Add("__"+m.Key+"_"+m.Value);
        }

        bufferOrder.Add("Image");
        buffers["Image"] = createRenderTex();
        textureCh["Image"] = new Dictionary <int,string> ();
        textureDemandsMip["Image"] = new Dictionary <int,bool> ();
        shaders["Image"] = createShaderOptSuff("flockaroo_Aquarelle/ImageHDRP",defineModeSuffixes);
        textureCh["Image"][0] = "Geom_A";
        textureCh["Image"][1] = "rand256";
        textureCh["Image"][2] = "Buff_C";
        textureCh["Image"][3] = "Ref:Buff_A:Tex0";
        textureCh["Image"][4] = "effectMask";
        bufferOrder.Add("Buff_A");
        buffers["Buff_A"] = createRenderTex();
        textureCh["Buff_A"] = new Dictionary <int,string> ();
        textureDemandsMip["Buff_A"] = new Dictionary <int,bool> ();
        shaders["Buff_A"] = createShaderOptSuff("flockaroo_Aquarelle/Buff_AHDRP",defineModeSuffixes);
        textureCh["Buff_A"][0] = "https://www.shaderoo.org/textures/10xfx_trailer.webm";
        textureCh["Buff_A"][1] = "Buff_A";
        buffers["Buff_A"].useMipMap=true;
        buffers["Buff_A"].filterMode=FilterMode.Trilinear;
        //buffers["Buff_A"].width=Tex0;
        //buffers["Buff_A"].height=Tex0;
        bufferOrder.Add("Buff_B");
        buffers["Buff_B"] = createRenderTex();
        textureCh["Buff_B"] = new Dictionary <int,string> ();
        textureDemandsMip["Buff_B"] = new Dictionary <int,bool> ();
        shaders["Buff_B"] = createShaderOptSuff("flockaroo_Aquarelle/Buff_BHDRP",defineModeSuffixes);
        textureCh["Buff_B"][0] = "Buff_A";
        textureCh["Buff_B"][1] = "Ref:Buff_A:Tex0";
        textureCh["Buff_B"][2] = "rand256";
        //buffers["Buff_B"].width=Tex0;
        //buffers["Buff_B"].height=Tex0;
        buffers["Buff_B"].useMipMap=true;
        buffers["Buff_B"].filterMode=FilterMode.Trilinear;
        bufferOrder.Add("Buff_C");
        buffers["Buff_C"] = createRenderTex();
        textureCh["Buff_C"] = new Dictionary <int,string> ();
        textureDemandsMip["Buff_C"] = new Dictionary <int,bool> ();
        shaders["Buff_C"] = createShaderOptSuff("flockaroo_Aquarelle/Buff_CHDRP",defineModeSuffixes);
        textureCh["Buff_C"][0] = "Buff_A";
        textureCh["Buff_C"][1] = "rand256";
        textureCh["Buff_C"][2] = "Buff_B";
        bufferOrder.Add("Geom_A");
        buffers["Geom_A"] = createRenderTex();
        buffers["Geom_A"].depth = 24;
        textureCh["Geom_A"] = new Dictionary <int,string> ();
        textureDemandsMip["Geom_A"] = new Dictionary <int,bool> ();
        shaders["Geom_A"] = createShaderOptSuff("flockaroo_Aquarelle/Geom_AHDRP",defineModeSuffixes);
        meshes["Geom_A"] = createMesh(0x10000);
        textureCh["Geom_A"][0] = "Buff_B";
        textureCh["Geom_A"][1] = "rand256";
        buffers["Geom_A"].useMipMap=true;
        buffers["Geom_A"].filterMode=FilterMode.Trilinear;
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
        if(name=="effectMask")         return maskTexture.value;
        return mainTex;
        // FIXME: alloc textures if not present
        //return textures["rand256"];
    }

    
    public bool IsActive() => MasterFade.value > 0f;

    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > HDRP Default Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        myStart();
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle src_h, RTHandle dest_h) {
        RenderTexture src=src_h.rt;
        RenderTexture dest=dest_h.rt;
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
        if(gammaShader==null) gammaShader = createShader("flockaroo_Aquarelle/GammaCorrectShader");

        mainTex = mySrc;

        if(HDRPGamma.value==false || DoHDRPGamma==false)
        {
            cmd.CopyTexture(src, 0, mySrc, 0);
        }
        else
        {
            cmd.CopyTexture(src, 0, mySrc2, 0);
            gammaShader.SetFloat("gamma",1.0f/2.2f);
            cmd.Blit(mySrc2,mySrc,gammaShader);
        }

        if(inputTexture.value)
        {
            if(myInputTex==null || myInputTex.width!=inputTexture.value.width || myInputTex.height!=inputTexture.value.height)
            {
                myInputTex = new RenderTexture(inputTexture.value.width, inputTexture.value.height, 0, RenderTextureFormat.ARGBFloat);
                reinit=true;
            }
            //cmd.CopyTexture(inputTexture.value, 0, myInputTex, 0);
            if(HDRPGamma.value==false || DoHDRPGamma==false)
                cmd.Blit(inputTexture.value, myInputTex);
            else
            {
                gammaShader.SetFloat("gamma",1.0f/2.2f);
                cmd.Blit(inputTexture.value, myInputTex,gammaShader);
            }
            //cmd.Blit(myInputTex,mySrc,gammaShader);
            mainTex=myInputTex;
        }

        if (renderToTexture.value  && outputTexture.value==null) { reinit=true; }
        if (!renderToTexture.value && outputTexture.value!=null) { reinit=true; }

        // reinit if any defineMode changed

        if(mainTex.width!=actWidth || mainTex.height!=actHeight || reinit)
        {
            Debug.Log("Aquarelle 1st init (or Resolution changed)");
            initAll(mainTex.width,mainTex.height);
        }

        //mainTex=src;
        if(useMipOnMain)
        {
            initMainMipmapRenderTexture(mainTex);
            cmd.Blit(mainTex, mainMip);
            mainTex = mainMip;
        }

        meshNumTri["Geom_A"]=((int)(PredrawAmount.value*(float)(0x10000)))/2*2;

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

            mat.SetFloat("ContentVignAspect",ContentVignAspect.value);
            mat.SetFloat("ContentVignSharp",ContentVignSharp.value);
            mat.SetFloat("ContentVignSize",ContentVignSize.value);
            mat.SetFloat("MasterFade",MasterFade.value);
            mat.SetFloat("PaperRough",PaperRough.value);
            mat.SetColor("PaperTint",PaperTint.value);
            mat.SetFloat("PredrawStrength",PredrawStrength.value);
            mat.SetFloat("Spread",Spread.value);
            mat.SetFloat("PreGrad",fastGrad.value?1.0f:0.0f);
            mat.SetFloat("ColorSpread",ColorSpread.value);
            mat.SetFloat("StrokeScale",StrokeScale.value);
            mat.SetFloat("RimDry",RimDry.value);
            mat.SetFloat("Vignette",Vignette.value);
            mat.SetFloat("UseMask",UseMask.value);
            mat.SetFloat("NumSamples",NumSamples.value);
            mat.SetFloat("SrcBrightness",SrcBrightness.value);
            mat.SetFloat("SrcContrast",SrcContrast.value);
            mat.SetFloat("SrcColor",SrcColor.value);
            mat.SetFloat("linear2Gamma", linear2Gamma.value?1.0f:0.0f);
            //###MatParams

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
                    int actTriNum=meshNumTri[buffName];
                    if(getMeshNumTriangles(meshes[buffName])!=actTriNum)
                    {
                        Debug.Log("resizeing mesh to "+actTriNum);
                        meshes[buffName]=createMesh(actTriNum);
                    }
                    mat.SetFloat("iNumTriangles",meshNumTri[buffName]);
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
                        mat.SetFloat("HDRPGamma",HDRPGamma.value?2.2f:1.0f);
                        if(outputTexture.value)
                        {
                            //cmd.Blit(mainTex, outputTexture, mat);
                            cmd.SetRenderTarget(outputTexture.value);
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
                            cmd.SetRenderTarget(dest);
                            cmd.DrawMesh(screenQuadMesh[0], Matrix4x4.identity, mat, 0, -1, null);
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
    public override void Cleanup(){}
    /*public void OnPostRender() {
    }*/

} // class

} // namespace
#endif
