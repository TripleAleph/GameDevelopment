using TMPro;
using UnityEngine;

public class DoorBehaviour : MonoBehaviour
{
    Animator animator;
    AudioSource sound;
   public GameObject Door;
   public GameObject player_camera;

   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
    {
        animator = GetComponent<Animator>();
        sound = GetComponent<AudioSource>();

        animator.SetBool("DoorOpens", false);
   }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
      RaycastHit hit; // saves where the sight hits
      if (Physics.Raycast(player_camera.transform.position, transform.forward, out hit))
      {
         float distance = hit.distance;
      }
      if (CoinBehaviour.numCoins >= 5)
      {
         animator.SetBool("DoorOpens", true);
         sound.Play();
      }
   }

    private void OnTriggerExit(Collider other)
    {
      if (CoinBehaviour.numCoins >= 5)
      {
         animator.SetBool("DoorOpens", false);
         //sound.Play();
      }
    }
}
