using UnityEngine;

public class HealthBarBehavior : MonoBehaviour
{
   public GameObject PlayerCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
      transform.LookAt(PlayerCamera.transform.position);
    }
}
