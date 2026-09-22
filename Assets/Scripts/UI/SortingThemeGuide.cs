using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies the same theme data to the existing title and pause instruction cards.</summary>
public static class SortingThemeGuide
{
    public static void Apply(GameObject root, CarVisualDatabase theme)
    {
        if (root == null || theme == null) return;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            string parent = text.transform.parent != null ? text.transform.parent.name : "";
            if (text.name == "VehicleHeading" && !theme.IsVehicleTheme) text.text = $"1. {theme.Noun}と同じボタンを押す";
            if (text.name == "SpecialHeading") text.text = "3. 特殊な目印とボーナス";
            if (text.name == "Label" && TryGetRowType(parent, out CarType type)) text.text = theme.GetDisplayName(type);
            if (text.name == "Title" && parent == "BrokenRule") text.text = theme.BrokenName;
            if (text.name == "Detail" && parent == "BrokenRule") text.text = $"「{theme.RepairAction}」を押す";
            if (text.name == "Title" && parent == "CoveredRule") text.text = theme.CoveredName;
            if (text.name == "Title" && parent == "ExpressRule") text.text = theme.ExpressName;
            if (text.name == "Label" && parent == "MissRule" && !theme.IsVehicleTheme) text.text = "何もない時もMISS\n上限でしごと終了";
            if (text.name == "TipText") text.text = $"COMBOをつなぐとFEVERで得点×2\n大名行列（RUSH）は3{theme.CountUnit}連続!";
        }
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if ((image.name == "IncomingVehicle" || image.name == "TargetVehicle")
                && TryGetRowType(image.transform.parent.name, out CarType type))
            {
                image.sprite = theme.GetIconSprite(type);
                image.preserveAspect = true;
            }
        }
    }

    private static bool TryGetRowType(string name, out CarType type)
    {
        type = name == "TruckRow" ? CarType.LightTruck : name == "CompactCarRow" ? CarType.CompactCar : CarType.SportsCar;
        return name == "TruckRow" || name == "CompactCarRow" || name == "SportsCarRow";
    }
}
