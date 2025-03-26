using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class CubeLoader : MonoBehaviour
{
    public string apiUrl = Var.CubeApiUrl;
    public Material baseMaterial;
    public Vector3 cubeSize = Vector3.one;

    public Dictionary<int, int> cubeSeqToIndexMap = new Dictionary<int, int>();

    private Mesh cubeMesh;
    private List<Matrix4x4[]> matrixBatches = new List<Matrix4x4[]>();
    public List<MaterialPropertyBlock[]> propertyBatches = new List<MaterialPropertyBlock[]>();
    public int batchSize = 1023;

    public CubeData[] cubes;



    
    

    void Start()
    {
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

            cubeSeqToIndexMap.Clear();  // 딕셔너리 초기화

            int index = 0;
            for (int i = 0; i < cubes.Length; i++)
            {
                cubeSeqToIndexMap[cubes[i].seq] = i;  // seq를 index로 매핑
                
                if (index % batchSize == 0)
                {
                    matrixBatches.Add(new Matrix4x4[Mathf.Min(batchSize, cubes.Length - index)]);
                    propertyBatches.Add(new MaterialPropertyBlock[Mathf.Min(batchSize, cubes.Length - index)]);
                }

                int x = index % 100;
                int y = (index / 100) % 100;
                int z = index / (100 * 100);

                Vector3 pos = new Vector3(x * 1.2f, y * 1.2f, z * 1.2f);
                matrixBatches[index / batchSize][index % batchSize] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

                MaterialPropertyBlock props = new MaterialPropertyBlock();

                string[] parts = cubes[i].column5.Split('&');
                Color colorToUse = Color.gray;

                if (parts.Length > 1 && int.TryParse(parts[1], out int colorCode))
                {
                    colorToUse=Var.ColorMap[colorCode];
                }

                props.SetColor("_BaseColor", colorToUse);
                propertyBatches[index / batchSize][index % batchSize] = props;

                GameObject colliderCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                colliderCube.transform.position = pos;
                colliderCube.transform.localScale = Vector3.one * 1.2f;

                colliderCube.GetComponent<MeshRenderer>().enabled = false;

                CubeIndex meta = colliderCube.AddComponent<CubeIndex>();
                meta.seq = cubes[i].seq;

                index++;
            }
        }
    }


    
    void Update()
    {
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        

        // DrawMesh 그대로 유지
        for (int i = 0; i < matrixBatches.Count; i++)
        {
            int batchLength = matrixBatches[i].Length;
            Vector4[] colors = new Vector4[batchLength];

            // 인스턴스 컬러 배열 설정
            for (int j = 0; j < batchLength; j++)
            {
                string[] parts = cubes[i * batchSize + j].column5.Split('&');
                Color colorToUse = Color.gray;

                if (parts.Length > 1 && int.TryParse(parts[1], out int colorCode))
                {
                    colorToUse = Var.ColorMap[colorCode];
                }

                colors[j] = colorToUse;
            }

            props.Clear();
            props.SetVectorArray("_BaseColor", colors);

            Graphics.DrawMeshInstanced(
                cubeMesh,
                0,
                baseMaterial,  // 커스텀 셰이더를 사용하는 Material
                matrixBatches[i],
                batchLength,
                props, // 배열 형태로 한 번에 전달
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false,
                0,
                null
            );
        }


        // 마우스 클릭 처리
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CubeIndex meta = hit.collider.GetComponent<CubeIndex>();
                if (meta != null)
                {
                    int seq = meta.seq;
                    //Debug.Log($"■ 클릭 큐브 seq: {seq}");
                    StartCoroutine(GetCubeDetail(seq));
                }
            }
        }
    }


    IEnumerator GetCubeDetail(int seq)
    {
        string url = Var.CubeDetailUrl(seq);
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API 요청 실패: " + req.error);
            }
            else
            {
                Debug.Log($"■ click Cube {seq}: " + req.downloadHandler.text);
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


    //GC handler error 때문에 추가
    void OnDestroy()
    {
        matrixBatches.Clear();
        propertyBatches.Clear();
    }

}
