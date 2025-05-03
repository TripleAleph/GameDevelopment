using UnityEngine;
using TMPro;

public class ReadText : MonoBehaviour
{
   public TMP_Text playerName;
   public TMP_Text displayPlayerName;
   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   public void ReadTheTextFromInput()
   {
      displayPlayerName.text = "Welcome to the Game, " + playerName.text + "!";
   }
}
