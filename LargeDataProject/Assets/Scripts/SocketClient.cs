using UnityEngine;
using SocketIOClient;
using System.Threading.Tasks;
using System;

public class SocketManager : MonoBehaviour
{
    private SocketIO client;
    public CubeLoader cubeLoader;

    void Awake()
    {
        cubeLoader = GetComponent<CubeLoader>();
    }

    async void Start()
    {
        client = new SocketIO(Var.BaseUrl);

        client.OnConnected += async (sender, e) =>
        {
            Debug.Log("🟢 Socket.IO 연결");
        };

        client.On("cube_updated", response =>
        {
            Debug.Log("💩 Raw JSON: " + response.ToString());

            try
            {
                string jsonString = response.ToString();

                if (jsonString.TrimStart().StartsWith("[") && jsonString.TrimEnd().EndsWith("]"))
                {
                    jsonString = jsonString.TrimStart('[').TrimEnd(']');
                }

                CubeData cube = JsonUtility.FromJson<CubeData>(jsonString);
                HandleUpdatedCube(cube);
            }
            catch (Exception ex)
            {
                Debug.LogError("JSON 파싱 실패: " + ex.Message);
            }
        });

        await client.ConnectAsync();
    }

    async void OnDestroy()
    {
        await client.DisconnectAsync();
    }

    void HandleUpdatedCube(CubeData updatedCube)
    {
        Debug.Log($"수정 id: {updatedCube.object_id}, now_status: {updatedCube.now_status}, remark: {updatedCube.remark}");

        if (cubeLoader.cubeSeqToIndexMap.TryGetValue(updatedCube.object_id, out int index))
        {
            // 데이터 갱신
            cubeLoader.cubes[index] = updatedCube;

            // 색상 업데이트
            cubeLoader.UpdateCubeColor(updatedCube.object_id, updatedCube.now_status);

            Debug.Log($"Cube {updatedCube.object_id} 색상 갱신 완료");
        }
        else
        {
            Debug.LogWarning($"Cube {updatedCube.object_id} 없음");
        }
    }
}
