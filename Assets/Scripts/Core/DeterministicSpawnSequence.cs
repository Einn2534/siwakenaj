using System.Collections.Generic;
using UnityEngine;

public static class StableHash
{
    public static uint Fnv1a(string value)
    {
        const uint offset = 2166136261u;
        const uint prime = 16777619u;
        uint hash = offset;
        string safeValue = value ?? string.Empty;
        for (int i = 0; i < safeValue.Length; i += 1)
        {
            char character = safeValue[i];
            hash ^= (byte)(character & 0xff);
            hash *= prime;
            hash ^= (byte)(character >> 8);
            hash *= prime;
        }

        return hash;
    }
}

public sealed class DeterministicRandom
{
    private uint _state;

    public DeterministicRandom(uint seed)
    {
        _state = seed == 0u ? 0x6d2b79f5u : seed;
    }

    public uint NextUInt()
    {
        uint value = _state;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        _state = value;
        return value;
    }

    public int Range(int minimumInclusive, int maximumExclusive)
    {
        return maximumExclusive <= minimumInclusive
            ? minimumInclusive
            : minimumInclusive + (int)(NextUInt() % (uint)(maximumExclusive - minimumInclusive));
    }

    public float Value01()
    {
        return (NextUInt() & 0x00ffffffu) / 16777216f;
    }

    public float Range(float minimumInclusive, float maximumInclusive)
    {
        return Mathf.Lerp(minimumInclusive, maximumInclusive, Value01());
    }
}

public sealed class DeterministicSpawnSequence
{
    private readonly StageDefinition _stage;
    private readonly DeterministicRandom _random;
    private readonly List<CarType> _bag = new();
    private int _introIndex;
    private SpawnInstruction _pendingInstruction;

    public DeterministicSpawnSequence(StageDefinition stage, uint seed)
    {
        _stage = stage ?? StageDefinition.CreateFallback(1);
        _random = new DeterministicRandom(seed);
    }

    public SpawnInstruction Next()
    {
        SpawnInstruction instruction = Peek();
        Commit();
        return instruction;
    }

    public SpawnInstruction Peek()
    {
        _pendingInstruction ??= GenerateNext();
        return _pendingInstruction.Copy();
    }

    public void Commit()
    {
        _pendingInstruction = null;
    }

    private SpawnInstruction GenerateNext()
    {
        if (_stage.SpawnPattern == SpawnPattern.FixedIntroThenBag && _stage.IntroSpawns != null && _introIndex < _stage.IntroSpawns.Length)
        {
            SpawnInstruction instruction = _stage.IntroSpawns[_introIndex++];
            return instruction != null ? instruction.Copy() : new SpawnInstruction(SelectBagCarType(), SelectModifier());
        }

        CarType carType = _stage.SpawnPattern == SpawnPattern.WeightedRandom ? SelectWeightedCarType() : SelectBagCarType();
        return new SpawnInstruction(carType, SelectModifier());
    }

    private CarType SelectWeightedCarType()
    {
        int light = Mathf.Max(0, _stage.WeightLightTruck);
        int compact = Mathf.Max(0, _stage.WeightCompactCar);
        int sports = Mathf.Max(0, _stage.WeightSportsCar);
        int total = light + compact + sports;
        if (total <= 0)
        {
            return CarType.LightTruck;
        }

        int roll = _random.Range(0, total);
        return roll < light ? CarType.LightTruck : roll < light + compact ? CarType.CompactCar : CarType.SportsCar;
    }

    private CarType SelectBagCarType()
    {
        if (_bag.Count == 0)
        {
            RefillBag();
        }

        if (_bag.Count == 0)
        {
            return CarType.LightTruck;
        }

        int last = _bag.Count - 1;
        CarType result = _bag[last];
        _bag.RemoveAt(last);
        return result;
    }

    private void RefillBag()
    {
        int light = _stage.LightTruckQuota > 0 ? _stage.LightTruckQuota : Mathf.Max(0, _stage.WeightLightTruck);
        int compact = _stage.CompactCarQuota > 0 ? _stage.CompactCarQuota : Mathf.Max(0, _stage.WeightCompactCar);
        int sports = _stage.SportsCarQuota > 0 ? _stage.SportsCarQuota : Mathf.Max(0, _stage.WeightSportsCar);
        AddCopies(CarType.LightTruck, light);
        AddCopies(CarType.CompactCar, compact);
        AddCopies(CarType.SportsCar, sports);
        for (int i = _bag.Count - 1; i > 0; i -= 1)
        {
            int swapIndex = _random.Range(0, i + 1);
            CarType swap = _bag[i];
            _bag[i] = _bag[swapIndex];
            _bag[swapIndex] = swap;
        }
    }

    private void AddCopies(CarType carType, int count)
    {
        for (int i = 0; i < count; i += 1)
        {
            _bag.Add(carType);
        }
    }

    private CarModifier SelectModifier()
    {
        float broken = CarModifierRules.ClampChance(_stage.BrokenChance);
        float covered = CarModifierRules.ClampChance(_stage.CoveredChance);
        float express = CarModifierRules.ClampChance(_stage.ExpressChance);
        float roll = _random.Value01();
        if (roll < broken)
        {
            return CarModifier.Broken;
        }

        if (roll < broken + covered)
        {
            return CarModifier.Covered;
        }

        return roll < broken + covered + express ? CarModifier.Express : CarModifier.Normal;
    }
}
