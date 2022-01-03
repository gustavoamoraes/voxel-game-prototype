using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class Voxel : MonoBehaviour
{
    public Vector3Int cubeSize;
    public float gridSize;

    public Vector3 offset;
    public float surfaceLevel;
    public bool autoUpdate;

    public Material meshMaterial;

    [Header("Noise Settings")]

    public int octaves;
    public float scaleMultiplier;
    public float noiseMultiplier;

    [Header("Shaders")]

    public ComputeShader mapCompute;
    public ComputeShader meshCompute;
    public RenderTexture noiseMap;
    public int textureSize;

    public void GenerateCube()
    {
        //Vector3Int sideCubes = new Vector3Int((int)(cubeSize.x / gridSize), (int)(cubeSize.y / gridSize), (int)(cubeSize.z / gridSize));
        //Dictionary<Vector3Int, bool> cubeMap = GenerateCubeMap(sideCubes);

        //CubeMesh cubeMesh = new CubeMesh(sideCubes, gridSize, cubeMap);

        //GetComponent<MeshFilter>().mesh = cubeMesh.GenerateCubeMesh();
        //GetComponent<MeshRenderer>().sharedMaterial = meshMaterial;

        noiseMap = new RenderTexture(textureSize, textureSize, 0);

        noiseMap.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat;
        noiseMap.volumeDepth = textureSize;
        noiseMap.enableRandomWrite = true;
        noiseMap.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;

        mapCompute.SetTexture(0, "NoiseTexture", noiseMap);
        meshCompute.SetTexture(0, "NoiseTexture", noiseMap);

        mapCompute.SetFloat("gridSize", gridSize);
        mapCompute.SetFloat("noiseScale", scaleMultiplier);
        mapCompute.SetFloat("noiseHeightMultiplier", noiseMultiplier);
        mapCompute.SetInt("textureSize", textureSize);

        mapCompute.Dispatch(0, textureSize/8, textureSize/8, textureSize / 8);

        noiseMap.Create(); 
    }

    public Dictionary<Vector3Int, bool> GenerateCubeMap(Vector3Int sideCubes)
    {
        Dictionary<Vector3Int, bool> cubeMap = new Dictionary<Vector3Int, bool>();

        var scale = (1f / scaleMultiplier * gridSize);
         
        for (int y = 0; y < sideCubes.y; y++)
        {
            for (int x = 0; x < sideCubes.x; x++)
            {
                for (int z = 0; z < sideCubes.z; z++)
                {
                    float noise = Perlin.Noise((offset.x + x) * scale, (offset.y + y) * scale, (offset.z + z) * scale);
                    float value = noise * noiseMultiplier;
                    cubeMap.Add(new Vector3Int(x, y, z), value < surfaceLevel);
                }
            }
        }

        return cubeMap;
    }

    public class CubeMesh
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        int triangleIndex = 0;

        Dictionary<Vector3Int, bool> cubeMap;
        Vector3 sideCubes;
        float pixelSize;

        public CubeMesh (Vector3Int sideCubes, float pixelSize, Dictionary<Vector3Int, bool> cubeMap)
        {
            this.sideCubes = sideCubes;
            this.cubeMap = cubeMap;
            this.pixelSize = pixelSize;
        }

        public Mesh GenerateCubeMesh()
        {
            Mesh mesh = new Mesh();

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            for (int x = 0; x < sideCubes.x; x++)
            {
                for (int y = 0; y < sideCubes.y; y++)
                {
                    for (int z = 0; z < sideCubes.z; z++)
                    {
                        //Index pos
                        Vector3Int normalPos = new Vector3Int(x, y, z);
                        //Actual position
                        Vector3 pos = (Vector3)normalPos * pixelSize + (Vector3.one * pixelSize / 2);

                        if (!cubeMap[normalPos])
                            continue;

                        bool up = y < sideCubes.y - 1 && cubeMap[normalPos + Vector3Int.up];
                        bool down = y > 0 && cubeMap[normalPos + Vector3Int.down]; ;
                        bool right = x < sideCubes.x - 1 && cubeMap[normalPos + Vector3Int.right];
                        bool left = x > 0 && cubeMap[normalPos + Vector3Int.left];
                        bool forward = z < sideCubes.z - 1 && cubeMap[normalPos + Vector3Int.forward];
                        bool backward = z > 0 && cubeMap[normalPos + Vector3Int.back];

                        //Draw no faces if none would be visible
                        if (up & down & left & right & forward & backward)
                            continue;

                        if (!up)
                        {
                            CreateFace(pos + Vector3.up * pixelSize / 2, Vector3.forward, Vector3.right, false);
                        }

                        if (!down)
                        {
                            CreateFace(pos - Vector3.up * pixelSize / 2, Vector3.back, Vector3.right, true);
                        }

                        if (!forward)
                        {
                            CreateFace(pos + Vector3.forward * pixelSize / 2, Vector3.up, Vector3.left, false);
                        }

                        if (!backward)
                        {
                            CreateFace(pos - Vector3.forward * pixelSize / 2, Vector3.up, Vector3.right, true);
                        }

                        if (!right)
                        {
                            CreateFace(pos + Vector3.right * pixelSize / 2, Vector3.up, Vector3.forward, false);
                        }

                        if (!left)
                        {
                            CreateFace(pos - Vector3.right * pixelSize / 2, Vector3.up, Vector3.back, true);
                        }
                    }
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();

            return mesh;
        }

        public void CreateFace (Vector3 faceCenter, Vector3 up, Vector3 right, bool inverted)
        {
            vertices.Add(faceCenter - (up * pixelSize / 2) - (right * pixelSize / 2));
            vertices.Add(faceCenter + (up * pixelSize / 2) - (right * pixelSize / 2));
            vertices.Add(faceCenter + (up * pixelSize / 2) + (right * pixelSize / 2));
            vertices.Add(faceCenter - (up * pixelSize / 2) + (right * pixelSize / 2));

            int[] faceTriangles = { triangleIndex, triangleIndex + 1, triangleIndex + 2, triangleIndex, triangleIndex + 2, triangleIndex + 3 };

            if(inverted)
                faceTriangles.Reverse();

            foreach (int index in faceTriangles)
            {
                triangles.Add(index);
            }

            triangleIndex += 4;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position + (Vector3)cubeSize/2f, cubeSize);
    }
}
