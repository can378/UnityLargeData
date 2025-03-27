using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class CubeLoader : MonoBehaviour
{
    public Material baseMaterial;
    public Vector3 cubeSize = Vector3.one;

    public Dictionary<string, int> cubeSeqToIndexMap = new Dictionary<string, int>();

    private Mesh cubeMesh;
    private List<Matrix4x4[]> matrixBatches = new List<Matrix4x4[]>();
    public List<Vector4[]> colorBatches = new List<Vector4[]>();
    public int batchSize = 1023;

    public List<CubeData> cubes = new List<CubeData>();
    private bool renderReady = false;

    private float renderStartTime = 0f;

    private List<MaterialPropertyBlock> propertyBlocks = new List<MaterialPropertyBlock>();
    
    void Start()
    {
        baseMaterial.enableInstancing = true;
        
        //create cube mesh
        cubeMesh = CreateCubeMesh(cubeSize);
        
        renderStartTime = Time.realtimeSinceStartup; //시간 기록 시작

        //Load cube!
        StartCoroutine(LoadCubesFromCustom());
    }


    IEnumerator LoadCubesFromCustom()
    {
        float networkStartTime = Time.realtimeSinceStartup;

        //get data
        UnityWebRequest req = UnityWebRequest.Get(Var.CustomFormatApiUrl);
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();


        //check time
        float networkEndTime = Time.realtimeSinceStartup;
        float downloadDuration = networkEndTime - networkStartTime;
        Debug.Log($"⌛ 데이터 다운로드: {downloadDuration:F2}초");


        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("👹 get data 실패: " + req.error);
            yield break;
        }

        Debug.Log("csv 수신 성공");

        ParseCustomFormat(req.downloadHandler.text);
    }

    void ParseCustomFormat(string raw)
    {
        string[] lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            //&으로 구분
            string[] fields = line.Split('&');

            //데이터 변경하게 되면 바꿔야함
            if (fields.Length != 6)
            {
                Debug.LogWarning("필드 수 불일치: " + line);
                continue;
            }

            CubeData cube = new CubeData
            {
                object_id = fields[0].Trim('"'),
                now_status = int.Parse(fields[1]),
                receiving_dt = fields[2],
                shipping_dt = fields[3],
                remark = fields[4],
                cur_qty = float.Parse(fields[5])
            };

            cubes.Add(cube);
            cubeSeqToIndexMap[cube.object_id] = cubes.Count - 1;
        }

        Debug.Log($"{cubes.Count}개 큐브 로딩 완료");
        StartCoroutine(RenderCubesCoroutine());
    }



    //cube들 그리기
    IEnumerator RenderCubesCoroutine()
    {
        matrixBatches.Clear();
        colorBatches.Clear();
        propertyBlocks.Clear();

        for (int index = 0; index < cubes.Count; index++)
        {
            CubeData cube = cubes[index];

            if (index % batchSize == 0)
            {
                matrixBatches.Add(new Matrix4x4[Mathf.Min(batchSize, cubes.Count - index)]);
                colorBatches.Add(new Vector4[Mathf.Min(batchSize, cubes.Count - index)]);
            }

            int x = index % 100;
            int y = (index / 100) % 100;
            int z = index / (100 * 100);
            Vector3 pos = new Vector3(x * 1.2f, y * 1.2f, z * 1.2f);
            matrixBatches[index / batchSize][index % batchSize] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

            int nowStatus = cube.now_status;
            Color color = Var.ColorMap.ContainsKey(nowStatus) ? Var.ColorMap[nowStatus] : Color.gray;
            colorBatches[index / batchSize][index % batchSize] = (Vector4)color;

            if (index % 1000 == 0)
                yield return null;
        }

        // MaterialPropertyBlock
        for (int i = 0; i < matrixBatches.Count; i++)
        {
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            props.SetVectorArray("_BaseColor", colorBatches[i]);
            propertyBlocks.Add(props);
        }

        // 렌더 준비 시간 측정 종료
        float elapsed = Time.realtimeSinceStartup - renderStartTime;
        Debug.Log($"⌛ 렌더 준비 시간: {elapsed:F2}초");

        renderReady = true;

    }


    void Update()
    {
        if (!renderReady) return;

        for (int i = 0; i < matrixBatches.Count; i++)
        {
            Graphics.DrawMeshInstanced(
                cubeMesh,
                0,
                baseMaterial,
                matrixBatches[i],
                matrixBatches[i].Length,
                propertyBlocks[i]
            );
        }
    }


    //update한거 받아왔을 때 색 변경하는 것. 안쓰면 지워도됨.
    public void UpdateCubeColor(string objectId, int newStatus)
    {
        if (!cubeSeqToIndexMap.TryGetValue(objectId, out int index)) return;

        Vector4 newColor = Var.ColorMap.ContainsKey(newStatus) ? (Vector4)Var.ColorMap[newStatus] : (Vector4)Color.gray;
        int batchIndex = index / batchSize;
        int inBatchIndex = index % batchSize;

        colorBatches[batchIndex][inBatchIndex] = newColor;
        cubes[index].now_status = newStatus;
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