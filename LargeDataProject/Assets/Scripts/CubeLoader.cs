using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class CubeLoader : MonoBehaviour
{
    public string apiUrl = Var.CubeApiUrl;
    public Material baseMaterial;
    public Vector3 cubeSize = Vector3.one;

    public Dictionary<string, int> cubeSeqToIndexMap = new Dictionary<string, int>();

    private Mesh cubeMesh;
    private List<Matrix4x4[]> matrixBatches = new List<Matrix4x4[]>();
    public List<Vector4[]> colorBatches = new List<Vector4[]>();
    public int batchSize = 1023;

    public CubeData[] cubes;

    void Start()
    {
        baseMaterial.enableInstancing = true;
        cubeMesh = CreateCubeMesh(cubeSize);
        StartCoroutine(LoadCubeData());
    }

    IEnumerator LoadCubeData()
    {
        
        using (UnityWebRequest req = UnityWebRequest.Get(apiUrl))
        {
            
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching cube data: " + req.error);
                yield break;
            }

            string json = "{\"cubes\":" + req.downloadHandler.text + "}";
            CubeWrapper wrapper = JsonUtility.FromJson<CubeWrapper>(json);
            cubes = wrapper.cubes;

            cubeSeqToIndexMap.Clear();

            int index = 0;
            for (int i = 0; i < cubes.Length; i++)
            {
                cubeSeqToIndexMap[cubes[i].object_id] = i;

                if (index % batchSize == 0)
                {
                    matrixBatches.Add(new Matrix4x4[Mathf.Min(batchSize, cubes.Length - index)]);
                    colorBatches.Add(new Vector4[Mathf.Min(batchSize, cubes.Length - index)]);
                }

                int x = index % 100;
                int y = (index / 100) % 100;
                int z = index / (100 * 100);
                Vector3 pos = new Vector3(x * 1.2f, y * 1.2f, z * 1.2f);
                matrixBatches[index / batchSize][index % batchSize] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

                int nowStatus = cubes[i].now_status;
                Color color = Var.ColorMap.ContainsKey(nowStatus) ? Var.ColorMap[nowStatus] : Color.gray;
                colorBatches[index / batchSize][index % batchSize] = (Vector4)color;

                index++;
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < matrixBatches.Count; i++)
        {
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            props.SetVectorArray("_BaseColor", colorBatches[i]);

            Graphics.DrawMeshInstanced(
                cubeMesh,
                0,
                baseMaterial,
                matrixBatches[i],
                matrixBatches[i].Length,
                props,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false,
                0,
                null
            );
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CubeIndex meta = hit.collider.GetComponent<CubeIndex>();
                if (meta != null)
                {
                    string id = meta.object_id;
                    StartCoroutine(GetCubeDetail(id));
                }
            }
        }
    }

    public void UpdateCubeColor(string objectId, int newStatus)
    {
        if (!cubeSeqToIndexMap.TryGetValue(objectId, out int index)) return;

        Vector4 newColor = Var.ColorMap.ContainsKey(newStatus) ? (Vector4)Var.ColorMap[newStatus] : (Vector4)Color.gray;
        int batchIndex = index / batchSize;
        int inBatchIndex = index % batchSize;

        colorBatches[batchIndex][inBatchIndex] = newColor;
        cubes[index].now_status = newStatus;
    }

    IEnumerator GetCubeDetail(string id)
    {
        string url = Var.CubeDetailUrl(id);
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API 요청 실패: " + req.error);
            }
            else
            {
                Debug.Log($"\u25A0 click Cube {id}: " + req.downloadHandler.text);
            }
        }
    }

    Mesh CreateCubeMesh(Vector3 size)
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return mesh;
    }

    void OnDestroy()
    {
        matrixBatches.Clear();
        colorBatches.Clear();
    }
} 
