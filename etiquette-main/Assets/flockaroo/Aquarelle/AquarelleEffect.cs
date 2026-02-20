using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Flockaroo
{
[ExecuteInEditMode]
[RequireComponent(typeof (Camera))]
//[AddComponentMenu("Image Effects/Artistic/Aquarelle")]
public class AquarelleEffect : MonoBehaviour {

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
    private RenderTexture myInputTex = null;
    Regex refRegex = new Regex(@"Ref:([^:]+):Tex([0-9]+)");
    private int actWidth=0;
    private int actHeight=0;
    private List<Mesh> screenQuadMesh = null;

    //FIXME: automate useMipOnMain - activate when needed
    private bool useMipOnMain = false;

    [Header("Input/Output")]

    [Tooltip("take a texture as input instead of the camera")]
    public Texture inputTexture;
    [Tooltip("render to a texture instead of the screen")]
    public bool renderToTexture = false;
    [Tooltip("texture being rendered to if above is checked")]
    public RenderTexture outputTexture;
    [Tooltip("generate mipmap for output texture")]
    public bool outputMipmap = false;

    [Header("Source Adjustments")]
    [Range(0.0f,2.0f)]
    public float SrcBrightness = 1.000000f;
    [Range(0.0f,2.0f)]
    public float SrcContrast = 1.000000f;
    [Range(0.0f,2.0f)]
    public float SrcColor = 1.000000f;
    //[Range(0.0f,2.0f)]
    //public float SrcGamma = 1.000000f;

    [Header("Effect")]

    [Tooltip("effect was originally made for gamma colorspace. if you use linear space (default in recent unity) check this box to get better results")]
    public bool linear2Gamma = true;
    [Range(0.0f, 1.0f)]
    public float ContentVignAspect = 0.500000f;
    [Range(0.0f, 1.0f)]
    public float ContentVignSharp = 0.500000f;
    [Range(0.0f, 1.0f)]
    public float ContentVignSize = 0.650000f;
    [Range(0.0f, 1.0f)]
    public float MasterFade = 1.000000f;
    [Range(0.0f,1.0f)]
    public float PaperRough = 0.500000f;
    public Color PaperTint = new Color(1.000000f,1.000000f,1.000000f);
    [Range(0.0f, 1.0f)]
    public float PredrawStrength = 0.850000f;
    [Range(0.0f, 1.0f)]
    public float PredrawAmount = 1.0f;
    [Tooltip("fast gradient optimization")]
    public bool fastGrad = true;
    [Range(2.0f,64.0f)]
    public float NumSamples = 24.0f;
    [Range(0.0f,1.0f)]
    public float Spread = 0.500000f;
    [Range(0.0f,1.0f)]
    public float ColorSpread = 0.0500000f;
    [Range(0.0f,2.0f)]
    public float StrokeScale = 1.0f;
    [Range(0.0f,1.0f)]
    public float RimDry = 0.200000f;
    [Range(0.0f, 1.0f)]
    public float Vignette = 0.600000f;
    [Tooltip("texture as effect mask")]
    public Texture maskTexture;
    [Range(0.0f, 1.0f)]
    public float UseMask = 0.0f;
    //###PublicVars
    [Header("Other")]
    public bool flipY=false;
    public bool geomFlipY=false;

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
            int vnum = Mathf.Min(num,maxMeshSize);
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
            mainMip = new RenderTexture(src.width, src.height,0,RenderTextureFormat.ARGB32/*,RenderTextureReadWrite.Linear*/);
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
        if(renderToTexture)
        {
            outputTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            if(outputMipmap)
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

        // check for define suffixes (appended to shader name - makes it possible to have different versions or "modes" of shader)
        List <string> defineModeSuffixes = new List <string>();
        foreach( var m in defineMode ){
            defineModeSuffixes.Add("__"+m.Key+"_"+m.Value);
        }

        bufferOrder.Add("Image");
        buffers["Image"] = createRenderTex();
        textureCh["Image"] = new Dictionary <int,string> ();
        textureDemandsMip["Image"] = new Dictionary <int,bool> ();
        shaders["Image"] = createShaderOptSuff("flockaroo_Aquarelle/Image",defineModeSuffixes);
        textureCh["Image"][0] = "Geom_A";
        textureCh["Image"][1] = "rand256";
        textureCh["Image"][2] = "Buff_C";
        textureCh["Image"][3] = "Ref:Buff_A:Tex0";
        textureCh["Image"][4] = "effectMask";
        bufferOrder.Add("Buff_A");
        buffers["Buff_A"] = createRenderTex();
        textureCh["Buff_A"] = new Dictionary <int,string> ();
        textureDemandsMip["Buff_A"] = new Dictionary <int,bool> ();
        shaders["Buff_A"] = createShaderOptSuff("flockaroo_Aquarelle/Buff_A",defineModeSuffixes);
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
        shaders["Buff_B"] = createShaderOptSuff("flockaroo_Aquarelle/Buff_B",defineModeSuffixes);
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
        shaders["Buff_C"] = createShaderOptSuff("flockaroo_Aquarelle/Buff_C",defineModeSuffixes);
        textureCh["Buff_C"][0] = "Buff_A";
        textureCh["Buff_C"][1] = "rand256";
        textureCh["Buff_C"][2] = "Buff_B";
        bufferOrder.Add("Geom_A");
        buffers["Geom_A"] = createRenderTex();
        buffers["Geom_A"].depth = 24;
        textureCh["Geom_A"] = new Dictionary <int,string> ();
        textureDemandsMip["Geom_A"] = new Dictionary <int,bool> ();
        shaders["Geom_A"] = createShaderOptSuff("flockaroo_Aquarelle/Geom_A",defineModeSuffixes);
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

    void Start () {
        //initAll(Screen.width,Screen.height);
    }
    
    // Update is called once per frame
    void Update () {

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
        if(name=="effectMask")         return maskTexture;
        return mainTex;
        // FIXME: alloc textures if not present
        //return textures["rand256"];
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest) {

        bool reinit=false;

        mainTex=src;

        if(src.width!=actWidth || src.height!=actHeight)
        {
            Debug.Log("Aquarelle 1st init (or Resolution changed)");
            initAll(src.width,src.height);
        }

        mainTex=src;

        if(inputTexture)
        {
            if(myInputTex==null || myInputTex.width!=inputTexture.width || myInputTex.height!=inputTexture.height)
            {
                myInputTex = new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGBFloat);
            }
            Graphics.Blit(inputTexture, myInputTex);
            mainTex=myInputTex;
        }

        if (renderToTexture  && outputTexture==null) { reinit=true; }
        if (!renderToTexture && outputTexture!=null) { reinit=true; }

        // reinit if any defineMode chaned

        if(mainTex.width!=actWidth || mainTex.height!=actHeight || reinit)
        {
            Debug.Log("Aquarelle 1st init (or Resolution changed)");
            initAll(mainTex.width,mainTex.height);
        }

        if(useMipOnMain)
        {
            initMainMipmapRenderTexture(mainTex);
            Graphics.Blit(mainTex, mainMip);
            mainTex = mainMip;
        }

        meshNumTri["Geom_A"]=((int)(PredrawAmount*(float)(0x10000)))/2*2;

        foreach( string buffName in bufferOrder )
        {
            Material mat = null;
            if(shaders.ContainsKey(buffName)) mat = shaders[buffName];
            if(mat==null) { continue; }

            mat.SetFloat("geomFlipY", geomFlipY?1.0f:0.0f);
            mat.SetFloat("flipY", flipY?1.0f:0.0f);
            mat.SetInt("_FrameCount", Time.frameCount);
            mat.SetFloat("iResolutionWidth", actWidth);
            mat.SetFloat("iResolutionHeight", actHeight);

            mat.SetFloat("ContentVignAspect",ContentVignAspect);
            mat.SetFloat("ContentVignSharp",ContentVignSharp);
            mat.SetFloat("ContentVignSize",ContentVignSize);
            mat.SetFloat("MasterFade",MasterFade);
            mat.SetFloat("PaperRough",PaperRough);
            mat.SetColor("PaperTint",PaperTint);
            mat.SetFloat("PredrawStrength",PredrawStrength);
            mat.SetFloat("PreGrad",fastGrad?1.0f:0.0f);
            mat.SetFloat("Spread",Spread);
            mat.SetFloat("ColorSpread",ColorSpread);
            mat.SetFloat("StrokeScale",StrokeScale);
            mat.SetFloat("RimDry",RimDry);
            mat.SetFloat("Vignette",Vignette);
            mat.SetFloat("UseMask",UseMask);
            mat.SetFloat("NumSamples",NumSamples);
            mat.SetFloat("SrcBrightness",SrcBrightness);
            mat.SetFloat("SrcContrast",SrcContrast);
            mat.SetFloat("SrcColor",SrcColor);
            mat.SetFloat("linear2Gamma", linear2Gamma?1.0f:0.0f);
            //mat.SetFloat("SrcGamma",SrcGamma);
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
                Graphics.SetRenderTarget(buffers[buffName]);
                GL.Clear(true, true, Color.clear);
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
                    Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
                }
            }
            else
            {
                if(buffName=="Image")
                {
                    if(outputTexture) Graphics.Blit(src, dest); //just copy default output
                    Graphics.SetRenderTarget(outputTexture?outputTexture:dest);
                    if(mat!=null) mat.SetPass(0);
                    Graphics.DrawMeshNow(screenQuadMesh[0], Vector3.zero, Quaternion.identity);
                }
                else
                {
                    //if(mat!=null) Graphics.Blit(src, buffers[buffName], mat);
                    Graphics.SetRenderTarget(buffers[buffName]);
                    if(mat!=null) mat.SetPass(0);
                    Graphics.DrawMeshNow(screenQuadMesh[0], Vector3.zero, Quaternion.identity);
                }
            }
        }

    }
    /*public void OnPostRender() {
    }*/

} // class

} // namespace
