using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public sealed class MissBoundaryEditModeTests
{
    [Test]
    public void BoundaryUsesSixPercentMissPointUsesSixtyPercentAndWarningUsesFifteenPercent()
    {
        System.Type geometry = CoreReflection.RequiredType("MissBoundaryGeometry");
        Assert.That(CoreReflection.CallStatic(geometry, "GetBoundaryX", -6f, 100f), Is.EqualTo(0f).Within(.000001f));
        Assert.That(CoreReflection.CallStatic(geometry, "GetMissPointX", -6f, 10f), Is.EqualTo(0f).Within(.000001f));
        Assert.That(CoreReflection.CallStatic(geometry, "GetProximity", 15f, 0f, 100f), Is.EqualTo(0f).Within(.000001f));
        Assert.That(CoreReflection.CallStatic(geometry, "GetProximity", 7.5f, 0f, 100f), Is.EqualTo(.5f).Within(.0001f));
        Assert.That(CoreReflection.CallStatic(geometry, "GetProximity", 0f, 0f, 100f), Is.EqualTo(1f));
    }

    [Test]
    public void TargetMissPointOnBoundaryIsSafeAndOnlyCrossingIsLate()
    {
        GameObject target = new("BoundaryTest", typeof(SpriteRenderer), typeof(BoxCollider2D));
        try
        {
            Component car = target.AddComponent(CoreReflection.RequiredType("CarController"));
            // A power-of-two width gives an exactly representable zero boundary.
            CoreReflection.Call(car, "Initialize", CoreReflection.EnumValue(CoreReflection.RequiredType("CarType"), "CompactCar"), 0f, -.96f, 16f);
            Assert.That(CoreReflection.GetProperty<float>(car, "MissBoundaryX"), Is.EqualTo(0f));
            BoxCollider2D collider = target.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(10f, 1f);
            MethodInfo isOut = car.GetType().GetMethod("IsOutOfPlayZone", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (float missPoint in new[] { .01f, 0f, -.01f })
            {
                target.transform.position = new Vector3(-1f + missPoint, 0f, 0f);
                Physics2D.SyncTransforms();
                Assert.That(CoreReflection.Call(car, "GetMissPointX"), Is.EqualTo(missPoint).Within(.000001f));
                Assert.That(isOut.Invoke(car, null), Is.EqualTo(missPoint < 0f), $"missPoint={missPoint}");
            }
        }
        finally { Object.DestroyImmediate(target); }
    }
}
