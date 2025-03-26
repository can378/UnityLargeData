using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

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
    public List<CubeData> cubes2 = new List<CubeData>();

    //public string csvUrl = "http://localhost:8001/api/cubes_csv";
    public string csvUrl3 = "http://127.0.0.1:8001/api/cubes_csv";

    void Start()
    {
        Debug.Log(csvUrl3);
        baseMaterial.enableInstancing = true;
        cubeMesh = CreateCubeMesh(cubeSize);
        StartCoroutine(LoadCubesFromCustom());
    }

    IEnumerator LoadCubesFromCustom()
    {
        string url = "http://127.0.0.1:8001/api/cubes_custom";
        UnityWebRequest req = UnityWebRequest.Get(url);
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Custom 포맷 다운로드 실패: " + req.error);
            yield break;
        }

        Debug.Log("✅ Custom 포맷 수신 성공");
        ParseCustomFormat(req.downloadHandler.text);
    }

    void ParseCustomFormat(string raw)
    {
        string[] lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string[] fields = line.Split('&');
            if (fields.Length != 6)
            {
                Debug.LogWarning("⚠️ 필드 수 불일치: " + line);
                continue;
            }

            CubeData cube = new CubeData
            {
                object_id = fields[0].Trim('\"'),
                now_status = int.Parse(fields[1]),
                receiving_dt = fields[2],
                shipping_dt = fields[3],
                remark = fields[4],
                cur_qty = float.Parse(fields[5])
            };

            cubes2.Add(cube);
            cubeSeqToIndexMap[cube.object_id] = cubes2.Count - 1;
        }

        Debug.Log($"✅ {cubes2.Count}개 큐브 로딩 완료 (커스텀)");
        StartCoroutine(RenderCubesCoroutine());
    }



    void ParseCSV(string csv)
    {
        cubeSeqToIndexMap.Clear();
        cubes2.Clear();

        string[] lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string[] headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length != headers.Length) continue;

            CubeData cube = new CubeData
            {
                object_id = row[0],
                now_status = int.Parse(row[1]),
                receiving_dt = row[2],
                shipping_dt = row[3],
                remark = row[4],
                cur_qty = float.Parse(row[5])
            };

            cubes2.Add(cube);
            cubeSeqToIndexMap[cube.object_id] = cubes2.Count - 1;
        }

        Debug.Log($"✅ {cubes2.Count}개 큐브 로딩 완료 (CSV)");
    }

    IEnumerator RenderCubesCoroutine()
    {
        matrixBatches.Clear();
        colorBatches.Clear();

        for (int index = 0; index < cubes2.Count; index++)
        {
            CubeData cube = cubes2[index];

            if (index % batchSize == 0)
            {
                matrixBatches.Add(new Matrix4x4[Mathf.Min(batchSize, cubes2.Count - index)]);
                colorBatches.Add(new Vector4[Mathf.Min(batchSize, cubes2.Count - index)]);
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

        cubes = cubes2.ToArray(); // 배열로 복사 (색상 업데이트 호환 위해)
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
                Debug.Log($"■ click Cube {id}: " + req.downloadHandler.text);
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
