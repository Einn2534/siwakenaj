using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public sealed class MissBoundaryEditModeTests
{
    [Test]
    public void BoundaryUsesSixPercentAndWarningUsesFifteenPercent()
    {
        System.Type geometry = CoreReflection.RequiredType("MissBoundaryGeometry");
        Assert.That(CoreReflection.CallStatic(geometry, "GetBoundaryX", -6f, 100f), Is.EqualTo(0f).Within(.000001f));
        Assert.That(CoreReflection.CallStatic(geometry, "GetProximity", 15f, 0f, 100f), Is.EqualTo(0f).Within(.000001f));
        Assert.That(CoreReflection.CallStatic(geometry, "GetProximity", 7.5f, 0f, 100f), Is.EqualTo(.5f).Within(.0001f));
        Assert.That(CoreReflection.CallStatic(geometry, "GetProximity", 0f, 0f, 100f), Is.EqualTo(1f));
    }

    [Test]
    public void TargetLeftEdgeOnBoundaryIsSafeAndOnlyCrossingIsLate()
    {
        GameObject target = new("BoundaryTest", typeof(SpriteRenderer), typeof(BoxCollider2D));
        try
        {
            Component car = target.AddComponent(CoreReflection.RequiredType("CarController"));
            // A power-of-two width gives an exactly representable zero boundary.
            CoreReflection.Call(car, "Initialize", CoreReflection.EnumValue(CoreReflection.RequiredType("CarType"), "CompactCar"), 0f, -.96f, 16f);
            Assert.That(CoreReflection.GetProperty<float>(car, "MissBoundaryX"), Is.EqualTo(0f));
            BoxCollider2D collider = target.GetComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            MethodInfo isOut = car.GetType().GetMethod("IsOutOfPlayZone", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (float leftEdge in new[] { .01f, 0f, -.01f })
            {
                target.transform.position = new Vector3(.5f + leftEdge, 0f, 0f);
                Physics2D.SyncTransforms();
                Assert.That(isOut.Invoke(car, null), Is.EqualTo(leftEdge < 0f), $"leftEdge={leftEdge}");
            }
        }
        finally { Object.DestroyImmediate(target); }
    }
}
