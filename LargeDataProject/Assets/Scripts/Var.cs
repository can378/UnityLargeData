using System.Collections.Generic;
using UnityEngine;

public class Var : MonoBehaviour
{
    public static string BaseUrl = "http://localhost:8001";// FastAPI 서버 주소
    public static string CubeApiUrl = BaseUrl + "/api/cubes";
    public static string CubeDetailUrl(string object_id) => $"{BaseUrl}/api/cube?object_id={object_id}";


    //color
    public static readonly Dictionary<int, Color> ColorMap = new Dictionary<int, Color>
    {
        {1, Color.red},
        {2, Color.green},
        {3, Color.white},
        {4, Color.yellow},
        {5, Color.blue},
    };


    //singleton
    public static Var instance = null;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
