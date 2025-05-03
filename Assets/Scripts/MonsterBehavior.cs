using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class MonsterBehavior : MonoBehaviour
{
   public Slider HealthBar;
   int MaxHealth = 100;
   int CurrentHealth = 100;
   int damage = 10;
   bool isDead = false;

   Animator animator;
   NavMeshAgent agent;
   //LineRenderer line;

   public GameObject player;
   public GameObject Sword;
   float closeDistance = 18;
   private Coroutine animationDelayCoroutine;
   public GameObject Alarm;

   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
    {
      animator = GetComponent<Animator>();
      agent = GetComponent<NavMeshAgent>();
      //line = GetComponent<LineRenderer>();
   }

    // Update is called once per frame
    void Update()
    {
      float distance = Vector3.Distance(transform.position, player.transform.position);
      if (Alarm.activeInHierarchy == true && !isDead)
      {
         GetAttacked(Sword, KeyCode.Space);

         if (distance < closeDistance)
         {
            AttackPlayer(distance);
         }
         else
         {
            // if the player get farther than 10 in the middle of the attack
            animator.SetInteger("State", 1);
            agent.SetDestination(player.transform.position); // compute path usint AI A* algorithm
         }
      }

      /*if (agent.isActiveAndEnabled)
      {
         line.positionCount = agent.path.corners.Length;
         line.SetPositions(agent.path.corners);
      }*/
    }

   public void AttackPlayer(float distanceFromPlayer)
   {
      Vector3 directionAwayFromPlayer = (transform.position - player.transform.position).normalized;
      Vector3 positionOffset = player.transform.position + directionAwayFromPlayer * 10f;

      agent.isStopped = true;
      animator.SetInteger("State", 2);
   }

   IEnumerator delayDamage()
   {
      yield return new WaitForSeconds(1.5f);
      DoDamage();
   }
   public void GetAttacked(GameObject weapon, KeyCode theWeaponKey)
   {
      float distanceFromWeapon = Vector3.Distance(transform.position, weapon.transform.position);
      if (weapon.activeInHierarchy == true && distanceFromWeapon < closeDistance && Input.GetKeyDown(theWeaponKey))
      {
         StartCoroutine(delayDamage());
      }
   }

   public void DoDamage()
   {
      if (CurrentHealth > 0)
      {
         animator.SetInteger("State", 3);
         CurrentHealth -= damage;
      }
      else
      {
         isDead = true;
         animator.SetInteger("State", 4);
         agent.isStopped = true;
         Alarm.SetActive(false);
      }
      HealthBar.value = CurrentHealth / (float)MaxHealth;
   }
}
