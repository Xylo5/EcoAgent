using UnityEngine;

public class Building : MonoBehaviour
{
    public float pollution = 10f;  // Placeholder value

    private void Start()
    {
        Debug.Log("Building placed. Pollution: " + pollution);
    }
}