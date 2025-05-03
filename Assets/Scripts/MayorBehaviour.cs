using UnityEngine;

public class MayorBehaviour : MonoBehaviour
{
   Animator animator;
   public GameObject player;
   public GameObject OrangeCrystalInPlayerHand;
   public GameObject PurpleCrystalInPlayerHand;
   public GameObject GreenCrystalInPlayerHand;
   public GameObject BlueCrystalInPlayerHand;

   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
   {
      animator = GetComponent<Animator>();
      animator.SetInteger("State", 0);
   }

   // Update is called once per frame
   void Update()
   {
      float distance = Vector3.Distance(transform.position, player.transform.position);
      if (OrangeCrystalInPlayerHand.gameObject.activeInHierarchy && 
         PurpleCrystalInPlayerHand.gameObject.activeInHierarchy && 
         GreenCrystalInPlayerHand.gameObject.activeInHierarchy && 
         BlueCrystalInPlayerHand.gameObject.activeInHierarchy && 
         distance < 12)
      {
         animator.SetInteger("State", 1);

         // rotate NPC towards the player when NPC starts speak
         Vector3 target = player.transform.position - transform.position;
         target.y = 0; // to prevent falling down
         Vector3 tmp = Vector3.RotateTowards(transform.forward, target, Time.deltaTime, 0.1f);
         transform.rotation = Quaternion.LookRotation(tmp);

      }
   }
}