using UnityEngine;

// Represents which hand is dominant for the experience.
public enum DominantHand { Left, Right }

// Manages the global dominant hand setting and persists it between sessions.
public class SetHandedness : MonoBehaviour
{
    public static DominantHand Dominant { get; private set; } = DominantHand.Right;

    public static HandSide DrawHand  => (Dominant == DominantHand.Left) ? HandSide.Left : HandSide.Right;
    public static HandSide MenuHand  => (Dominant == DominantHand.Left) ? HandSide.Right : HandSide.Left;

    // Optional: persist between runs
    const string Key = "DominantHand";

    void Awake()
    {
        Dominant = (DominantHand)PlayerPrefs.GetInt(Key, (int)DominantHand.Right);
    }

    public void SetLeftHanded()
    {
        SetDominant(DominantHand.Left);
    }

    public void SetRightHanded()
    {
        SetDominant(DominantHand.Right);
    }

    public void ToggleDominant()
    {
        SetDominant(Dominant == DominantHand.Left ? DominantHand.Right : DominantHand.Left);
    }

    static void SetDominant(DominantHand d)
    {
        Dominant = d;
        PlayerPrefs.SetInt(Key, (int)d);
        PlayerPrefs.Save();
    }
}
