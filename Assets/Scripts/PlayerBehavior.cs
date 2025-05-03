using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class PlayerBehavior : MonoBehaviour
{
   public GameObject player_camera; // connect int Unity !!!
   CharacterController controller;
   float speed = 15; //6
   float angular_speed = 100;
   AudioSource footSteps;

   // Inn:
   public TMP_Text OpenInnMinCoins;
   public GameObject InnDoorAxis;

   public PaladinBehaviour Paladin;
   public MonsterBehavior Monster;
   public GameObject Alarm;
   public GameObject MonsterCaveWall;

   // Sword:
   public GameObject swordInHand;
   public GameObject swordInRock;
   public Text pickSwordText;
   AudioSource swordSound;
   Animator swordAnimator;

   // Axe:
   public GameObject axeInHand;
   public GameObject axeInCave;
   public Text pickAxeText;
   AudioSource axeSound;
   Animator axeAnimator;

   // Wand:
   public GameObject wandInHand;
   public GameObject wandInCave;
   public Text pickWandText;
   AudioSource wandSound;
   Animator wandAnimator;

   // Staff:
   public GameObject staffInHand;
   public GameObject staffInCave;
   public Text pickStaffText;
   AudioSource staffSound;
   Animator staffAnimator;

   // Sheild:
   public GameObject sheildInHand;
   public GameObject sheildInCave;
   public Text pickSheildText;
   AudioSource sheildSound;
   Animator sheildAnimator;

   // Bow:
   public GameObject bowInHand;
   public GameObject bowInCave;
   public Text pickBowText;
   AudioSource bowSound;
   Animator bowAnimator;

   // Key:
   public GameObject keyInHand;
   public GameObject keyInCave;
   public Text pickKeyText;
   AudioSource keySound;
   Animator keyAnimator;

   // Chest:
   public GameObject ChestClose;
   public GameObject ChestOpen;
   public Text OpenChestText;
   AudioSource ChestSound;

   // Orange Crystal:
   public GameObject OrangeCrystal;
   public GameObject OrangeCrystalInHand;
   public Text pickOrangeCrystalText;

   // Purple Crystal:
   public GameObject PurpleCrystal;
   public GameObject PurpleCrystalInHand;
   public Text pickPurpleCrystalText;

   // Green Crystal:
   public GameObject GreenCrystal;
   public GameObject GreenCrystalInHand;
   public Text pickGreenCrystalText;

   // Blue Crystal:
   public GameObject BlueCrystal;
   public GameObject BlueCrystalInHand;
   public Text pickBlueCrystalText;

   // All Crystals
   public GameObject Crystals;

   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
   {
      // connects to the component in Unity
      controller = GetComponent<CharacterController>();
      footSteps = GetComponent<AudioSource>();

      swordSound = swordInHand.GetComponent<AudioSource>();
      swordAnimator = swordInHand.GetComponent<Animator>();

      axeSound = axeInHand.GetComponent<AudioSource>();
      axeAnimator = axeInHand.GetComponent<Animator>();

      wandSound = wandInHand.GetComponent<AudioSource>();
      wandAnimator = wandInHand.GetComponent<Animator>();

      staffSound = staffInHand.GetComponent<AudioSource>();
      staffAnimator = staffInHand.GetComponent<Animator>();

      sheildSound = sheildInHand.GetComponent<AudioSource>();
      sheildAnimator = sheildInHand.GetComponent<Animator>();

      bowSound = bowInHand.GetComponent<AudioSource>();
      bowAnimator = bowInHand.GetComponent<Animator>();

      keySound = keyInHand.GetComponent<AudioSource>();
      keyAnimator = keyInHand.GetComponent<Animator>();

      ChestSound = ChestClose.GetComponent<AudioSource>();

   }

   // Update is called once per frame
   void Update()
   {
      float rotation_about_x = Input.GetAxis("Mouse Y") * angular_speed * Time.deltaTime;
      float rotation_about_y = Input.GetAxis("Mouse X") * angular_speed * Time.deltaTime;

      float dx = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
      float dz = Input.GetAxis("Vertical") * speed * Time.deltaTime;

      // rotate camera
      player_camera.transform.Rotate(new Vector3(-rotation_about_x, 0, 0));

      // rotate player
      transform.Rotate(new Vector3(0, rotation_about_y, 0));

      // absolute motion (not adaptive)
      //transform.Translate(new Vector3(-dx, 0, dz));

      // we use the definitions in local coordinates
      Vector3 motion = new Vector3(dx, -0.5f, dz);

      // converts local coordinates into global coordinates
      motion = transform.TransformDirection(motion);
      controller.Move(motion); // uses global coordinates

      if (Mathf.Abs(dx) > 0.001f || Mathf.Abs(dz) > 0.001f)
      {
         if (!footSteps.isPlaying)
         {
            footSteps.Play();
         }
      }
      pickingSword();
      pickingWeapon(axeInCave, axeInHand, pickAxeText, KeyCode.A, axeAnimator, axeSound);
      pickingWeapon(wandInCave, wandInHand, pickWandText, KeyCode.W, wandAnimator, wandSound);
      pickingWeapon(staffInCave, staffInHand, pickStaffText, KeyCode.T, staffAnimator, staffSound);
      pickingWeapon(sheildInCave, sheildInHand, pickSheildText, KeyCode.H, sheildAnimator, sheildSound);
      pickingWeapon(bowInCave, bowInHand, pickBowText, KeyCode.B, bowAnimator, bowSound);
      pickingWeapon(keyInCave, keyInHand, pickKeyText, KeyCode.K, keyAnimator, keySound);
      OpenTheChest(ChestOpen, ChestClose, keyInHand, OpenChestText, KeyCode.K, ChestSound);

      AudioSource CrystalSound = Crystals.GetComponent<AudioSource>();
      pickOrangeCrystal(OrangeCrystal, OrangeCrystalInHand, ChestOpen, pickOrangeCrystalText, KeyCode.O, CrystalSound);
      pickCrystal(PurpleCrystal, PurpleCrystalInHand, pickPurpleCrystalText, KeyCode.P, CrystalSound);
      pickCrystal(GreenCrystal, GreenCrystalInHand, pickGreenCrystalText, KeyCode.G, CrystalSound);
      pickCrystal(BlueCrystal, BlueCrystalInHand, pickBlueCrystalText, KeyCode.C, CrystalSound);

      float distanceToInn = Vector3.Distance(transform.position, InnDoorAxis.transform.position);
      if(distanceToInn < 6)
      {
         OpenInnMinCoins.gameObject.SetActive(true);
      }
      else
      {
         OpenInnMinCoins.gameObject.SetActive(false);
      }

   }

   void pickingSword()
   {
      RaycastHit hit; // saves where the sight hits
      if (Physics.Raycast(player_camera.transform.position, transform.forward, out hit))
      {
         float distance = hit.distance;
         float MaxDist = 12;
         if (hit.collider.gameObject == swordInRock.gameObject && distance < MaxDist)
         {
            pickSwordText.gameObject.SetActive(true);
         }
         else
         {
            pickSwordText.gameObject.SetActive(false);
         }
      }

      if (Input.GetKeyDown(KeyCode.S) && pickSwordText.IsActive())
      {
         swordInRock.SetActive(false);
         swordInHand.SetActive(true);

         Alarm.SetActive(true);
         Animator anim = Alarm.GetComponent<Animator>();
         anim.SetTrigger("SetAlarm");
         AudioSource sound = Alarm.GetComponentInParent<AudioSource>();
         sound.Play();

         Animator anim2 = MonsterCaveWall.GetComponent<Animator>();
         anim2.SetTrigger("AlarmOn");
      }

      // sword attack
      if (Input.GetKeyDown(KeyCode.Space) && swordInHand.gameObject.activeSelf)
      {
         swordAnimator.SetTrigger("Attack");
         swordSound.PlayDelayed(0.25f);
         if (SceneManager.GetActiveScene().buildIndex == 0)
         {
            Paladin.DoDamage();
         }
         else if (SceneManager.GetActiveScene().buildIndex == 1)
         {
            Monster.DoDamage();
         }

      }
   }

   void pickingWeapon(GameObject weaponInCave, GameObject weaponInHand, Text pickWeaponText, KeyCode key, Animator weaponAnimator, AudioSource weaponSound)
   {
      RaycastHit hit; // saves where the sight hits
      if (Physics.Raycast(player_camera.transform.position, transform.forward, out hit))
      {
         float distance = hit.distance;
         float MaxDist = 15;
         if (hit.collider.gameObject == weaponInCave.gameObject && distance < MaxDist)
         {
            pickWeaponText.gameObject.SetActive(true);
         }
         else
         {
            pickWeaponText.gameObject.SetActive(false);
         }
      }

      if (Input.GetKeyDown(key) && pickWeaponText.IsActive())
      {
         weaponInCave.SetActive(false);
         weaponInHand.SetActive(true);
      }

      // weapon attack
      if (Input.GetKeyDown(key) && weaponInHand.gameObject.activeSelf)
      {
         weaponAnimator.SetTrigger("Attack");
         weaponSound.PlayDelayed(0.25f);
      }
   }

   void OpenTheChest(GameObject ChestOpen, GameObject ChestClose, GameObject theKey, Text OpenChestText, KeyCode key, AudioSource ChestSound)
   {
      RaycastHit hit; // saves where the sight hits
      if (Physics.Raycast(player_camera.transform.position, transform.forward, out hit))
      {
         float distance = hit.distance;
         float MaxDist = 15;
         if (hit.collider.gameObject == ChestClose.gameObject && distance < MaxDist)
         {
            OpenChestText.gameObject.SetActive(true);
         }
         else
         {
            OpenChestText.gameObject.SetActive(false);
         }
      }

      if (Input.GetKeyDown(key) && OpenChestText.IsActive() && theKey.activeInHierarchy == true)
      {
         ChestSound.Play();
         ChestClose.SetActive(false);
         ChestOpen.SetActive(true);
      }
   }

   void pickOrangeCrystal(GameObject OrangeCrystal, GameObject OrangeCrystalInHand, GameObject ChestOpen, Text pickCrystalText, KeyCode key, AudioSource crystalSound)
   {
      RaycastHit hit; // saves where the sight hits
      if (Physics.Raycast(player_camera.transform.position, transform.forward, out hit))
      {
         float distance = hit.distance;
         float MaxDist = 15;
         if (hit.collider.gameObject == OrangeCrystal.gameObject && distance < MaxDist && ChestOpen.activeInHierarchy == true)
         {
            pickCrystalText.gameObject.SetActive(true);
         }
         else
         {
            pickCrystalText.gameObject.SetActive(false);
         }
      }

      if (Input.GetKeyDown(key) && ChestOpen.activeInHierarchy == true)
      {
         crystalSound.Play();
         OrangeCrystal.SetActive(false);
         OrangeCrystalInHand.SetActive(true);
         keyInHand.SetActive(false);
      }
   }

   void pickCrystal(GameObject Crystal, GameObject CrystalInHand, Text pickCrystalText, KeyCode key, AudioSource crystalSound)
   {
      RaycastHit hit; // saves where the sight hits
      if (Physics.Raycast(player_camera.transform.position, transform.forward, out hit))
      {
         float distance = hit.distance;
         float MaxDist = 15;
         if (hit.collider.gameObject == Crystal.gameObject && distance < MaxDist)
         {
            pickCrystalText.gameObject.SetActive(true);
         }
         else
         {
            pickCrystalText.gameObject.SetActive(false);
         }
      }

      if (Input.GetKeyDown(key))
      {
         crystalSound.Play();
         Crystal.SetActive(false);
         CrystalInHand.SetActive(true);
      }
   }

}
