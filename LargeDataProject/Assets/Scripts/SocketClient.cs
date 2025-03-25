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
            Debug.Log("resoponse="+response);
            // 원본 문자열에서 배열 대괄호 제거 후 객체로 변환
            string jsonString = response.ToString().TrimStart('[').TrimEnd(']');

            // JSON 파싱으로 객체 변환
            CubeData updatedCube = JsonUtility.FromJson<CubeData>(jsonString);

            if (updatedCube != null)
            {
                Debug.Log("수정 " + updatedCube.seq + "=" + updatedCube.column5);

                for (int i = 0; i < cubeLoader.cubes.Length; i++)
                {
                    if (cubeLoader.cubes[i].seq == updatedCube.seq)
                    {
                        cubeLoader.cubes[i] = updatedCube;

                        // 색상 다시 설정
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

                        int batchIndex = i / cubeLoader.batchSize;
                        int cubeIndexInBatch = i % cubeLoader.batchSize;
                        MaterialPropertyBlock props = cubeLoader.propertyBatches[batchIndex][cubeIndexInBatch];
                        if (props == null) props = new MaterialPropertyBlock();

                        props.SetColor("_BaseColor", colorToUse);

                        // 반드시 수정된 MaterialPropertyBlock을 저장 (중요!)
                        cubeLoader.propertyBatches[batchIndex][cubeIndexInBatch] = props;


                        Debug.Log($"Cube {updatedCube.seq} 갱신 완료");
                        break;
                    }
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
