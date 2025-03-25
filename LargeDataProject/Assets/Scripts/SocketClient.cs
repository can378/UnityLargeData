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

        client.On("cube_updated", response =>
        {
            CubeData updatedCube = response.GetValue<CubeData>();
            Debug.Log("수정된 것="+updatedCube);

            for (int i = 0; i < cubeLoader.cubes.Length; i++)
            {
                if (cubeLoader.cubes[i].seq == updatedCube.seq)
                {
                    cubeLoader.cubes[i] = updatedCube;

                    // 색상 다시 설정
                    string[] parts = updatedCube.Column5.Split('&');
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

                    int batchIndex = i / cubeLoader.batchSize;
                    int cubeIndexInBatch = i % cubeLoader.batchSize;
                    MaterialPropertyBlock props = new MaterialPropertyBlock();
                    props.SetColor("_BaseColor", colorToUse);
                    cubeLoader.propertyBatches[batchIndex][cubeIndexInBatch] = props;

                    Debug.Log($"Cube {updatedCube.seq} 갱신 완료");
                    break;
                }
            }
        });

        await client.ConnectAsync();
    }

    async void OnDestroy()
    {
        await client.DisconnectAsync();
    }
}
