using System.Collections.Generic;
using UnityEngine;

public sealed partial class Idas8HakoneCourse {
    // These four scenery atlases share their layout. Restrict the correction to
    // the atlas itself, including the wet mesh's combined guardrail material.
    internal static bool IsTsubakiSignAtlas(Surface surface){
        foreach(var texture in surface.textures)if(texture.type==1&&
            (texture.file=="8d0e6b7df194246cbad8.dds"||texture.file=="ffd3af778b4cabf6e5c8.dds"||
             texture.file=="34fe434760263278ce8f.dds"||texture.file=="853065f671e3746c5131.dds"))return true;
        return false;
    }
    // Pixel rectangles in the 2048-square source atlas. Negative width marks
    // lettering packed counterclockwise: its horizontal axis is decreasing V.
    // Separate tiles keep a reversed face from sampling its neighboring sign.
    static readonly Vector4[] TsubakiSignTiles={
        new Vector4(0,0,512,128),new Vector4(512,0,512,128),
        new Vector4(0,128,512,128),new Vector4(512,128,512,128),
        new Vector4(0,256,512,128),new Vector4(512,256,512,128),new Vector4(0,384,512,128),
        new Vector4(1920,256,64,128),new Vector4(1920,384,64,256), // Medical signs
        new Vector4(768,1280,128,256),new Vector4(896,1280,128,256), // Hiking directions
        new Vector4(1536,1408,256,128),new Vector4(1792,1408,256,128), // Park notices
        new Vector4(0,1536,128,128),new Vector4(128,1536,128,128),
        new Vector4(256,1536,-128,128),new Vector4(384,1536,-128,128), // Bus stops
        new Vector4(1280,1536,-128,128),new Vector4(1408,1536,-128,128),
        new Vector4(1536,1536,256,128),new Vector4(1792,1536,256,64), // Road closure / park name
        new Vector4(1792,1600,256,64),
        new Vector4(0,1664,-256,64),new Vector4(768,1664,-256,64), // Vertical warning boards
        new Vector4(1024,1664,-256,64),new Vector4(1280,1664,-256,64),
        new Vector4(256,1728,-128,32),new Vector4(512,1728,-128,32),new Vector4(640,1728,-128,32),
        new Vector4(1536,1728,-128,32),new Vector4(1664,1728,-128,32),
        new Vector4(1792,1664,128,64),new Vector4(1920,1664,128,64), // Small distance/name plates
        new Vector4(1408,1824,-128,64),
        new Vector4(1664,1824,128,64),new Vector4(1792,1856,128,64), // Blue direction signs
        new Vector4(640,1888,64,64),new Vector4(768,1888,64,64), // 40 / 30 km/h
        new Vector4(192,1920,64,64), // Route 75
        new Vector4(704,1952,64,32),new Vector4(960,1952,64,32), // Distance / restriction plates
        new Vector4(1920,1920,64,32),new Vector4(1984,1920,64,32)
    };
    internal static int TsubakiSignTile(Vector2 a,Vector2 b,Vector2 c){
        float left=Mathf.Min(a.x,b.x,c.x)*2048,right=Mathf.Max(a.x,b.x,c.x)*2048;
        float top=Mathf.Min(a.y,b.y,c.y)*2048,bottom=Mathf.Max(a.y,b.y,c.y)*2048;
        if(right-left<.01f||bottom-top<.01f)return -1;
        for(int i=0;i<TsubakiSignTiles.Length;++i){var tile=TsubakiSignTiles[i];
            if(left>=tile.x-.02f&&right<=tile.x+Mathf.Abs(tile.z)+.02f&&top>=tile.y-.02f&&bottom<=tile.y+tile.w+.02f)return i;
        }
        return -1;
    }
    internal static Vector2[] TsubakiSignFaceTags(ref Vector3[] vertices,ref Vector3[] normals,ref Vector2[] uv,
        ref Vector2[] uv2,ref Color32[] colors,ref int[] indices){
        var points=new List<Vector3>(vertices);var ns=new List<Vector3>(normals);
        var ts=new List<Vector2>(uv);var ts2=new List<Vector2>(uv2);var cs=new List<Color32>(colors);
        var tags=new List<Vector2>(new Vector2[vertices.Length]);var copies=new Dictionary<(int,int),int>();
        for(int i=0;i<indices.Length;i+=3){
            int tileId=TsubakiSignTile(uv[indices[i]],uv[indices[i+1]],uv[indices[i+2]]);if(tileId<0)continue;
            var tile=TsubakiSignTiles[tileId];float axis=(tile.z<0?-(2*tile.y+tile.w):2*tile.x+tile.z)/2048;
            for(int k=0;k<3;++k){int source=indices[i+k];var key=(source,tileId);
                if(!copies.TryGetValue(key,out int copy)){
                    copy=points.Count;copies.Add(key,copy);points.Add(vertices[source]);ns.Add(normals[source]);
                    ts.Add(uv[source]);ts2.Add(uv2[source]);cs.Add(colors[source]);tags.Add(new Vector2(0,axis));
                }
                indices[i+k]=copy;
            }
        }
        if(copies.Count==0)return null;
        vertices=points.ToArray();normals=ns.ToArray();uv=ts.ToArray();uv2=ts2.ToArray();colors=cs.ToArray();return tags.ToArray();
    }
}
