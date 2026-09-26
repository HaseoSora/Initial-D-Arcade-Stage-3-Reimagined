using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class Idas8HakoneCourse
{
    // Back faces may use the opposite diagonal or additional vertices. Match
    // their covered area instead of assuming their triangle indices agree.
    static bool[] OverlappingTreeFaces(Vector3[] vertices, int[] indices)
    {
        int count = indices.Length / 3;
        var normals = new Vector3[count];
        var covered = new bool[count];
        var minimum = new Vector3[count]; var maximum = new Vector3[count]; var order = new int[count];
        var bounds = new Bounds();
        for (int i = 0; i < count; ++i) {
            var a=vertices[indices[i*3]];var b=vertices[indices[i*3+1]];var c=vertices[indices[i*3+2]];
            normals[i]=Vector3.Cross(b-a,c-a).normalized;
            minimum[i]=Vector3.Min(a,Vector3.Min(b,c));maximum[i]=Vector3.Max(a,Vector3.Max(b,c));order[i]=i;
            if(i==0)bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(minimum[i]);bounds.Encapsulate(maximum[i]);
        }
        int sweep=bounds.size.x>bounds.size.y?0:1;if(bounds.size.z>bounds.size[sweep])sweep=2;
        Array.Sort(order,(a,b)=>minimum[a][sweep].CompareTo(minimum[b][sweep]));
        var active=new List<int>();
        var polygon = new Vector2[8];
        var scratch = new Vector2[8];
        var other = new Vector2[3];
        // Roadside foliage meshes have thousands of triangles. Sweep their
        // bounds so distant cards never enter the coplanar polygon test.
        foreach (int i in order)
        {
            var a = vertices[indices[i * 3]];
            var b = vertices[indices[i * 3 + 1]];
            var c = vertices[indices[i * 3 + 2]];
            var n = normals[i];
            if (n.sqrMagnitude < .5f) continue;
            int axis = Mathf.Abs(n.x) > Mathf.Abs(n.y) ? 0 : 1;
            if (Mathf.Abs(n.z) > Mathf.Abs(n[axis])) axis = 2;
            Vector2 pa = ProjectTreeFace(a, axis), pb = ProjectTreeFace(b, axis), pc = ProjectTreeFace(c, axis);
            float area = Mathf.Abs(TreeCross(pb - pa, pc - pa)) * .5f;
            if (area < .000001f) continue;
            for(int at=active.Count-1;at>=0;--at)if(maximum[active[at]][sweep]<minimum[i][sweep]-.0001f)active.RemoveAt(at);
            foreach (int j in active)
            {
                if(covered[i]&&covered[j])continue;
                if (Vector3.Dot(n, normals[j]) > -.99999f) continue;
                if(minimum[i].x>maximum[j].x+.0001f||minimum[j].x>maximum[i].x+.0001f||
                   minimum[i].y>maximum[j].y+.0001f||minimum[j].y>maximum[i].y+.0001f||
                   minimum[i].z>maximum[j].z+.0001f||minimum[j].z>maximum[i].z+.0001f)continue;
                bool coplanar = true;
                for (int k = 0; k < 3; ++k)
                {
                    var v = vertices[indices[j * 3 + k]];
                    if (Mathf.Abs(Vector3.Dot(n, v - a)) > .0001f) { coplanar = false; break; }
                    other[k] = ProjectTreeFace(v, axis);
                }
                if (!coplanar) continue;
                polygon[0] = pa; polygon[1] = pb; polygon[2] = pc;
                float overlap = TreeIntersectionArea(polygon, scratch, other);
                // The two sides can also have different silhouette outlines.
                // Any nontrivial overlap needs facing rejection on both sides;
                // requiring total coverage leaves their shared interior fighting.
                if (overlap >= area * .001f) covered[i] = true;
                float otherArea=Mathf.Abs(TreeCross(other[1]-other[0],other[2]-other[0]))*.5f;
                if(otherArea>.000001f&&overlap>=otherArea*.001f)covered[j]=true;
            }
            active.Add(i);
        }
        return covered;
    }
    static Vector2 ProjectTreeFace(Vector3 p, int axis) => axis == 0 ? new Vector2(p.y, p.z) : axis == 1 ? new Vector2(p.x, p.z) : new Vector2(p.x, p.y);
    static float TreeCross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    static float TreeIntersectionArea(Vector2[] polygon, Vector2[] scratch, Vector2[] clip)
    {
        int count = 3;
        float sign = TreeCross(clip[1] - clip[0], clip[2] - clip[0]) >= 0 ? 1 : -1;
        for (int edge = 0; edge < 3 && count > 0; ++edge)
        {
            var origin = clip[edge]; var direction = clip[(edge + 1) % 3] - origin;
            int nextCount = 0;
            var previous = polygon[count - 1];
            float previousDistance = sign * TreeCross(direction, previous - origin);
            for (int i = 0; i < count; ++i)
            {
                var current = polygon[i]; float distance = sign * TreeCross(direction, current - origin);
                if ((distance >= 0) != (previousDistance >= 0))
                    scratch[nextCount++] = Vector2.LerpUnclamped(previous, current, previousDistance / (previousDistance - distance));
                if (distance >= 0) scratch[nextCount++] = current;
                previous = current; previousDistance = distance;
            }
            var swap = polygon; polygon = scratch; scratch = swap; count = nextCount;
        }
        float area = 0;
        for (int i = 1; i + 1 < count; ++i) area += TreeCross(polygon[i] - polygon[0], polygon[i + 1] - polygon[0]);
        return Mathf.Abs(area) * .5f;
    }
}
