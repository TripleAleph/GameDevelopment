using UnityEngine;
using TMPro;

public class GameInfo : MonoBehaviour
{
   public GameObject panelToToggle; // �������� ������� ������/�����

   public void TogglePanel()
   {
      // ����� �� �����: �� ���� -> �����, �� ���� -> ������
      panelToToggle.SetActive(!panelToToggle.activeSelf);
   }

   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
