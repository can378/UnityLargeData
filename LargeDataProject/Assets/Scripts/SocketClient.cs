using UnityEngine;
using SocketIOClient;
using System.Threading.Tasks;
using System;

public class SocketManager : MonoBehaviour
{
    private SocketIO client;
    public CubeLoader cubeLoader;

    async void Start()
    {
        client = new SocketIO("http://localhost:8001"); // FastAPI 서버 주소
        cubeLoader = GetComponent<CubeLoader>();

        client.OnConnected += async (sender, e) =>
        {
            Debug.Log("🟢 Socket.IO 연결됨");
        };

        // SocketManager.cs의 수정된 cube_updated 처리
        client.On("cube_updated", response =>
        {
            string jsonString = response.ToString().TrimStart('[').TrimEnd(']');
            CubeData updatedCube = JsonUtility.FromJson<CubeData>(jsonString);

            if (updatedCube != null)
            {
                Debug.Log("수정 " + updatedCube.seq + "=" + updatedCube.column5);

                if (cubeLoader.cubeSeqToIndexMap.TryGetValue(updatedCube.seq, out int index))
                {
                    cubeLoader.cubes[index] = updatedCube;

                    string[] parts = updatedCube.column5.Split('&');
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
                        }
                    }

                    int batchIndex = index / cubeLoader.batchSize;
                    int cubeIndexInBatch = index % cubeLoader.batchSize;

                    MaterialPropertyBlock props = cubeLoader.propertyBatches[batchIndex][cubeIndexInBatch];
                    if (props == null) props = new MaterialPropertyBlock();

                    props.SetColor("_BaseColor", colorToUse);
                    cubeLoader.propertyBatches[batchIndex][cubeIndexInBatch] = props;

                    Debug.Log($"Cube {updatedCube.seq} 갱신 완료");
                }
                else
                {
                    Debug.LogWarning($"Cube seq {updatedCube.seq}를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogError("JSON 변환 실패");
            }
        });


        await client.ConnectAsync();
    }

    async void OnDestroy()
    {
        await client.DisconnectAsync();
    }
}
