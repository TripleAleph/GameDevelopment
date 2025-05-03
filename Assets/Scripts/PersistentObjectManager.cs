using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PersistentObjectManager : MonoBehaviour
{
   // implement Singletone pattern
   public static PersistentObjectManager instance = null;
   public bool gameStarted = false;

   //store return point & orientation not only for the first scene.
   static Vector3 SpawnPointPosition;
   static Vector3 SpawnPointOrientation;
   public GameObject Player;

   // Coins:
   static int numGoldCoins = 0;
   public Text coinsText;

   // Sword:
   static bool isSwordInHand = false;
   public GameObject SwordInHand;
   public GameObject SwordInRock;

   // Axe:
   static bool isAxeInHand = false;
   public GameObject AxeInHand;
   public GameObject AxeInCave;

   // Wand:
   static bool isWandInHand = false;
   public GameObject WandInHand;
   public GameObject WandInCave;

   // Staff:
   static bool isStaffInHand = false;
   public GameObject StaffInHand;
   public GameObject StaffInCave;

   // Sheild:
   static bool isSheildInHand = false;
   public GameObject SheildInHand;
   public GameObject SheildInCave;

   // Bow:
   static bool isBowInHand = false;
   public GameObject BowInHand;
   public GameObject BowInCave;

   // Key:
   static bool isKeyInHand = false;
   public GameObject KeyInHand;
   public GameObject KeyInCave;

   // Orange Crystal:
   static bool isOrangeCrystalInHand = false;
   public GameObject OrangeCrystalInHand;
   public GameObject OrangeCrystal;

   // Purple Crystal:
   static bool isPurpleCrystalInHand = false;
   public GameObject PurpleCrystalInHand;
   public GameObject PurpleCrystal;

   // Green Crystal:
   static bool isGreenCrystalInHand = false;
   public GameObject GreenCrystalInHand;
   public GameObject GreenCrystal;

   // Blue Crystal:
   static bool isBlueCrystalInHand = false;
   public GameObject BlueCrystalInHand;
   public GameObject BlueCrystal;

   public void weaponActivate(bool isObjectInHand, GameObject WeaponInHand, GameObject WeaponInCave)
   {
      // the sword is one and unique therefore it can be either in hand or in rock
      if (isObjectInHand)
      {
         WeaponInHand.SetActive(true);
         WeaponInCave.SetActive(false);
      }
      else
      {
         WeaponInHand.SetActive(false);
         WeaponInCave.SetActive(true);
      }
      Destroy(gameObject);
   }

   private void Awake()
   {
      if(instance == null) // only for the first time
      {
         instance = this;
      }
      else // not for the first time
      {
         coinsText.text = "Gold: " + numGoldCoins;

         weaponActivate(isSwordInHand, SwordInHand, SwordInRock);
         weaponActivate(isAxeInHand, AxeInHand, AxeInCave);
         weaponActivate(isWandInHand, WandInHand, WandInCave);
         weaponActivate(isStaffInHand, StaffInHand, StaffInCave);
         weaponActivate(isSheildInHand, SheildInHand, SheildInCave);
         weaponActivate(isBowInHand, BowInHand, BowInCave);
         weaponActivate(isKeyInHand, KeyInHand, KeyInCave);
         weaponActivate(isOrangeCrystalInHand, OrangeCrystalInHand, OrangeCrystal);
         weaponActivate(isPurpleCrystalInHand, PurpleCrystalInHand, PurpleCrystal);
         weaponActivate(isGreenCrystalInHand, GreenCrystalInHand, GreenCrystal);
         weaponActivate(isBlueCrystalInHand, BlueCrystalInHand, BlueCrystal);

         if (SceneManager.GetActiveScene().buildIndex == 0)
         {
            Player.transform.position = SpawnPointPosition;
            Player.transform.rotation = Quaternion.Euler(SpawnPointOrientation);
         }
      }
      DontDestroyOnLoad(gameObject);
   }

   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   public void UpdateCoins(int amount)
   {
      numGoldCoins = amount;
   }
   public void SetIsBlueCrystalInHand(bool blueCrystalIsVisible)
   {
      isBlueCrystalInHand = blueCrystalIsVisible;
   }
   public void SetIsGreenCrystalInHand(bool greenCrystalIsVisible)
   {
      isGreenCrystalInHand = greenCrystalIsVisible;
   }
   public void SetIsPurpleCrystalInHand(bool purpleCrystalIsVisible)
   {
      isPurpleCrystalInHand = purpleCrystalIsVisible;
   }
   public void SetIsOrangeCrystalInHand(bool orangeCrystalIsVisible)
   {
      isOrangeCrystalInHand = orangeCrystalIsVisible;
   }
   public void SetIsSwordInHand(bool swordIsVisible)
   {
      isSwordInHand = swordIsVisible;
   }
   public void SetIsAxeInHand(bool axeIsVisible)
   {
      isAxeInHand = axeIsVisible;
   }
   public void SetIsWandInHand(bool wandIsVisible)
   {
      isWandInHand = wandIsVisible;
   }
   public void SetIsStaffInHand(bool staffIsVisible)
   {
      isStaffInHand = staffIsVisible;
   }
   public void SetIsSheildInHand(bool sheildIsVisible)
   {
      isSheildInHand = sheildIsVisible;
   }
   public void SetIsBowInHand(bool bowIsVisible)
   {
      isBowInHand = bowIsVisible;
   }
   public void SetIsKeyInHand(bool keyIsVisible)
   {
      isKeyInHand = keyIsVisible;
   }
   public void SetSpawnPointPositionAndOrientation(Vector3 position, Vector3 rotation)
   {
      SpawnPointPosition = position;
      SpawnPointOrientation = rotation;
   }
}
