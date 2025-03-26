using UnityEngine;
using SocketIOClient;
using System.Threading.Tasks;
using System;
using Unity.VisualScripting;

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
            Debug.Log("📦 Raw JSON from server: " + response.ToString());

            try
            {
                string jsonString = response.ToString();

                // 배열 기호가 있으면 제거
                if (jsonString.TrimStart().StartsWith("[") && jsonString.TrimEnd().EndsWith("]"))
                {
                    jsonString = jsonString.TrimStart('[').TrimEnd(']');
                }

                CubeData cube = JsonUtility.FromJson<CubeData>(jsonString);
                HandleUpdatedCube(cube);
            }
            catch (Exception ex)
            {
                Debug.LogError("❌ JSON 파싱 실패: " + ex.Message);
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
            // update
            cubeLoader.cubes[index] = updatedCube;

            // set color
            int nowStatus = updatedCube.now_status;
            Color colorToUse = Color.gray;
            Debug.Log("nowstatus=" + nowStatus);

            if (Var.ColorMap.ContainsKey(nowStatus))
            {
                colorToUse = Var.ColorMap[nowStatus];
            }

            // apply color
            int batchIndex = index / cubeLoader.batchSize;
            int cubeIndexInBatch = index % cubeLoader.batchSize;

            MaterialPropertyBlock props = cubeLoader.propertyBatches[batchIndex][cubeIndexInBatch];
            if (props == null) props = new MaterialPropertyBlock();

            props.SetColor("_BaseColor", colorToUse);
            cubeLoader.propertyBatches[batchIndex][cubeIndexInBatch] = props;

            Debug.Log($"🎨 Cube {updatedCube.object_id} 갱신 완료");
        }
        else
        {
            Debug.LogWarning($"⚠️ Cube seq {updatedCube.object_id} 없다");
        }
    }



}


