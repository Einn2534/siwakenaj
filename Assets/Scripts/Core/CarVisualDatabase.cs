using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CarVisualDatabase", menuName = "Game/Car Visual Database")]
public class CarVisualDatabase : ScriptableObject
{
    [Serializable]
    public struct VisualEntry
    {
        [SerializeField]
        private CarType _carType;

        [SerializeField]
        private Sprite _bodySprite;

        [SerializeField]
        private Sprite _iconSprite;

        public CarType CarType => _carType;
        public Sprite BodySprite => _bodySprite;
        public Sprite IconSprite => _iconSprite != null ? _iconSprite : _bodySprite;
    }

    [SerializeField]
    private VisualEntry[] _entries;

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
