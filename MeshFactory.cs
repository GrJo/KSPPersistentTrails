using System;
using System.Collections.Generic;
using UnityEngine;

namespace PersistentTrails
{
    class MeshFactory
    {
        public static GameObject makeMeshGameObject(ref Mesh mesh, string name) {

            GameObject go = new GameObject("Dynamic Mesh: " + name);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            filter.mesh = mesh;

            //renderer.material.color = color;

            return go;
        }

        public static Mesh createCone(float radius, float height, int approx)
        {
            Mesh cone = new Mesh();

            //create vertices
            int numVertices = approx + 1;
            Vector3[] vertices = new Vector3[numVertices];
            vertices[0] = new Vector3(0, 0, height);

            float angleIncrement = 2.0f * (float) Math.PI / approx;
            
            for(uint i=0; i<approx; ++i){
                float angle = angleIncrement * i; //angle from 0 to 2PI
                vertices[i+1] = new Vector3(    (float) Math.Cos(angle) * radius,
                                                (float) Math.Sin(angle) * radius,
                                                0);
            }

            //create triangles (three indices per face)
            int[] triangleVertexIndices = new int[approx * 3 + (approx-1) * 3];

            //mantle
            for(int i=0; i < approx; ++i){
                triangleVertexIndices[3 * i + 0] = 0; //first corner is always the top
                triangleVertexIndices[3 * i + 1] = i + 1;
                triangleVertexIndices[3 * i + 2] = (i + 2) % numVertices; 
                //i=0 => 0, 1, 2
                //i=1 => 0, 2, 3
            }

            int indexOffset = 3 * approx;
            //base
            for(int i=0; i < approx - 1; ++i){
                triangleVertexIndices[indexOffset + 3 * i + 0] = 1; //first corner is always the top
                triangleVertexIndices[indexOffset + 3 * i + 2] = i + 2;
                triangleVertexIndices[indexOffset + 3 * i + 1] = (i + 3) % numVertices; 
                //i=0 => 1, 2, 3
                //i=1 => 1, 3, 4
            }

            //texture coordinates

            //put it all together
            cone.vertices = vertices;
            cone.triangles = triangleVertexIndices;

            //Auto-Normals
            cone.RecalculateNormals();

            return cone;
        }

        public static Mesh createSphere() 
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();


            // create 12 vertices of a icosahedron
            float t = (1.0f + (float)Math.Sqrt(5.0)) / 2.0f;

            vertices.Add(new Vector3(-1, t, 0).normalized);
            vertices.Add(new Vector3(1, t, 0).normalized);
            vertices.Add(new Vector3(-1, -t, 0).normalized);
            vertices.Add(new Vector3(1, -t, 0).normalized);

            vertices.Add(new Vector3(0, -1, t).normalized);
            vertices.Add(new Vector3(0, 1, t).normalized);
            vertices.Add(new Vector3(0, -1, -t).normalized);
            vertices.Add(new Vector3(0, 1, -t).normalized);

            vertices.Add(new Vector3(t, 0, -1).normalized);
            vertices.Add(new Vector3(t, 0, 1).normalized);
            vertices.Add(new Vector3(-t, 0, -1).normalized);
            vertices.Add(new Vector3(-t, 0, 1).normalized);
            
            //Ico-Faces
            triangles.AddRange(new int[] {
                0, 11, 5,
                0, 5, 1,
                0, 1, 7,
                0, 7, 10,
                0, 10, 11,
                1, 5, 9,
                5, 11, 4,
                11, 10, 2,
                10, 7, 6,
                7, 1, 8,
                3, 9, 4,
                3, 4, 2,
                3, 2, 6,
                3, 6, 8,
                3, 8, 9,
                4, 9, 5,
                2, 4, 11,
                6, 2, 10,
                8, 6, 7,
                9, 8, 1});


            // refine triangles
            int recursionLevel = 2;
            for (int recursion = 0; recursion < recursionLevel; recursion++)
            {
                var faces2 = new List<int>();

                int numTriangles = triangles.Count / 3; //stored as indices

                for (int triIndex = 0; triIndex < numTriangles; ++triIndex)
                {
                    int triV1 = triangles[3 * triIndex];
                    int triV2 = triangles[3 * triIndex + 1];
                    int triV3 = triangles[3 * triIndex + 2];
                    // replace triangle by 4 triangles
                    int a = getOrCreateMiddlePoint(triV1, triV2, ref vertices, ref middlePointIndexCache);
                    int b = getOrCreateMiddlePoint(triV2, triV3, ref vertices, ref middlePointIndexCache);
                    int c = getOrCreateMiddlePoint(triV3, triV1, ref vertices, ref middlePointIndexCache);

                    faces2.AddRange(new int[] { triV1, a, c });
                    faces2.AddRange(new int[] { triV2, b, a });
                    faces2.AddRange(new int[] { triV3, c, b });
                    faces2.AddRange(new int[] { a, b, c });
                }
                triangles = faces2;
            }



            //inflate to sphere and scale 
            //for (var i = 0; i < vertices.Count; i++)
            //{
            //    vertices[i] *= radius;
            //}


            Mesh sphere = new Mesh();
            sphere.vertices = vertices.ToArray();
            sphere.triangles = triangles.ToArray();

            return sphere; 
        }


        // return index of point in the middle of p1 and p2
        private static int getOrCreateMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<Int64, int> midPointCache)
        {
            // first check if we have it already
            bool firstIsSmaller = p1 < p2;
            Int64 smallerIndex = firstIsSmaller ? p1 : p2;
            Int64 greaterIndex = firstIsSmaller ? p2 : p1;
            Int64 key = (smallerIndex << 32) + greaterIndex;

            int ret;
            if (midPointCache.TryGetValue(key, out ret))
            {
                return ret;
            }

            // not in cache, calculate it
            Vector3 point1 = vertices[p1];
            Vector3 point2 = vertices[p2];
            Vector3 middle = new Vector3(
                (point1.x + point2.x) / 2.0f,
                (point1.y + point2.y) / 2.0f,
                (point1.z + point2.z) / 2.0f);

            // add vertex makes sure point is on unit sphere
            vertices.Add(middle.normalized);
            int index = vertices.Count -1;

            // store it, return index
            midPointCache.Add(key, index);
            return index;
        }
    
    }


}
