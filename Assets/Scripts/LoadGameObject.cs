using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using OpenCTM;

public class LoadGameObject : MonoBehaviour
{

    private List<OpenCTM.Mesh> ctmMeshList;


    void Start()
    {
        ctmMeshList = new List<OpenCTM.Mesh>();

        readTest();
    }



    public void readTest()
    {
        FileStream file = new FileStream("Assets/Resources/brunnen.ctm", FileMode.Open);
        CtmFileReader reader = new CtmFileReader(file);

        reader.decode(ref ctmMeshList);

        for (int i = 0; i < ctmMeshList.Count; i++)
        {
            UnityEngine.Mesh um = new UnityEngine.Mesh();

            ctmMeshList[i].checkIntegrity();

            List<Vector3> Vertices = new List<Vector3>();

            for (int j = 0; j < ctmMeshList[i].getVertexCount(); j++)
                Vertices.Add(new Vector3(ctmMeshList[i].vertices[(j * 3)], ctmMeshList[i].vertices[(j * 3) + 1], ctmMeshList[i].vertices[(j * 3) + 2]));

            List<Vector2> UVList = new List<Vector2>();

            for (int j = 0; j < ctmMeshList[i].texcoordinates[0].values.Length / 2; j++)
                UVList.Add(new Vector2(ctmMeshList[i].texcoordinates[0].values[(j * 2)], ctmMeshList[i].texcoordinates[0].values[(j * 2) + 1]));

            um.vertices = Vertices.ToArray();
            um.triangles = ctmMeshList[i].indices.Clone() as int[];
            um.uv = UVList.ToArray();

            um.RecalculateBounds();
            um.RecalculateNormals();

            GameObject go = new GameObject();
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Diffuse"));
            mf.mesh = um;
        }





    }

}
