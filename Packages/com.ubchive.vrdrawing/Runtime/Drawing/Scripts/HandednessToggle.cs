using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;

public class HandednessToggle : MonoBehaviour
{
    [Header("Assign your HandMenu component")]
    [SerializeField] HandMenu handMenu;

    [Header("Default side on startup")]
    [SerializeField] HandMenu.MenuHandedness defaultSide = HandMenu.MenuHandedness.Left;

    void Awake()
    {
        if (handMenu == null)
            handMenu = FindFirstObjectByType<HandMenu>();

        // Force a sensible default
        if (handMenu != null)
            handMenu.menuHandedness = (defaultSide == HandMenu.MenuHandedness.Right)
                ? HandMenu.MenuHandedness.Right
                : HandMenu.MenuHandedness.Left;
    }

    // Hook this to your UI Button OnClick()
    public void SwitchHands()
    {
        if (handMenu == null) return;

        handMenu.menuHandedness =
            (handMenu.menuHandedness == HandMenu.MenuHandedness.Left)
            ? HandMenu.MenuHandedness.Right
            : HandMenu.MenuHandedness.Left;
    }
}