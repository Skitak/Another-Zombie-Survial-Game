using Shapes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public class FireLines : MonoBehaviour
{
    public GameObject linePrefab;
    ObjectPool<Line> pool;
    void Awake()
    {

    }
}
