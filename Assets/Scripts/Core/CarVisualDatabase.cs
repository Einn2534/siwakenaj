using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CarVisualDatabase", menuName = "Game/Car Visual Database")]
public class CarVisualDatabase : ScriptableObject
{
    private const string ResourcePath = "CarVisualDatabase";
    private static CarVisualDatabase _defaultDatabase;

    [Serializable]
    public struct VisualEntry
    {
        [SerializeField]
        private CarType _carType;

        [SerializeField] private string _displayName;

        [SerializeField]
        private Sprite _bodySprite;

        [SerializeField]
        private Sprite _iconSprite;

        public CarType CarType => _carType;
        public string DisplayName => _displayName;
        public Sprite BodySprite => _bodySprite;
        public Sprite IconSprite => _iconSprite != null ? _iconSprite : _bodySprite;
    }

    [SerializeField]
    private VisualEntry[] _entries;

    [SerializeField] private string _themeName = "くるま";
    [SerializeField] private string _noun = "車";
    [SerializeField] private string _countUnit = "台";
    [SerializeField] private string _brokenName = "故障車";
    [SerializeField] private string _repairAction = "整備へ送る";
    [SerializeField] private Sprite _coveredSprite;
    [SerializeField] private bool _isVehicleTheme = true;

    public string ThemeName => _themeName;
    public string Noun => _noun;
    public string CountUnit => _countUnit;
    public string BrokenName => _brokenName;
    public string RepairAction => _repairAction;
    public bool IsVehicleTheme => _isVehicleTheme;
    public string ExpressName => _isVehicleTheme ? "急送車" : "特急便";
    public string CoveredName => _isVehicleTheme ? "覆面車" : "ひみつ便";
    public Sprite CoveredSprite => _coveredSprite != null ? _coveredSprite : Resources.Load<Sprite>("Gameplay/covered_vehicle");
    public string MatchInstruction => $"来た{Noun}と同じボタンを押そう";

    // Resolve from the selected session every time, so Retry/Next/Daily cannot retain a previous theme.
    public static CarVisualDatabase LoadForStage(int stageNumber, GameMode mode = GameMode.Stage)
    {
        if (mode == GameMode.Daily) return LoadDefault();
        StageDatabase stages = Resources.Load<StageDatabase>("StageDatabase");
        StageDefinition stage = stages != null ? stages.GetStageDefinition(stageNumber) : null;
        return stage != null && stage.VisualTheme != null ? stage.VisualTheme : LoadDefault();
    }

    public static CarVisualDatabase LoadSelected() => LoadForStage(SessionState.SelectedStageNumber, SessionState.SelectedGameMode);

    public string GetDisplayName(CarType type)
    {
        if (TryGetEntry(type, out VisualEntry entry) && !string.IsNullOrEmpty(entry.DisplayName)) return entry.DisplayName;
        return type switch { CarType.LightTruck => "トラック", CarType.CompactCar => "普通車", _ => "スポーツカー" };
    }

    public static CarVisualDatabase LoadDefault()
    {
        if (_defaultDatabase == null)
        {
            _defaultDatabase = Resources.Load<CarVisualDatabase>(ResourcePath);
        }

        return _defaultDatabase;
    }

    public bool TryGetEntry(CarType carType, out VisualEntry entry)
    {
        if (_entries != null)
        {
            for (int i = 0; i < _entries.Length; i += 1)
            {
                if (_entries[i].CarType == carType)
                {
                    entry = _entries[i];
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }

    public Sprite GetBodySprite(CarType carType)
    {
        return TryGetEntry(carType, out VisualEntry entry) ? entry.BodySprite : null;
    }

    public Sprite GetIconSprite(CarType carType)
    {
        return TryGetEntry(carType, out VisualEntry entry) ? entry.IconSprite : null;
    }
}
