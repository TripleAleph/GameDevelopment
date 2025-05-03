using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class NewMonoBehaviourScript : MonoBehaviour
{
   // Start is called once before the first execution of Update after the MonoBehaviour is created
   public GameObject fadeImage;
   public GameObject SpawnPoint;
   public GameObject SwordInHand;
   public GameObject AxeInHand;
   public GameObject WandInHand;
   public GameObject StaffInHand;
   public GameObject SheildInHand;
   public GameObject BowInHand;
   public GameObject KeyInHand;
   public GameObject OrangeCrystalInHand;
   public GameObject PurpleCrystalInHand;
   public GameObject GreenCrystalInHand;
   public GameObject BlueCrystalInHand;

   public GameObject mainMenuUI;   // Reference to the main menu UI (e.g., Play, Exit buttons)
   public GameObject gameplayUI;   // Reference to the gameplay UI (e.g., health bar, inventory)


   void Start()
    {
      // When the scene starts, check if the game was already started
      if (PersistentObjectManager.instance != null && PersistentObjectManager.instance.gameStarted)
      {
         ContinueGameFromSavedState();
      }
      else
      {
         ShowMainMenu();
      }
   }

   // Update is called once per frame
   void Update()
    {
        
    }

   private void OnTriggerEnter(Collider other)
   {
      // scene change
      // update gold coins
      PersistentObjectManager.instance.UpdateCoins(CoinBehaviour.numCoins);

      // setup SpawnPoint of return only if active scene is scene 0
      if (SceneManager.GetActiveScene().buildIndex == 0)
      {
         PersistentObjectManager.instance.SetSpawnPointPositionAndOrientation(SpawnPoint.transform.position, SpawnPoint.transform.rotation.eulerAngles);
      }
      else if (SceneManager.GetActiveScene().buildIndex == 1)
      {
         PersistentObjectManager.instance.SetIsSwordInHand(SwordInHand.activeSelf);
         PersistentObjectManager.instance.SetIsAxeInHand(AxeInHand.activeSelf);
         PersistentObjectManager.instance.SetIsWandInHand(WandInHand.activeSelf);
         PersistentObjectManager.instance.SetIsStaffInHand(StaffInHand.activeSelf);
         PersistentObjectManager.instance.SetIsSheildInHand(SheildInHand.activeSelf);
         PersistentObjectManager.instance.SetIsBowInHand(BowInHand.activeSelf);
         PersistentObjectManager.instance.SetIsKeyInHand(KeyInHand.activeSelf);
         PersistentObjectManager.instance.SetIsOrangeCrystalInHand(OrangeCrystalInHand.activeSelf);
         PersistentObjectManager.instance.SetIsPurpleCrystalInHand(PurpleCrystalInHand.activeSelf);
         PersistentObjectManager.instance.SetIsGreenCrystalInHand(GreenCrystalInHand.activeSelf);
         PersistentObjectManager.instance.SetIsBlueCrystalInHand(BlueCrystalInHand.activeSelf);
      }

      // activate fade in (before actual scene change)
      StartCoroutine(FadeInAndSceneChange());
   }

   IEnumerator FadeInAndSceneChange()
   {
      Animator animator = fadeImage.GetComponent<Animator>();
      animator.SetBool("FadeIn", true);
      yield return new WaitForSeconds(5);

      // only now scene can be changed
      if (SceneManager.GetActiveScene().buildIndex == 0)
      {
         // Set that the game has started
         PersistentObjectManager.instance.gameStarted = true;
         SceneManager.LoadScene(1); // go to gameplay scene
      }
      else if (SceneManager.GetActiveScene().buildIndex == 1)
         SceneManager.LoadScene(0);
   }

   void ContinueGameFromSavedState()
   {
      // Show gameplay UI (HUD)
      gameplayUI.SetActive(true);

      // Hide main menu UI
      mainMenuUI.SetActive(false);
   }

   void ShowMainMenu()
   {
      // Show main menu UI
      mainMenuUI.SetActive(true);

      // Hide gameplay UI
      gameplayUI.SetActive(false);
   }
}
