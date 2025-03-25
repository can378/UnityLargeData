using UnityEngine;
using System.Collections.Generic;

public class DrawInstancedCubes_Grid : MonoBehaviour
{
    public int instanceCount = 100000;
    public Material instanceMaterial;
    public Vector3 cubeSize = Vector3.one;
    public float spacing = 1.2f; // Cube 간격

    private Mesh instanceMesh;
    private List<Matrix4x4[]> matrixBatches = new List<Matrix4x4[]>();
    private const int batchSize = 1023;

    void Start()
    {
        Debug.Log("start create cube");
        
        instanceMesh = CreateCubeMesh(cubeSize);

        //100 x 100 x 10 정렬 (X, Y, Z)
        int countX = 100;
        int countY = 100;
        int countZ = Mathf.CeilToInt((float)instanceCount / (countX * countY));

        int index = 0;

        for (int z = 0; z < countZ && index < instanceCount; z++)
        {
            for (int y = 0; y < countY && index < instanceCount; y++)
            {
                for (int x = 0; x < countX && index < instanceCount; x++)
                {
                    if (index % batchSize == 0)
                    {
                        matrixBatches.Add(new Matrix4x4[Mathf.Min(batchSize, instanceCount - index)]);
                    }

                    Vector3 pos = new Vector3(
                        x * spacing,
                        y * spacing,
                        z * spacing
                    );

                    matrixBatches[index / batchSize][index % batchSize] =
                        Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                    
                    index++;

                    //Debug.Log(x+" "+y+" "+z);
                }
            }
        }

        // 카메라를 배열 방향으로 보게
        Camera.main.transform.position = new Vector3(countX * spacing / 2f, countY * spacing / 2f, -countZ * spacing * 2f);
        Camera.main.transform.LookAt(new Vector3(countX * spacing / 2f, countY * spacing / 2f, countZ * spacing / 2f));
    }

    void Update()
    {
        foreach (var matrices in matrixBatches)
        {
            Graphics.DrawMeshInstanced(instanceMesh, 0, instanceMaterial, matrices);
        }
    }

    Mesh CreateCubeMesh(Vector3 size)
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return mesh;
    }
}
