using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PaladinBehaviour : MonoBehaviour
{
   public Slider HealthBar;
   int MaxHealth = 100;
   int CurrentHealth = 100;
   int damage = 10;
   bool isDead = false;

   Animator animator;
    NavMeshAgent agent;
   // LineRenderer line;
    public GameObject target1;
    public GameObject target2;
    private GameObject currentTarget;
    public GameObject player;
    public GameObject Sword;
   float closeDistance = 8;
   private Coroutine animationDelayCoroutine;

   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        //line = GetComponent<LineRenderer>();
         setNewTarget(target1);
    }

   // Update is called once per frame
   void Update()
   {
      if (!isDead)
      {
         checkTarget();
         GetAttacked(Sword, KeyCode.J);
      }

      /*if (agent.isActiveAndEnabled)
      {
         line.positionCount = agent.path.corners.Length;
         line.SetPositions(agent.path.corners);
      }*/
   }

   public void checkTarget()
   {
      float distanceFromTarget1 = Vector3.Distance(transform.position, target1.transform.position);
      float distanceFromTarget2 = Vector3.Distance(transform.position, target2.transform.position);
      float distanceFromPlayer = Vector3.Distance(transform.position, player.transform.position);

      if (currentTarget != player && distanceFromPlayer < 20) // Player is getting closer -> Player becomes the target
      {
         setNewTarget(player);
      }
      else if (currentTarget == target1 && distanceFromTarget1 < 3) // Current target is 1, but get close enought -> Target 2 becomes the target
      {
         setNewTarget(target2);
      }
      else if (currentTarget == target2 && distanceFromTarget2 < 3) // Current target is 2, but get close enought -> Target 1 becomes the target
      {
         setNewTarget(target1);
      }
      else if (currentTarget == player) // Current target is player and he is close enought -> Attack the player
      {
         if (distanceFromPlayer < closeDistance)
         {
            AttackPlayer(distanceFromPlayer);
         }
         else
         {
            animator.SetInteger("State", 1);
            setNewTarget(player);
         }
      }
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
      if (distanceFromWeapon < closeDistance && Input.GetKeyDown(theWeaponKey))
      {
         StartCoroutine(delayDamage());
      }
   }

   public void setNewTarget(GameObject newTarget)
   {
      agent.SetDestination(newTarget.transform.position); // compute path usint AI A* algorithm
      agent.isStopped = false;
      currentTarget = newTarget;
   }

   public void rotateNPCToPlayer()
   {
      // rotate NPC towards the player when NPC starts speak
      Vector3 target = player.transform.position - transform.position;
      target.y = 0; // to prevent falling down
      Vector3 tmp = Vector3.RotateTowards(transform.forward, target, Time.deltaTime, 0.1f);
      transform.rotation = Quaternion.LookRotation(tmp);
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
      }
      HealthBar.value = CurrentHealth / (float)MaxHealth;
   }
}
