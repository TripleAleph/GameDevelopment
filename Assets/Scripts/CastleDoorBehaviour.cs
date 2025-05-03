using UnityEngine;

public class CastleDoorBehaviour : MonoBehaviour
{
   Animator animator;
   AudioSource sound;
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
      animator.SetBool("DoorOpens", true);
      sound.Play();
   }

   private void OnTriggerExit(Collider other)
   {
      animator.SetBool("DoorOpens", false);
      //sound.Play();
   }
}
