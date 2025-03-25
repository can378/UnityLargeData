using UnityEngine;

public class DrawCubeTest:MonoBehaviour
{

    public GameObject cube;

    void Start()
    {
        GameObject testCube=Instantiate(cube);
        testCube.transform.position = new Vector3(0, 0, 0);
        
        
        
        // 확인용 큐브 1개만 생성
        //GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
       

        // 나머지 인스턴싱 로직 아래에 계속...
    }

}
