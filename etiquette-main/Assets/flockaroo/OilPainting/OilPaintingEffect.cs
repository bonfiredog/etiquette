using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Flockaroo
{
[ExecuteInEditMode]
[RequireComponent(typeof (Camera))]
//[AddComponentMenu("Image Effects/Artistic/OilPainting")]
public class OilPaintingEffect : MonoBehaviour {

    List <string> bufferOrder = new List <string>();
    Dictionary<string, RenderTexture> buffers  = new Dictionary<string, RenderTexture>();
    Dictionary<string, Material>     shaders  = new Dictionary<string, Material>();
    Dictionary<string, Texture>      textures = new Dictionary<string, Texture>();
    Dictionary<string, Dictionary <int,string>> textureCh = new Dictionary<string, Dictionary <int,string>>();
    Dictionary<string, Dictionary <int,bool>> textureDemandsMip = new Dictionary<string, Dictionary <int,bool>>();
    Dictionary<string, List<Mesh>>   meshes   = new Dictionary<string, List<Mesh>>();
    RenderTexture mainTex = null;
    RenderTexture mainMip = null;
    Regex refRegex = new Regex(@"Ref:([^:]+):Tex([0-9]+)");
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
    public bool renderToTexture;
    [Tooltip("texture being rendered to if above is checked")]
    public RenderTexture outputTexture;
    [Tooltip("generate mipmap for output texture")]
    public bool outputMipmap;

    [Header("Common")]

    [Range(0.0f,1.0f)]
    public float EffectFade = 0.0f;
    [Range(0.0f,1.0f)]
    public float PanFade = 0.0f;

    [Header("Source")]

    [Range(0.0f,2.0f)]
    public float SrcBright = 1.0f;
    [Range(0.0f,2.0f)]
    public float SrcContrast = 1.4f;
    [Range(0.0f,4.0f)]
    public float SrcColor = 1.0f;
    [Range(0.0f,1.0f)]
    public float SrcBlur = 0.0f;

    [Header("Effect")]

    [Tooltip("effect was originally made for gamma colorspace. if you use linear space (default in recent unity) check this box to get better results")]
    public bool linear2Gamma = true;
    [Range(0.0f,1.0f)]
    public float BrushDetail = 0.1f;
    [Range(0.0f,1.0f)]
    public float BrushFill = 0.5f;
    [Range(5000,200000)]
    public int NumStrokes=0x8000;
    [Range(0.5f,0.95f)]
    public float LayerScale = 0.8f;
    [Range(0.0f,1.0f)]
    public float Canvas = 0.4f;
    [Range(0.0f,100.0f)]
    public float FlickerFreq = 15.0f;
    [Range(0.0f,1.0f)]
    public float FlickerStrength = 0.0f;
    [Range(0.0f,360.0f)]
    public float LightAng = 135.0f;
    [Range(0.0f,90.0f)]
    public float LightOffs = 60.0f;
    [Range(0.0f,1.0f)]
    public float PaintDiff = 0.15f;
    [Range(0.0f,1.0f)]
    public float PaintSpec = 0.15f;
    [Range(0.0f,1.0f)]
    public float PaintShiny = 0.5f;
    [Range(0.0f,2.0f)]
    public float ColorSpread = 0.3f;
    [Range(0.0f,120.0f)]
    public float ScreenFOV = 0.0f;
    [Range(0.0f,360.0f)]
    public float StrokeAng = 0.0f;
    [Range(-1.0f,1.0f)]
    public float StrokeBend = -1.0f;
    [Range(0.0f,1.0f)]
    public float StrokeDir = 0.0f;
    [Range(0.0f,1.0f)]
    public float StrokeContour = 1.0f;
    [Range(0.0f,16.0f)]
    public float MultiStroke = 6.0f;
    [Range(0.0f,100000.0f)]
    public float strokeSeed = 10.0f;
    [Range(0.0f,1.0f)]
    public float Vignette = 1.0f;
    [Range(0.0f,1.0f)]
    public float CanvasBg = 0.5f;
    public Color CanvasTint = new Color(1.0f,0.97f,0.85f);
    //###PublicVars
    [Header("Other")]
    public bool flipY=false;
    public bool geomFlipY=false;

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

        bufferOrder.Add("Buff_A");
        buffers["Buff_A"] = createRenderTex();
        textureCh["Buff_A"] = new Dictionary <int,string> ();
        textureDemandsMip["Buff_A"] = new Dictionary <int,bool> ();
        shaders["Buff_A"] = createShader("flockaroo_OilPainting/Buff_A");
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
        shaders["Geom_A"] = createShader("flockaroo_OilPainting/Geom_A");
        meshes["Geom_A"] = createMesh(NumStrokes*2);
        NumTriangles=NumStrokes*2;
        textureCh["Geom_A"][0] = "https://ak1.picdn.net/shutterstock/videos/15325111/preview/stock-footage-beautiful-young-woman-enjoying-her-vacation-on-santorini-happy-tourist-is-wearing-sunhat.webm";
        textureCh["Geom_A"][1] = "rand256";
        textureCh["Geom_A"][2] = "Buff_A";
        textureDemandsMip["Geom_A"][0]=true;
        bufferOrder.Add("Image");
        buffers["Image"] = createRenderTex();
        textureCh["Image"] = new Dictionary <int,string> ();
        textureDemandsMip["Image"] = new Dictionary <int,bool> ();
        shaders["Image"] = createShader("flockaroo_OilPainting/Image");
        textureCh["Image"][0] = "Geom_A";
        textureCh["Image"][1] = "rand256";
        textureCh["Image"][2] = "Buff_A";
        textureCh["Image"][3] = "https://ak1.picdn.net/shutterstock/videos/15325111/preview/stock-footage-beautiful-young-woman-enjoying-her-vacation-on-santorini-happy-tourist-is-wearing-sunhat.webm";

        //###InitMarker
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
        return mainTex;
        // FIXME: alloc textures if not present
        //return textures["rand256"];
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        mainTex=src;
        bool reinit=false;

        if(inputTexture)
        {
            mainTex = new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGBFloat);
            Graphics.Blit(inputTexture, mainTex);
        }

        if (renderToTexture  && outputTexture==null) { reinit=true; }
        if (!renderToTexture && outputTexture!=null) { reinit=true; }

        if( mainTex.width!=actWidth || mainTex.height!=actHeight || reinit )
        {
            Debug.Log("OilPainting 1st init (or Resolution changed)");
            initAll(mainTex.width,mainTex.height);
        }

        if(NumTriangles!=NumStrokes*2)
        {
            meshes["Geom_A"] = createMesh(NumStrokes*2);
            NumTriangles=NumStrokes*2;
        }

        if(useMipOnMain)
        {
            initMainMipmapRenderTexture(mainTex);
            Graphics.Blit(mainTex, mainMip);
            mainTex = mainMip;
        }

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

            mat.SetFloat("BrushDetail",(BrushDetail*0.8f+0.2f)*(BrushDetail*0.8f+0.2f));
            mat.SetFloat("BrushSize",BrushFill*2.0f+0.25f);
            mat.SetFloat("Canvas",Canvas);
            mat.SetFloat("EffectFade",EffectFade);
            mat.SetFloat("FlickerFreq",FlickerFreq);
            mat.SetFloat("FlickerStrength",FlickerStrength);
            mat.SetFloat("halfFOV",ScreenFOV/180.0f*3.14159265359f*0.5f);
            mat.SetFloat("LayerScale",LayerScale);
            mat.SetFloat("LightAng",LightAng/180.0f*3.14159265359f);
            mat.SetFloat("LightOffs",LightOffs/180.0f*3.14159265359f);
            mat.SetFloat("NumTriangles",NumStrokes*2);
            mat.SetFloat("PaintDiff",PaintDiff);
            mat.SetFloat("PaintSpec",PaintSpec);
            mat.SetFloat("PaintShiny",PaintShiny);
            mat.SetFloat("PanFade",PanFade);
            mat.SetFloat("SrcBlur",SrcBlur);
            mat.SetFloat("SrcBright",SrcBright);
            mat.SetFloat("SrcContrast",SrcContrast);
            mat.SetFloat("SrcColor",SrcColor);
            mat.SetFloat("ColorSpread",ColorSpread);
            mat.SetFloat("StrokeAng",StrokeAng/180.0f*3.14159265359f);
            mat.SetFloat("StrokeBend",StrokeBend);
            mat.SetFloat("StrokeContour",StrokeContour);
            mat.SetFloat("StrokeDir",StrokeDir);
            mat.SetVector("strokeNumXY",new Vector4(MultiStroke,MultiStroke,0.0f,0.0f));
            mat.SetFloat("strokeSeed",strokeSeed);
            mat.SetFloat("Vignette",Vignette);
            mat.SetColor("CanvasTint", CanvasTint);
            mat.SetFloat("CanvasBg", CanvasBg);
            mat.SetFloat("linear2Gamma", linear2Gamma?1.0f:0.0f);
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
                Graphics.SetRenderTarget(buffers[buffName]);
                GL.Clear(true, true, Color.clear);
                if(mat!=null) mat.SetPass(0);
                foreach(Mesh mesh in meshes[buffName])
                {
                    Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
                }
            }
            else
            {
                if(buffName=="Image")
                {
                    if(mat!=null)
                        if(outputTexture)
                        {
                            Graphics.SetRenderTarget(outputTexture/*.value*/);
                            mat.SetPass(0);
                            Graphics.DrawMeshNow(screenQuadMesh[0], Vector3.zero, Quaternion.identity);
                            //cmd.Blit(mainTex, outputTexture, mat);
                            // default blit of screen - no effect
                            Graphics.Blit(mainTex, dest);
                        }
                        else
                        {
                            //cmd.Blit(mainTex, dest, mat);
                            Graphics.SetRenderTarget(dest);
                            mat.SetPass(0);
                            Graphics.DrawMeshNow(screenQuadMesh[0], Vector3.zero, Quaternion.identity);
                        }
                }
                else
                {
                    if(mat!=null)
                    {
                        Graphics.SetRenderTarget(buffers[buffName]);
                        mat.SetPass(0);
                        Graphics.DrawMeshNow(screenQuadMesh[0], Vector3.zero, Quaternion.identity);
                        //cmd.Blit(mainTex, buffers[buffName], mat);
                    }
                }
            }
        }

    }
    /*public void OnPostRender() {
    }*/

}
}
