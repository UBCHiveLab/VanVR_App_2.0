using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;

public class BlockHandMenuWhileDrawing : MonoBehaviour
{
    [SerializeField] HandMenu handMenu;
    [SerializeField] GameObject menuUI;

    void LateUpdate()
    {
        bool drawing = HandDrawingSystem.isDrawing;

        if (handMenu != null)
            handMenu.enabled = !drawing;

        if (menuUI != null && drawing)
            menuUI.SetActive(false);
    }
}
