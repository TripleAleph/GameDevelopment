using UnityEngine;
using UnityEngine.UI;
public class CoinBehaviour : MonoBehaviour
{
   public Text coinText;
   public static int numCoins = 0;
   public GameObject coins;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   private void OnTriggerEnter(Collider other)
   {
      gameObject.SetActive(false);
      numCoins++;
      coinText.text = "Gold: " + numCoins.ToString();
      AudioSource sound = coins.GetComponent<AudioSource>();
      sound.Play();
   }
}
