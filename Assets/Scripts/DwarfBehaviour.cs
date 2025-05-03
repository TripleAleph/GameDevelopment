using UnityEngine;
using TMPro;

public class DwarfBehaviour : MonoBehaviour
{
   public GameObject player;
   int talkDistance = 12;
   Animator animator;
   public TMP_Text MiddleQuest;
   public GameObject OrangeCrystalInPlayerHand;

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
         if (animator.GetInteger("State") == 0 && !OrangeCrystalInPlayerHand.gameObject.activeInHierarchy)
         {
            animator.SetInteger("State", 1);
            MiddleQuest.gameObject.SetActive(true);
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
         MiddleQuest.gameObject.SetActive(false);
      }
   }
}
