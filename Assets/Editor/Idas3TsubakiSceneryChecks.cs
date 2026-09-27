using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// Private scenery captures use the real loader, materials and shader without
// starting native gameplay or touching player data.
public static class Idas3TsubakiSceneryChecks {
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="Verification/tsubaki-clips-20260926";
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Hidden).SetValue(o,value);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Hidden).Invoke(o,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static object Static(string method,object[] args)=>typeof(Idas8HakoneCourse).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    static Vector3 V(BinaryReader r)=>new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    public static void Build(){Validate();Idas3Build.RebuildWindowsPlayer();}
    public static void Validate(){
        var report=new System.Text.StringBuilder();
        foreach(string variant in new[]{"day_dry","day_wet","night_dry","night_wet"}){
            string folder=Path.Combine("RuntimeAssets/TSUBAKI",variant=="day_dry"?"":variant);
            var data=JsonUtility.FromJson<Idas8HakoneCourse.Manifest>(File.ReadAllText(Path.Combine(folder,"scene.json")));
            int paired=0,signs=0,lone=0;var names=new System.Collections.Generic.HashSet<string>();
            using(var r=new BinaryReader(File.OpenRead(Path.Combine(folder,"scene.bin")))){
                r.ReadBytes(4);int count=r.ReadInt32();
                for(int shape=0;shape<count;++shape){
                    int mat=r.ReadInt32(),nv=r.ReadInt32(),ni=r.ReadInt32();var matrix=Matrix4x4.identity;for(int row=0;row<3;row++)for(int col=0;col<4;col++)matrix[row,col]=r.ReadSingle();
                    var surface=data.materials[mat];
                    if(!(bool)Static("UsesPairedFoliageFaces",new object[]{"TSUBAKI",surface})){r.BaseStream.Position+=nv*44L+ni*4L;continue;}
                    var v=new Vector3[nv];var n=new Vector3[nv];var uv=new Vector2[nv];var uv2=new Vector2[nv];var colors=new Color32[nv];var ix=new int[ni];
                    for(int i=0;i<nv;i++){v[i]=matrix.MultiplyPoint3x4(V(r));n[i]=matrix.MultiplyVector(V(r));uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());uv2[i]=new Vector2(r.ReadSingle(),r.ReadSingle());colors[i]=new Color32(r.ReadByte(),r.ReadByte(),r.ReadByte(),r.ReadByte());}
                    for(int i=0;i<ni;i++)ix[i]=r.ReadInt32();Vector2[] text=null;
                    if((bool)Static("IsTsubakiSignAtlas",new object[]{surface})){
                        object[] signArgs={v,n,uv,uv2,colors,ix};text=(Vector2[])Static("TsubakiSignFaceTags",signArgs);
                        v=(Vector3[])signArgs[0];n=(Vector3[])signArgs[1];uv=(Vector2[])signArgs[2];uv2=(Vector2[])signArgs[3];colors=(Color32[])signArgs[4];
                    }
                    var original=(int[])ix.Clone();object[] faceArgs={v,n,uv,uv2,colors,ix,text,0};var tags=(Vector2[])Static("PrepareSceneryFaces",faceArgs);int marked=(int)faceArgs[7];ix=(int[])faceArgs[5];
                    var vv=(Vector3[])faceArgs[0];var nn=(Vector3[])faceArgs[1];var tt=(Vector2[])faceArgs[2];var tt2=(Vector2[])faceArgs[3];var cc=(Color32[])faceArgs[4];
                    for(int i=0;i<ni;i++){
                        int a=original[i],b=ix[i];Check(v[a]==vv[b]&&n[a]==nn[b]&&uv[a]==tt[b]&&uv2[a]==tt2[b]&&colors[a].Equals(cc[b]),$"Scenery source attributes changed: {variant} {shape} {surface.name} corner {i}");
                        Check((text==null?0:text[a].y)==(tags==null?0:tags[b].y),"Lost sign orientation while splitting facing polygons");
                    }
                    for(int i=0;i<ni;i+=3){
                        if(tags!=null&&tags[ix[i]].x>0){Check(tags[ix[i+1]].x==1&&tags[ix[i+2]].x==1,"Partial facing tag");if(tags[ix[i]].y!=0)signs++;}
                        else lone++;
                    }
                    paired+=marked;if(marked>0&&surface.kind!="tree")names.Add(surface.name);
                }
            }
            Check(paired>100000&&signs>20&&lone>1000,"Incomplete actual scenery regression");
            // Night files batch the same terrain into O_barricade_panel.
            Check(names.Contains(variant.StartsWith("night")?"O_barricade_panel":"E_grail_ref_w6")&&names.Contains("downhill_O_barricade_panel1")&&names.Contains("P_bush_plane_a1"),"Missing road/barrier/foliage coverage: "+variant);
            report.AppendLine($"PASS {variant}: {paired} facing triangles, {signs} facing sign triangles retain lettering, {lone} unpaired triangles retained, {names.Count} static materials corrected.");
        }
        Idas3TsubakiSignChecks.Run();
        File.WriteAllText(Path.Combine(Output,"PASS.txt"),report.ToString());Debug.Log(report.ToString());
        foreach(string variant in new[]{"day_dry","day_wet","night_dry","night_wet"})CaptureVariant(variant);
    }
    public static void Run(){
        CaptureVariant("day_dry");
    }
    public static void WetBaseline(){CaptureVariant("day_wet");}
    static void CaptureVariant(string variant){
        Directory.CreateDirectory(Output);
        bool baseline=Array.IndexOf(Environment.GetCommandLineArgs(),"-idas3-roadside-foliage-baseline")>=0;
        string shots=Path.Combine(Output,baseline?"baseline":"verified",variant);Directory.CreateDirectory(shots);
        var go=new GameObject("Private Tsubaki scenery check");
        var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=60;camera.aspect=2.37f;camera.nearClipPlane=1;camera.farClipPlane=12000;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;camera.cullingMask=1<<28;
        var host=go.AddComponent<Idas3SceneGame>();host.enabled=false;
        var property=typeof(Idas3SceneGame).GetProperty("Status",Hidden);
        uint flags=16384u|16777216u|(variant.StartsWith("night")?65536u:0u)|(variant.EndsWith("wet")?131072u:0u);
        var status=Activator.CreateInstance(property.PropertyType);status.GetType().GetField("flags").SetValue(status,flags);property.SetValue(host,status);
        var scenery=new GameObject("Private course");var course=scenery.AddComponent<Idas8HakoneCourse>();course.enabled=false;
        string folder=Path.Combine("RuntimeAssets/TSUBAKI",variant=="day_dry"?"":variant);
        Set(course,"host",host);Set(course,"view",camera);Set(course,"root",Path.GetFullPath(folder));
        Set(course,"<LoadedCourse>k__BackingField","TSUBAKI");
        var data=JsonUtility.FromJson<Idas8HakoneCourse.Manifest>(File.ReadAllText(Path.Combine(folder,"scene.json")));Set(course,"data",data);
        var target=new RenderTexture(1280,540,24){antiAliasing=1};target.Create();camera.targetTexture=target;
        var image=new Texture2D(1280,540,TextureFormat.RGB24,false);var buffer=new ComputeBuffer(46,16);var old=RenderTexture.active;
        try {
            Call(course,"LoadRoad");Call(course,"LoadScene");foreach(var t in scenery.GetComponentsInChildren<Transform>())t.gameObject.layer=28;
            var roads=(Vector3[][])typeof(Idas8HakoneCourse).GetField("roads",Hidden).GetValue(course);
            foreach(bool reverse in new[]{false,true}){
            Set(course,"reverse",reverse);Call(course,"UpdateDirection");Set(course,"lightingProfile",-1);
            foreach(int p in new[]{164,500,1500,2900,3300,3500,3600,3770}){
                var eye=roads[0][p]+Vector3.up*1.4f;var look=roads[0][p+(reverse?-18:18)]+Vector3.up*1.4f;
                camera.transform.position=eye;camera.transform.LookAt(look);Call(course,"UpdateLighting",(float)p);Call(course,"UpdateScenery",eye);
                foreach(var t in scenery.GetComponentsInChildren<Transform>()){var renderer=t.GetComponent<MeshRenderer>();if(renderer!=null&&renderer.sharedMaterial!=null&&renderer.sharedMaterial.GetFloat("_ImportedSky")==1)t.position=eye;}
                var vp=GL.GetGPUProjectionMatrix(camera.projectionMatrix,false)*camera.worldToCameraMatrix;
                if(SystemInfo.usesReversedZBuffer)vp.SetRow(2,vp.GetRow(3)-vp.GetRow(2));vp=vp.transpose;
                var words=new Vector4[46];for(int r=0;r<4;r++)words[r]=vp.GetRow(r);words[4]=new Vector4(eye.x,eye.y,eye.z,1);buffer.SetData(words);
                Shader.SetGlobalBuffer("_IdasFrameWords",buffer);Shader.SetGlobalInt("_IdasView",0);Shader.SetGlobalVector("_IdasDepthProjection",Vector4.zero);
                    camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,540),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(shots,$"point-{p}-{(reverse?"uphill":"downhill")}.png"),image.EncodeToPNG());
            }
            }
        }finally{RenderTexture.active=old;camera.targetTexture=null;buffer.Dispose();target.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(scenery);UnityEngine.Object.DestroyImmediate(go);}
        Debug.Log("Tsubaki scenery captures complete");
    }
}
