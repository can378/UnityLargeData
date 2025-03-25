using UnityEngine;
using SocketIOClient;
using System.Threading.Tasks;
using System;

public class SocketManager : MonoBehaviour
{
    private SocketIO client;
    public CubeLoader cubeLoader;

    void Awake(){
        cubeLoader = GetComponent<CubeLoader>();
    }

    async void Start()
    {
        
        //connect
        client = new SocketIO(Var.BaseUrl); 
        client.OnConnected += async (sender, e) =>
        {
            Debug.Log("🟢 Socket.IO 연결");
        };


        // 수정된 data 처리
        client.On("cube_updated", response =>
        {
            string jsonString = response.ToString().TrimStart('[').TrimEnd(']');
            CubeData updatedCube = JsonUtility.FromJson<CubeData>(jsonString);

            if (updatedCube != null)
            {
                Debug.Log("수정 id" + updatedCube.seq + "=" + updatedCube.column5);

                if (cubeLoader.cubeSeqToIndexMap.TryGetValue(updatedCube.seq, out int index))
                {

                    //update
                    cubeLoader.cubes[index] = updatedCube;

                    //set color
                    string[] parts = updatedCube.column5.Split('&');
                    Color colorToUse = Color.gray;
                    if (parts.Length > 1 && int.TryParse(parts[1], out int colorCode))
                    {
                        colorToUse=Var.ColorMap[colorCode];
                    }


                    //apply color
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
                    Debug.LogWarning($"Cube seq {updatedCube.seq}없다");
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
