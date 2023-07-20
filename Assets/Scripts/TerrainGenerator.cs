using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using static UnityEngine.Mesh;

public struct Vert
{
    public Vector2 position;
    public float height;

}
public class TerrainGenerator : MonoBehaviour
{
    public int size;
    public GameObject Prefab;
    public ComputeShader computeShader;
    public Vert[] data;

    public float FreqScale;
    public float AmplScale;

    public float MeshScale = 100;

    GameObject[] objects;

    GameObject plane;

    Mesh mesh;

    public Material material;

    public bool autoupdate;

    float startingAmp;
    float startingFreq;

    // Start is called before the first frame update
    void Start()
    {
        plane = GameObject.Find("Plane");
        startingAmp = AmplScale;
        startingFreq = FreqScale;
    }

    // Update is called once per frame
    void Update()
    {
        AmplScale = startingAmp * Mathf.Sin(Time.time);
        FreqScale = startingFreq * (Mathf.Cos(Time.time*1.414f)*0.5f + 2f);
        DrawMapInEditor();
    }
    public void DrawMapInEditor()
    {
        //DefineObjects();
        Profiler.BeginSample("Initializing data");
        data = new Vert[size * size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                data[x * size + y] = new()
                {
                    position = new Vector2(x, y) * FreqScale / (size),
                    height = 0
                };

                //GameObject go = objects[x * size + y];
                //go.transform.position = new Vector3(x,vert.height, y);
            }
        }
        Profiler.EndSample();
        OnRandomizeGPU();
    }

    public void OnRandomizeGPU()
    {
        Profiler.BeginSample("Running Compute Shader");
        int Vec2Size = sizeof(float) * 2;
        int floatSize = sizeof(float);
        int totalSize = Vec2Size + floatSize;


        ComputeBuffer vertBuffer = new ComputeBuffer(data.Length,totalSize);
        vertBuffer.SetData(data);



        computeShader.SetBuffer(0, "verts", vertBuffer);
        computeShader.SetFloat("resolution", data.Length);


        computeShader.Dispatch(0, data.Length / 64 + 1, 1, 1);
            //computeShader.Dispatch(0,data.Length/8 + 1,1,1);

        vertBuffer.GetData(data);

        Profiler.EndSample();
        //UseData();
        UseMesh();

        vertBuffer.Dispose();

    }
    public void UseMesh()
    {

        MakeMesh();
        Profiler.BeginSample("Combining into mesh");
        plane = GameObject.Find("Plane");
        if (plane == null)
        {
            plane = new GameObject("Plane");

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            plane.AddComponent<MeshFilter>().mesh = mesh;
            plane.AddComponent<MeshRenderer>().material = material;
        }
        else
        {
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            plane.GetComponent<MeshFilter>().mesh = mesh;
            plane.GetComponent<MeshRenderer>().material = material;
        }
        Profiler.EndSample();

    }
    public void UseData()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            Vert vert = data[i];
            Vector3 newPos = obj.transform.position;
            newPos.y = vert.height * AmplScale;
            obj.transform.position = newPos;
        }
    }
    public void MakeMesh()
    {

        Profiler.BeginSample("Constructing Mesh");
        mesh = new Mesh();
        mesh.name = "CustomMesh";
        if(size > 256)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = new Vector3[size * size];
        int height = size;
        int width = size;


        int vertexindex = 0;

        int verticesperline = width;

        Vector3[] verts = new Vector3[size * size];
        int trisize = (size - 1) * (size - 1) * 6;

        int[] triArray = new int[trisize];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {

                verts[vertexindex] = new Vector3(x/(size/MeshScale), data[x * size + y].height * AmplScale, y/(size/MeshScale));

                //  meshdata.vertices[vertexindex] = new Vector3(topleftX + x, heightmap[x, y] * heightmult, topleftz - y);

                //mesh.uv[vertexindex] = new Vector2(x / (float)width, y / (float)height);

                if (x < width - 1 && y < height - 1)
                {
                    triArray[(x * (size - 1) + y) * 6] = vertexindex;
                    triArray[(x * (size - 1) + y) * 6 + 1] = vertexindex + verticesperline;
                    triArray[(x * (size - 1) + y) * 6 + 2] = vertexindex + verticesperline + 1;
                    triArray[(x * (size - 1) + y) * 6 + 3] = vertexindex + verticesperline + 1;
                    triArray[(x * (size - 1) + y) * 6 + 4] = vertexindex + 1;
                    triArray[(x * (size - 1) + y) * 6 + 5] = vertexindex;
                }
                vertexindex++;
            }
        }
        Profiler.EndSample();
        Profiler.BeginSample("Applying Lists");
        mesh.vertices = verts;
        mesh.triangles = triArray;
        Profiler.EndSample();

    }
}
public class MapVert
{
    Vector2Int position;
    float height;
}
