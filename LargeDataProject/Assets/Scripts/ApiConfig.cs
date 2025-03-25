using UnityEngine;

public class ApiConfig : MonoBehaviour
{
    public static string BaseUrl = "http://localhost:8001";
    public static string CubeApiUrl = BaseUrl + "/api/cubes";
    public static string CubeDetailUrl(int seq) => $"{BaseUrl}/api/cube?seq={seq}";

    private static ApiConfig instance = null;

    void Awake()
    {
        if (null == instance)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
