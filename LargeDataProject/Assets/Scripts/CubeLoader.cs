using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

public class CubeLoader : MonoBehaviour
{

    private float renderStartTime = 0f;

    //직렬화 결과
    private Dictionary<string, string> cubeDict = new Dictionary<string, string>();
    

    void Start()
    {
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
        //Debug.Log($"데이터 개수: {req.downloadHandler.text.Split('\n').Length}개");
        ParseCustomFormat(req.downloadHandler.text);
    }

    void ParseCustomFormat(string raw)
    {
        string[] lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        cubeDict.Clear();

        foreach (string line in lines)
        {
            int idx = line.IndexOf('&');
            if (idx == -1) continue;

            string key = line.Substring(0, idx);
            string value = line.Substring(idx + 1);

            cubeDict[key] = value;
        }

        //JSON 직렬화!!!!!!!
        string json = JsonConvert.SerializeObject(cubeDict, Formatting.Indented);
        Debug.Log("JSON 직렬화:\n" + json);

    }



    
}