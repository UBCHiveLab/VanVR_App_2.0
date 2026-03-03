using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Populates a dropdown with color options and updates the drawing system's pen color.
public class PenColorUI : MonoBehaviour
{
    public Dropdown dropdown;                 // Unity UI Dropdown
    public HandDrawingSystem drawingSystem;

    [System.Serializable]
    public struct ColorOption
    {
        public string name;
        public Color color;
    }

    public List<ColorOption> colors = new List<ColorOption>()
    {
        new ColorOption { name = "Red",    color = Color.red },
        new ColorOption { name = "Blue",   color = Color.blue },
        new ColorOption { name = "Green",  color = Color.green },
        new ColorOption { name = "Yellow", color = Color.yellow }
    };

    void Start()
    {
        if (dropdown == null || drawingSystem == null) return;

        dropdown.ClearOptions();

        List<Dropdown.OptionData> options = new();
        foreach (var c in colors)
        {
            options.Add(new Dropdown.OptionData(c.name));
        }

        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(OnColorChanged);

        // Set default color
        OnColorChanged(dropdown.value);
    }

    void OnColorChanged(int index)
    {
        if (index < 0 || index >= colors.Count) return;
        drawingSystem.SetPenColor(colors[index].color);
    }
}