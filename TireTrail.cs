using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PersistentTrails
{
    class TireTrail
    {
        private string textureName;
        private GameObject tireObject;
        private float tireWidth;
        private float tireCircumfence;
        private Transform parentOffset;

        private Mesh tireMesh;
        private List<Vector3> vertices;
        private List<Vector2> uvCoords;
        private List<int> triangleVertexIndices;

        private float lastTextureU; // "1" for each full revolution of the wheel
        private Vector3 lastTirePosition;

        public TireTrail(Transform tireWorld)
        {
            Debug.Log("TireTrail constructor");
            //tirePart.GetReferenceTransform
            
            Texture2D texture = Utilities.LoadImage("Tires/default.png", 64, 128);
            Debug.Log("creating gameobject");
            tireObject = new GameObject("Dynamic Tire Mesh");
            MeshFilter filter = tireObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = tireObject.AddComponent<MeshRenderer>();
            Debug.Log("setting shadow");
            renderer.castShadows = false;
            //tireObject.collider.enabled = false;
            Debug.Log("setting material tex");
            renderer.material.mainTexture = texture;

            Debug.Log("setting transform");
            tireObject.transform.position = tireWorld.position;
            tireObject.transform.rotation = tireWorld.rotation;

            tireWidth = 2;
            tireCircumfence = 5;
            lastTextureU = 0;
            lastTirePosition = new Vector3(0, 0, 0);

            initializeMesh();
            filter.mesh = tireMesh;
        }

        private void initializeMesh()
        {
            Debug.Log("creating tiretrail mesh");
            tireMesh = new Mesh();

            //create 4 vertices for first quad
            //the mesh will grow into positive x direction, spaced evenly around y
            //Mesh goes like this
            // row0 ... row1 ...rowN
            //  v0      v2   ... v2n
            //  v1      v3   ... v2n+1
            
            vertices = new List<Vector3>{   new Vector3(0, -tireWidth/2),
                                            new Vector3(0, +tireWidth/2)};

            //create triangles (three indices per face)
            triangleVertexIndices = new List<int>{};


            //texture coordinates - 0,0 is bottom left
            uvCoords = new List<Vector2>{ new Vector2(0, 1),
                                          new Vector2(0, 0)};

        }

        public void updateMesh(Vector3 newTirePosition) 
        {
            Vector3 newTireRelative = newTirePosition - tireObject.transform.position;
            //http://docs.unity3d.com/Documentation/ScriptReference/Mesh.html 

            //Also note:Unity supports only 65k triangle arrays
            float deltaDistance = (lastTirePosition - newTireRelative).magnitude;
            Debug.Log("Updating tiretrail mesh, new tire pos = " + newTireRelative.ToString() + ", delta to previous is " + deltaDistance + "m");

            lastTirePosition = newTirePosition;

            int oldVertexCount = vertices.Count;

            //two new vertices at new deltaDistance TODO fix rotation etc
            vertices.Add(new Vector3(newTireRelative.x, newTireRelative.y - tireWidth / 2));
            vertices.Add(new Vector3(newTireRelative.x, newTireRelative.y + tireWidth / 2));

            //texture coords
            lastTextureU += deltaDistance / tireCircumfence;
            Debug.Log("texcoords u=" + lastTextureU);
            uvCoords.Add(new Vector2(lastTextureU, 1));
            uvCoords.Add(new Vector2(lastTextureU, 0));

            //triangles
            //Mesh goes like this
            // row0 ... row1 ... last old row
            //  v0      v2   ... v2n
            //  v1      v3   ... v2n+1
            triangleVertexIndices.Add(oldVertexCount - 2);
            triangleVertexIndices.Add(oldVertexCount - 1);
            triangleVertexIndices.Add(oldVertexCount);

            triangleVertexIndices.Add(oldVertexCount - 1);
            triangleVertexIndices.Add(oldVertexCount + 1);
            triangleVertexIndices.Add(oldVertexCount);

            //put it all together
            tireMesh.vertices = vertices.ToArray();
            tireMesh.uv = uvCoords.ToArray();
            tireMesh.triangles = triangleVertexIndices.ToArray();

            //Auto-Normals
            tireMesh.RecalculateNormals();
        }
    }
}
