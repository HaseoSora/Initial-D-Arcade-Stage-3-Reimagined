using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public static class Idas8SadamineChecks
{
    static int checks;
    static void Check(bool value, string message) { ++checks; if (!value) throw new InvalidOperationException(message); }
    static Vector2[] Prepare(ref Vector3[] vertices, ref int[] indices, out Color32[] colors, out int paired)
    {
        var normals = new Vector3[vertices.Length]; var uv = new Vector2[vertices.Length]; var uv2 = new Vector2[vertices.Length];
        colors = new Color32[vertices.Length];
        for (int i = 0; i < colors.Length; ++i) colors[i] = i < 4 ? new Color32(255, 0, 0, 255) : new Color32(0, 0, 255, 255);
        return Idas8HakoneCourse.PrepareTreeFaces(ref vertices, ref normals, ref uv, ref uv2, ref colors, ref indices, out paired);
    }
    static Vector3[] Cards(float backZ = .5f) => new[] {
        new Vector3(-.8f,-.8f,.5f),new Vector3(.8f,-.8f,.5f),new Vector3(.8f,.8f,.5f),new Vector3(-.8f,.8f,.5f),
        new Vector3(-.8f,-.8f,backZ),new Vector3(.8f,-.8f,backZ),new Vector3(.8f,.8f,backZ),new Vector3(-.8f,.8f,backZ)};
    static int[] OppositeDiagonals() => new[] { 0,1,2,0,2,3,4,7,5,5,7,6 };
    static void Synthetic(string output)
    {
        var vertices = Cards(); var indices = OppositeDiagonals();
        var tags = Prepare(ref vertices, ref indices, out var colors, out int paired);
        Check(paired == 4 && tags != null, "Opposite diagonals are not recognized as paired leaf faces");
        var separated = Cards(.51f); var separatedIndices = OppositeDiagonals();
        Prepare(ref separated, ref separatedIndices, out _, out int separateCount);
        Check(separateCount == 0, "Separated leaves incorrectly lose their back faces");
        var lone = Cards(); var loneIndices = new[] {0,1,2,0,2,3};
        Prepare(ref lone, ref loneIndices, out _, out int loneCount);
        Check(loneCount == 0, "A single-sided source card must remain visible from both sides");
        var disjoint = Cards(); for (int i = 4; i < 8; ++i) disjoint[i].x += 3;
        var disjointIndices = OppositeDiagonals(); Prepare(ref disjoint, ref disjointIndices, out _, out int disjointCount);
        Check(disjointCount == 0, "Disjoint coplanar cards are not paired faces");
        var partial = Cards(); for (int i = 4; i < 8; ++i) partial[i].x += .2f;
        var partialIndices = OppositeDiagonals(); Prepare(ref partial, ref partialIndices, out _, out int partialCount);
        Check(partialCount == 4, "Opposing cards with different silhouettes still overlap");
        foreach(string name in new[]{"bush_a1","forest_a","forest_a3","sakura_main","corner_grass_b"})
            Check(Idas8HakoneCourse.UsesPairedFoliageFaces("SADAMINE",new Idas8HakoneCourse.Surface{name=name,kind="crs_a",cutoff=.4f}),"Roadside foliage excluded: "+name);
        Check(!Idas8HakoneCourse.UsesPairedFoliageFaces("SADAMINE",new Idas8HakoneCourse.Surface{name="house_tank2",kind="crs_a",cutoff=.4f}),"Non-foliage material included");
        var scattered=new Vector3[800];var scatteredIndices=new int[1200];
        for(int card=0;card<100;++card){var points=Cards();var triangles=OppositeDiagonals();
            for(int v=0;v<8;++v)scattered[card*8+v]=points[v]+new Vector3((card%10)*3,0,(card/10)*3);
            for(int t=0;t<12;++t)scatteredIndices[card*12+t]=triangles[t]+card*8;}
        Prepare(ref scattered,ref scatteredIndices,out _,out int scatteredCount);
        Check(scatteredCount==400,"Spatial sweep missed or misclassified separated paired cards");

        var mesh = new Mesh { vertices = vertices, colors32 = colors, uv3 = tags, triangles = indices };
        var material = new Material(Resources.Load<Shader>("Idas3Scene")); material.EnableKeyword("IDAS_IMPORTED_COURSE");
        material.mainTexture = Texture2D.whiteTexture; material.SetFloat("_ImportedSky", 1);
        var frame = new Vector4[23]; for (int i = 0; i < 4; ++i) frame[i][i] = 1;
        var buffer = new ComputeBuffer(23, 16); material.SetBuffer("_IdasFrameWords", buffer);
        var target = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        var readback = new Texture2D(128,128,TextureFormat.RGBA32,false,true);
        var command = new CommandBuffer(); var old = RenderTexture.active;
        try {
            Check(target.Create(), "Tree GPU target unavailable");
            foreach (int side in new[] {1,-1}) {
                frame[4] = new Vector4(0,0,side*2,1); buffer.SetData(frame);
                command.Clear(); command.SetRenderTarget(target); command.SetViewport(new Rect(0,0,128,128));
                command.ClearRenderTarget(true,true,Color.black); command.SetViewProjectionMatrices(Matrix4x4.identity,Matrix4x4.identity);
                command.SetGlobalVector("_ProjectionParams",new Vector4(1,.1f,100,.01f));
                command.DrawMesh(mesh,Matrix4x4.identity,material); Graphics.ExecuteCommandBuffer(command);
                RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,128,128),0,0);readback.Apply();
                File.WriteAllBytes(Path.Combine(output,"opposite-diagonals-"+side+".png"),readback.EncodeToPNG());
                for(int y=24;y<104;y+=8)for(int x=24;x<104;x+=8){
                    Color32 pixel=readback.GetPixel(x,y);
                    Check(side>0?pixel.r>250&&pixel.b<5:pixel.b>250&&pixel.r<5,"Overlapping back face rendered through the facing leaf");
                }
            }
        }finally{RenderTexture.active=old;command.Dispose();buffer.Dispose();target.Release();
            UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(readback);}
    }
    static void Course(string course,string variant,StreamWriter report)
    {
        string root=Path.Combine("RuntimeAssets",course,variant=="day_dry"?"":variant);
        var manifest=JsonUtility.FromJson<Idas8HakoneCourse.Manifest>(File.ReadAllText(Path.Combine(root,"scene.json")));
        int total=0,trees=0,roadside=0,roadsideMeshes=0;
        using(var r=new BinaryReader(File.OpenRead(Path.Combine(root,"scene.bin")))){
            r.ReadInt32();int count=r.ReadInt32();
            for(int shape=0;shape<count;++shape){
                int material=r.ReadInt32(),nv=r.ReadInt32(),nt=r.ReadInt32();
                var matrix=Matrix4x4.identity;for(int row=0;row<3;++row)for(int col=0;col<4;++col)matrix[row,col]=r.ReadSingle();
                var surface=manifest.materials[material];
                if(!Idas8HakoneCourse.UsesPairedFoliageFaces(course,surface)){r.BaseStream.Position+=44L*nv+4L*nt;continue;}
                bool tree=surface.kind=="tree";if(tree)++trees;else ++roadsideMeshes;var vertices=new Vector3[nv];
                for(int i=0;i<nv;++i){vertices[i]=matrix.MultiplyPoint3x4(new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle()));r.BaseStream.Position+=32;}
                var indices=new int[nt];for(int i=0;i<nt;++i)indices[i]=r.ReadInt32();
                var tags=Prepare(ref vertices,ref indices,out _,out int paired);if(tree)total+=paired;else roadside+=paired;
                Check(paired<=nt/3,"Invalid tree coverage count");
                if(paired>0)Check(tags.Length==indices.Length,"Tree winding tags missing");
            }
        }
        report.WriteLine(course+","+variant+","+trees+","+total+","+roadsideMeshes+","+roadside);
        report.Flush();
        Check(trees>0&&total>(course=="SADAMINE"?500:1000),"Imported paired tree coverage was lost: "+course+" "+variant+" "+total);
        if(course=="SADAMINE")Check(roadsideMeshes>100&&roadside>17000,"Sadamine roadside overlap correction missing: "+variant+" "+roadside);
    }
    public static void Run(){
        checks=0;string output=Path.GetFullPath("Verification/sadamine-bounce-20260926/trees-"+DateTime.Now.ToString("HHmmss"));Directory.CreateDirectory(output);
        Synthetic(output);
        using(var report=new StreamWriter(Path.Combine(output,"coverage.csv"))){report.WriteLine("course,variant,treeMeshes,pairedTriangles,roadsideMeshes,pairedRoadsideTriangles");
            foreach(string course in new[]{"SADAMINE","HAKONE"})foreach(string variant in new[]{"day_dry","day_wet","night_dry","night_wet"})Course(course,variant,report);}
        File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks+" tree coverage/GPU checks");Debug.Log("PASS "+checks+" tree coverage/GPU checks: "+output);
    }
}
