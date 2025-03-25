using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class CubeLoader : MonoBehaviour
{
    public string apiUrl = "http://127.0.0.1:8000/api/cubes";
    public Material baseMaterial;
    public Vector3 cubeSize = Vector3.one;

    private Mesh cubeMesh;
    private List<Matrix4x4[]> matrixBatches = new List<Matrix4x4[]>();
    private List<MaterialPropertyBlock[]> propertyBatches = new List<MaterialPropertyBlock[]>();
    private const int batchSize = 1023;

    private CubeData[] cubes;

    void Start()
    {
        cubeMesh = CreateCubeMesh(cubeSize);
        StartCoroutine(LoadCubeData());
        //CreateClickPlane(); // 클릭용 바닥 자동 생성
        //CreateCutPlane_X();

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

            // JSON 배열을 감싸서 JsonUtility가 읽을 수 있게 함
            string json = "{\"cubes\":" + req.downloadHandler.text + "}";

            CubeWrapper wrapper = JsonUtility.FromJson<CubeWrapper>(json);
            cubes = wrapper.cubes;
            
            
            //Debug.Log("cube개수="+cubes.Length);
            //test로 출력
            for (int i = 0; i < Mathf.Min(10, cubes.Length); i++)
            {
                Debug.Log($"[Cube {i}] seq: {cubes[i].seq}, {cubes[i].Column1}, {cubes[i].Column2}, column5: {cubes[i].Column5}");
                
            }
            Debug.Log("원본 JSON 응답: " + req.downloadHandler.text);


            int index = 0;
            for (int i = 0; i < cubes.Length; i++)
            {
                if (index % batchSize == 0)
                {
                    matrixBatches.Add(new Matrix4x4[Mathf.Min(batchSize, cubes.Length - index)]);
                    propertyBatches.Add(new MaterialPropertyBlock[Mathf.Min(batchSize, cubes.Length - index)]);
                }

                // 위치 설정정
                int x = index % 100;
                int y = (index / 100) % 100;
                int z = index / (100 * 100);

                Vector3 pos = new Vector3(x * 1.2f, y * 1.2f, z * 1.2f);
                matrixBatches[index / batchSize][index % batchSize] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

                // 색상 처리!!!!!!!!!!!!!!!!!!!!!!!
                MaterialPropertyBlock props = new MaterialPropertyBlock();

                string[] parts = cubes[i].Column5.Split('&');
                Color colorToUse = Color.gray;

                if (parts.Length > 1 && int.TryParse(parts[1], out int colorCode))
                {
                    switch (colorCode)
                    {
                        case 1: colorToUse = Color.red; break;
                        case 2: colorToUse = Color.green; break;
                        case 3: colorToUse = Color.white; break;
                        case 4: colorToUse = Color.yellow; break;
                        case 5: colorToUse = Color.blue; break;
                        default: colorToUse = Color.gray; break;
                    }
                }
                else
                {
                    Debug.LogWarning($"색상코드 얻기기 실패: {cubes[i].Column5}");
                }

                props.SetColor("_BaseColor", colorToUse);
                propertyBatches[index / batchSize][index % batchSize] = props;

                GameObject colliderCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                colliderCube.transform.position = pos;
                colliderCube.transform.localScale = Vector3.one * 1.2f; // spacing 고려

                // 렌더러 비활성화해서 안 보이게
                colliderCube.GetComponent<MeshRenderer>().enabled = false;

                // CubeMeta 스크립트에 seq 정보 저장 
                CubeIndex meta = colliderCube.AddComponent<CubeIndex>();
                if (meta == null)
                {
                    Debug.LogError("CubeIndex AddComponent 실패!");
                }
                else
                {
                    meta.seq = cubes[i].seq;
                }


                index++;
            }
        }

        

    }



    void Update()
    {
        // DrawMesh 그대로 유지
        for (int i = 0; i < matrixBatches.Count; i++)
        {
            for (int j = 0; j < matrixBatches[i].Length; j++)
            {
                Graphics.DrawMesh(
                    cubeMesh,
                    matrixBatches[i][j],
                    baseMaterial,
                    0,
                    null,
                    0,
                    propertyBatches[i][j]
                );
            }
        }

        // 마우스 클릭 처리
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            Debug.Log("click");
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CubeIndex meta = hit.collider.GetComponent<CubeIndex>();
                if (meta != null)
                {
                    int seq = meta.seq;
                    Debug.Log($"■ 클릭 큐브 seq: {seq}");
                    StartCoroutine(GetCubeDetail(seq));
                }
            }
        }
    }


    IEnumerator GetCubeDetail(int seq)
    {
        string url = $"http://127.0.0.1:8001/api/cube?seq={seq}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API 요청 실패: " + req.error);
            }
            else
            {
                Debug.Log($"Cube {seq}: " + req.downloadHandler.text);
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
