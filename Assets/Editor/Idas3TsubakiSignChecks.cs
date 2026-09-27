using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

// Actual course geometry plus the production fragment shader, using private
// diagnostic targets. No ordinary saves, game input or native physics involved.
public static class Idas3TsubakiSignChecks {
    const string Output="Verification/tsubaki-signs-20260926";
    static readonly Type Course=typeof(Idas8HakoneCourse);
    static MethodInfo Method(string name)=>Course.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Vector3 V(BinaryReader r)=>new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    public static void Build(){Run();Idas3Build.RebuildWindowsPlayer();}
    public static void Run(){
        Directory.CreateDirectory(Output);var summary=new StringBuilder();
        foreach(string variant in new[]{"","day_wet","night_dry","night_wet"}){
            string folder=Path.Combine("RuntimeAssets/TSUBAKI",variant);
            var data=JsonUtility.FromJson<Idas8HakoneCourse.Manifest>(File.ReadAllText(Path.Combine(folder,"scene.json")));
            int text=0,unmarked=0;var tiles=new System.Collections.Generic.HashSet<int>();
            using(var r=new BinaryReader(File.OpenRead(Path.Combine(folder,"scene.bin")))){
                r.ReadBytes(4);int count=r.ReadInt32();
                for(int shape=0;shape<count;++shape){
                    int mat=r.ReadInt32(),nv=r.ReadInt32(),ni=r.ReadInt32();r.ReadBytes(48);
                    if(!(bool)Method("IsTsubakiSignAtlas").Invoke(null,new object[]{data.materials[mat]})){r.BaseStream.Position+=nv*44L+ni*4L;continue;}
                    var v=new Vector3[nv];var n=new Vector3[nv];var uv=new Vector2[nv];var uv2=new Vector2[nv];var colors=new Color32[nv];var ix=new int[ni];
                    for(int i=0;i<nv;++i){v[i]=V(r);n[i]=V(r);uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());uv2[i]=new Vector2(r.ReadSingle(),r.ReadSingle());colors[i]=new Color32(r.ReadByte(),r.ReadByte(),r.ReadByte(),r.ReadByte());}
                    for(int i=0;i<ni;++i)ix[i]=r.ReadInt32();var original=(int[])ix.Clone();
                    object[] args={v,n,uv,uv2,colors,ix};var tags=(Vector2[])Method("TsubakiSignFaceTags").Invoke(null,args);
                    var vv=(Vector3[])args[0];var nn=(Vector3[])args[1];var tt=(Vector2[])args[2];var tt2=(Vector2[])args[3];var cc=(Color32[])args[4];
                    for(int i=0;i<ni;++i){int a=original[i],b=ix[i];Check(v[a]==vv[b]&&n[a]==nn[b]&&uv[a]==tt[b]&&uv2[a]==tt2[b]&&colors[a].Equals(cc[b]),"Changed authored geometry/UV/color");}
                    for(int i=0;i<ni;i+=3){
                        float axis=tags==null?0:tags[ix[i]].y;
                        if(tags!=null)Check(axis==tags[ix[i+1]].y&&axis==tags[ix[i+2]].y,"Correction leaks across a triangle");
                        if(axis==0){unmarked++;continue;}
                        int tile=(int)Method("TsubakiSignTile").Invoke(null,new object[]{uv[original[i]],uv[original[i+1]],uv[original[i+2]]});
                        Check(tile>=0,"Unknown text tile");tiles.Add(tile);text++;
                        foreach(int at in new[]{i,i+1,i+2}){
                            var a=uv[original[at]];var b=axis>0?new Vector2(axis-a.x,a.y):new Vector2(a.x,-axis-a.y);
                            Check(b.x>=0&&b.x<=1&&b.y>=0&&b.y<=1,"Reflected sign escapes atlas");
                        }
                    }
                }
            }
            Check(text>400&&unmarked>1000,"Signs or ordinary scenery missing");
            // Independent named samples from separate parts of the source atlas.
            foreach(var sample in new[]{new Vector2(1728,1850),new Vector2(1856,1880),new Vector2(672,1920),new Vector2(800,1920),new Vector2(900,1696)}){
                var uv=sample/2048;int tile=(int)Method("TsubakiSignTile").Invoke(null,new object[]{uv,uv+new Vector2(.0002f,0),uv+new Vector2(0,.0002f)});
                Check(tiles.Contains(tile),"Actual course missing corrected sign at "+sample);
            }
            summary.AppendLine($"PASS {variant}: {text} sign triangles; {unmarked} other triangles untouched; {tiles.Count} source text tiles.");
            string atlas=Array.Find(data.materials,s=>s.name=="downhill_O_barricade_panel1").textures[0].file;
            Gpu(Path.Combine(folder,atlas),variant==""?"day_dry":variant,summary);
        }
        Idas3SponsorChecks.Run();
        File.WriteAllText(Path.Combine(Output,"PASS.txt"),summary.ToString());Debug.Log(summary.ToString());
    }
    static void Gpu(string source,string variant,StringBuilder summary){
        var texture=(Texture2D)Method("DecodeTexture").Invoke(null,new object[]{File.ReadAllBytes(source),source});
        texture.filterMode=FilterMode.Point;
        var material=new Material(Resources.Load<Shader>("Idas8ImportedShadowCheck"));material.mainTexture=texture;
        var mesh=new Mesh();var commands=new CommandBuffer();
        var target=new RenderTexture(256,256,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        var image=new Texture2D(256,256,TextureFormat.RGBA32,false,true);var old=RenderTexture.active;
        try{
            Check(target.Create(),"Sign GPU target");
            var names=new[]{"direction-motohakone","direction-route135","speed40","speed30","vertical-warning","sponsor-honda"};
            var rects=new[]{new Vector4(1664,1824,128,64),new Vector4(1792,1856,128,64),new Vector4(640,1888,64,64),new Vector4(768,1888,64,64),new Vector4(768,1664,-256,64),new Vector4(512,128,512,128)};
            for(int sample=0;sample<rects.Length;++sample){var box=rects[sample];bool rotated=box.z<0;
                float l=box.x/2048,r=(box.x+Mathf.Abs(box.z))/2048,t=box.y/2048,b=(box.y+box.w)/2048;
                // Inset UVs by one pixel to exclude neighboring atlas cells.
                l+=1/2048f;r-=1/2048f;t+=1/2048f;b-=1/2048f;
                Color32[] front=null,back=null;
                for(int pass=0;pass<3;++pass){
                    var v=new[]{new Vector3(-1,-1,.5f),new Vector3(1,-1,.5f),new Vector3(1,1,.5f),new Vector3(-1,1,.5f)};
                    // The diagnostic clip-space pass uses D3D render-target Y;
                    // keep the displayed glyphs upright as well as readable.
                    var n=new Vector3[4];var uv=rotated?new[]{new Vector2(r,b),new Vector2(r,t),new Vector2(l,t),new Vector2(l,b)}:new[]{new Vector2(l,t),new Vector2(r,t),new Vector2(r,b),new Vector2(l,b)};
                    if(pass>0){var tmp=uv[0];uv[0]=uv[1];uv[1]=tmp;tmp=uv[2];uv[2]=uv[3];uv[3]=tmp;}
                    var uv2=new Vector2[4];var colors=new[]{new Color32(255,255,255,255),new Color32(255,255,255,255),new Color32(255,255,255,255),new Color32(255,255,255,255)};var ix=new[]{0,1,2,0,2,3};
                    object[] args={v,n,uv,uv2,colors,ix};var tags=(Vector2[])Method("TsubakiSignFaceTags").Invoke(null,args);Check(tags!=null,"GPU sign not recognized");
                    mesh.Clear();mesh.vertices=(Vector3[])args[0];mesh.normals=(Vector3[])args[1];mesh.uv=(Vector2[])args[2];mesh.uv2=(Vector2[])args[3];mesh.colors32=(Color32[])args[4];mesh.uv3=tags;mesh.triangles=ix;
                    material.SetFloat("_ImportedSponsorSigns",pass==1?0:1);
                    commands.Clear();commands.SetRenderTarget(target);commands.SetViewport(new Rect(0,0,256,256));commands.ClearRenderTarget(true,true,Color.magenta);commands.DrawMesh(mesh,Matrix4x4.identity,material);Graphics.ExecuteCommandBuffer(commands);
                    RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();var pixels=image.GetPixels32();
                    File.WriteAllBytes(Path.Combine(Output,$"{variant}-{names[sample]}-{(pass==0?"front":pass==1?"before-back":"fixed-back")}.png"),image.EncodeToPNG());
                    if(pass==0)front=pixels;else if(pass==1)back=pixels;else{
                        int baseline=0,fixedPixels=0;
                        for(int i=0;i<pixels.Length;++i){if(Different(front[i],back[i]))baseline++;if(Different(front[i],pixels[i]))fixedPixels++;}
                        Check(baseline>1000,"Baseline did not reproduce mirrored lettering");Check(fixedPixels<100,"Front/back lettering differs after correction: "+names[sample]+" pixels="+fixedPixels);
                        summary.AppendLine($"PASS GPU {variant} {names[sample]}: mirrored baseline {baseline} pixels; corrected mismatch {fixedPixels} pixels.");
                    }
                }
            }
        }finally{RenderTexture.active=old;target.Release();commands.Dispose();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static bool Different(Color32 a,Color32 b)=>Math.Abs(a.r-b.r)>3||Math.Abs(a.g-b.g)>3||Math.Abs(a.b-b.b)>3;
}
