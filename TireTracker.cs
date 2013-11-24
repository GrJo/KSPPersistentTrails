using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PersistentTrails
{
    //TODO make this a partModule
    class ModuleTireTracker : PartModule
    {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Tire Width")]
        public float tireWidth;

        [KSPField(isPersistant = false)]
        public float tireRadius;

        //[KSPField(isPersistant = true)]
        //public string textureName;
    }

    class TireRecorder
    {
        private TireRecorder()
        {
            tires = new Dictionary<ModuleTireTracker, GameObject>();
        }
        public static readonly TireRecorder Instance = new TireRecorder();

        //private GameObject trackContainer;


        private Dictionary<ModuleTireTracker, GameObject> tires;
        //PartModule trackingModule;

        public void update()
        {
            //Debug.Log("TireRecorder Update");

            if (FlightGlobals.ActiveVessel == null)
                return;

            //find all TireTracker modules on current vessel
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                //Debug.Log("part in activeVessel: " + p.name);

                foreach (PartModule module in p.Modules.OfType<ModuleTireTracker>())
                {
                    ModuleTireTracker tireModule = module as ModuleTireTracker;

                    updateForTire(tireModule);
                }

            }
        }

        //assumes checks for actual movement have already been done
        private void updateForTire(ModuleTireTracker tire)
        {
            //Debug.Log("TireRecorder.updateForTire() " + tire.part.name);
            //if (!tire.part.GroundContact) //doesnt work
            //    return;

            //Debug.Log("Part has ground contact!");

            GameObject tireObject;
            if (!tires.TryGetValue(tire, out tireObject))
            {
                //create mesh
                Debug.Log("Creating new Mesh for tire");
                Mesh m = new Mesh();
                m.triangles = new int[0];
                m.vertices = new Vector3[0];

                tireObject = MeshFactory.makeMeshGameObject(ref m, "TireObject");

                //TODO texture
                tireObject.renderer.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
                tireObject.renderer.renderer.castShadows = false;
                tireObject.renderer.renderer.receiveShadows = false;


                tireObject.renderer.material.SetColor("_EmissiveColor", Color.red);

                tires.Add(tire, tireObject);
                //.renderer.material.te
            }

            Mesh mesh = tireObject.GetComponent<MeshFilter>().mesh;

            float width = 2;
            Vector3 groundContactCenter = tire.part.transform.position;
            //apply offset
            groundContactCenter.z -= tire.tireWidth;

            Vector3 tireAxis = tire.part.transform.right;

            int oldVertexCount = mesh.vertexCount;




            Vector3 newLeft = groundContactCenter - width / 2 * tireAxis;
            Vector3 newRight = groundContactCenter + width / 2 * tireAxis;
            //have we moved at all?
            if (oldVertexCount > 0
                && (newLeft - mesh.vertices[mesh.vertexCount - 2]).sqrMagnitude < 0.1f
                && (newRight - mesh.vertices[mesh.vertexCount - 1]).sqrMagnitude < 0.1f
            )
            {
                return;
            }

            //Debug.Log("updating tiretrack-mesh");
            //add new mesh-vertices 




            Vector3[] newVertices = new Vector3[oldVertexCount + 2];
            Vector3[] newNormals = new Vector3[oldVertexCount + 2];
            
            //copy old vertices
            for (int i = 0; i < mesh.vertexCount; ++i)
            {
                newVertices[i] = mesh.vertices[i];
                newNormals[i] = mesh.normals[i];
            }

            //Debug.Log("copied vertices and normals");

            //put 2 new vertices and triangles
            newVertices[oldVertexCount] = newLeft;
            newVertices[oldVertexCount + 1] = newRight;

            //Debug.Log("created vertices");
            int[] newTriangles = {};

            if (oldVertexCount > 2) //first update creates vertices only
            {
                Debug.Log("oldVertexCount = " + oldVertexCount + ", newTriangleIndex-ArraySize = " + 3 * (oldVertexCount + 2));

                newTriangles = new int[3 * (oldVertexCount + 2)];

                for (int i = 0; i < mesh.triangles.Length; ++i )
                    newTriangles[i] = mesh.triangles[i];

                newTriangles[3 * oldVertexCount] = oldVertexCount; //new left upper
                newTriangles[3 * oldVertexCount + 1] = oldVertexCount - 2; //old last left;
                newTriangles[3 * oldVertexCount + 2] = oldVertexCount - 1; //old last right

                newTriangles[3 * oldVertexCount + 3] = oldVertexCount + 1; //new right
                newTriangles[3 * oldVertexCount + 4] = oldVertexCount; //new left
                newTriangles[3 * oldVertexCount + 5] = oldVertexCount - 1; //old last right

                string indexString = "Triangle indices:";
                for (int i = 0; i < newTriangles.Length; ++i)
                    indexString += "[" +i+"] " + newTriangles[i] + ";";
                Debug.Log(indexString);
            }

            mesh.Clear();
            mesh.vertices = newVertices;
            mesh.normals = newNormals;

            if (oldVertexCount > 2)
            {
                mesh.triangles = newTriangles;
            }


        }
    }
}
