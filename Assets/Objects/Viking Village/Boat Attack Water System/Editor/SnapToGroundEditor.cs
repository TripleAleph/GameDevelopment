using UnityEngine;
using UnityEditor;

public class SnapToGroundEditor : EditorWindow
{
    [MenuItem("Tools/Snap To Ground")]
    public static void SnapToGround()
    {
        foreach (GameObject parent in Selection.gameObjects)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>())
            {
                RaycastHit hit;
                // ���� Raycast ���� ��� ������� (���)
                if (Physics.Raycast(child.position, Vector3.down, out hit, Mathf.Infinity))
                {
                    Undo.RecordObject(child.transform, "Snap To Ground"); // ����� Undo
                    child.position = hit.point; // ����� �� ������ ������ �� �����

                    // ����� �� ����� ���� Y ����
                    Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    child.rotation = Quaternion.Euler(targetRotation.eulerAngles.x, child.rotation.eulerAngles.y, targetRotation.eulerAngles.z);

                    Debug.Log($"{child.name} snapped and aligned to ground at {hit.point}");
                }
                else
                {
                    Debug.LogWarning($"No ground detected for {child.name}. Make sure there is a collider below.");
                }
            }
        }
    }
}
