using UnityEngine;
using TMPro;

public class GirlBehaviour : MonoBehaviour
{
   public GameObject player;
   int talkDistance = 8;
   Animator animator;
   public GameObject OrangeCrystalInPlayerHand;
   public GameObject PurpleCrystalInPlayerHand;
   public GameObject GreenCrystalInPlayerHand;
   public GameObject BlueCrystalInPlayerHand;
   public TMP_Text StartQuest;
   public TMP_Text EndQuest;

   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
   {
      animator = GetComponent<Animator>();
   }

   // Update is called once per frame
   void Update()
   {
      float distance = Vector3.Distance(transform.position, player.transform.position);
      if (distance < talkDistance)
      {
         if (OrangeCrystalInPlayerHand.gameObject.activeInHierarchy &&
              PurpleCrystalInPlayerHand.gameObject.activeInHierarchy &&
              GreenCrystalInPlayerHand.gameObject.activeInHierarchy &&
              BlueCrystalInPlayerHand.gameObject.activeInHierarchy)
         {
            animator.SetInteger("State", 2);
            EndQuest.gameObject.SetActive(true);
         }
         else if (animator.GetInteger("State") == 0 &&
              !OrangeCrystalInPlayerHand.gameObject.activeInHierarchy &&
              !PurpleCrystalInPlayerHand.gameObject.activeInHierarchy &&
              !GreenCrystalInPlayerHand.gameObject.activeInHierarchy &&
              !BlueCrystalInPlayerHand.gameObject.activeInHierarchy)
         {
            animator.SetInteger("State", 1);
            StartQuest.gameObject.SetActive(true);
         }


         // rotate NPC towards the player when NPC starts speak
         Vector3 target = player.transform.position - transform.position;
         target.y = 0; // to prevent falling down
         Vector3 tmp = Vector3.RotateTowards(transform.forward, target, Time.deltaTime, 0.1f);
         transform.rotation = Quaternion.LookRotation(tmp);
      }
      else // the player is far enough
      {
         if (animator.GetInteger("State") != 0)
            animator.SetInteger("State", 0);
         StartQuest.gameObject.SetActive(false);
         EndQuest.gameObject.SetActive(false);
      }
   }
}
