using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;

public sealed class AdRemovalPurchaseEditModeTests
{
    private Type _entitlement;
    private Component _service;
    private string _key;
    private bool _hadKey;
    private int _savedValue;

    [SetUp]
    public void SetUp()
    {
        _entitlement = CoreReflection.RequiredType("AdRemovalEntitlement");
        _key = (string)_entitlement.GetProperty("PreferenceKey").GetValue(null);
        _hadKey = PlayerPrefs.HasKey(_key);
        _savedValue = PlayerPrefs.GetInt(_key);
        PlayerPrefs.DeleteKey(_key);
        _service = new GameObject("AdRemovalPurchaseTest").AddComponent(CoreReflection.RequiredType("AdRemovalPurchaseService"));
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(_service.gameObject);
        if (_hadKey) PlayerPrefs.SetInt(_key, _savedValue); else PlayerPrefs.DeleteKey(_key);
        PlayerPrefs.Save();
    }

    [Test]
    public void PaidOrderGrantsAndPersistsBenefit_AndDuplicateDeliveryIsIdempotent()
    {
        int changes = 0;
        Action<bool> listener = owned => { if (owned) changes++; };
        EventInfo changed = _entitlement.GetEvent("Changed");
        changed.AddEventHandler(null, listener);
        try
        {
            var order = new PendingOrder(CartFor("remove_ads"), Info());
            Callback("OnPurchasePending", order);
            Callback("OnPurchasePending", order);
            Assert.That(Owned, Is.True);
            Assert.That(PlayerPrefs.GetInt(_key), Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(Property<bool>("CanPurchase"), Is.False);
        }
        finally { changed.RemoveEventHandler(null, listener); }
    }

    [Test]
    public void ConfirmedPurchaseRestoresAfterLocalCacheIsLost()
    {
        Callback("OnPurchasesFetched", new Orders(
            new[] { new ConfirmedOrder(CartFor("remove_ads"), Info()) },
            Array.Empty<PendingOrder>(), Array.Empty<DeferredOrder>()));
        Assert.That(Owned, Is.True);
        Assert.That(State, Is.EqualTo("Ready"));
    }

    [Test]
    public void UnacknowledgedPurchaseRestoresAfterAnInterruptedPurchase()
    {
        Callback("OnPurchasesFetched", new Orders(Array.Empty<ConfirmedOrder>(),
            new[] { new PendingOrder(CartFor("remove_ads"), Info()) }, Array.Empty<DeferredOrder>()));
        Assert.That(Owned, Is.True);
    }

    [Test]
    public void DeferredPaymentDoesNotGrantBenefit_AndLaterPaidOrderDoes()
    {
        Callback("OnPurchaseDeferred", new DeferredOrder(CartFor("remove_ads"), Info()));
        Assert.That(Owned, Is.False);
        Assert.That(State, Is.EqualTo("Deferred"));
        Assert.That(Property<bool>("CanPurchase"), Is.False);
        Callback("OnPurchasePending", new PendingOrder(CartFor("remove_ads"), Info()));
        Assert.That(Owned, Is.True);
        Assert.That(State, Is.EqualTo("Ready"));
    }

    [TestCase(PurchaseFailureReason.UserCancelled)]
    [TestCase(PurchaseFailureReason.PaymentDeclined)]
    public void CancelledOrFailedPurchaseDoesNotGrantBenefit(PurchaseFailureReason reason)
    {
        Callback("OnPurchaseFailed", new FailedOrder(CartFor("remove_ads"), reason, "test"));
        Assert.That(Owned, Is.False);
        Assert.That(State, Is.EqualTo("Ready"));
    }

    [Test]
    public void OtherProductOrConsumableCannotGrantAdRemoval()
    {
        Callback("OnPurchasePending", new PendingOrder(CartFor("different_product"), Info()));
        Callback("OnPurchasePending", new PendingOrder(CartFor("remove_ads", ProductType.Consumable), Info()));
        Assert.That(Owned, Is.False);
    }

    [Test]
    public void FailedRestoreAndEmptyResponseKeepPreviouslyPurchasedOfflineBenefit()
    {
        Callback("OnPurchasePending", new PendingOrder(CartFor("remove_ads"), Info()));
        Callback("OnPurchasesFetchFailed", new object[] { null });
        Assert.That(Owned, Is.True);
        Callback("OnPurchasesFetched", new Orders(Array.Empty<ConfirmedOrder>(),
            Array.Empty<PendingOrder>(), Array.Empty<DeferredOrder>()));
        Assert.That(Owned, Is.True);
    }

    [Test]
    public void PriceAndReadinessGatePurchase_AndPurchasingPreventsASecondTap()
    {
        Product product = ProductFor("remove_ads", ProductType.NonConsumable);
        typeof(Product).GetProperty("availableToPurchase").SetValue(product, true);
        _service.GetType().GetField("_product", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_service, product);
        SetState("Ready");
        Assert.That(Property<string>("LocalizedPrice"), Is.EqualTo("¥480"));
        Assert.That(Property<bool>("CanPurchase"), Is.True);
        SetState("Purchasing");
        Assert.That(Property<bool>("IsBusy"), Is.True);
        Assert.That(Property<bool>("CanPurchase"), Is.False);
        CoreReflection.Call(_service, "Purchase");
        Assert.That(Owned, Is.False);
    }

    [Test]
    public void EditorUsesSeparateEntitlementCacheFromProduction()
    {
        Assert.That(_key, Is.EqualTo("IAP_Editor_RemoveAds_v1"));
    }

    private bool Owned => (bool)_entitlement.GetProperty("IsOwned").GetValue(null);
    private string State => _service.GetType().GetProperty("State").GetValue(_service).ToString();
    private T Property<T>(string name) => (T)_service.GetType().GetProperty(name).GetValue(_service);
    private void Callback(string name, params object[] arguments)
        => _service.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_service, arguments);

    private void SetState(string state)
    {
        Type stateType = CoreReflection.RequiredType("AdRemovalStoreState");
        Callback("SetState", Enum.Parse(stateType, state), "test");
    }

    private static ICart CartFor(string id, ProductType type = ProductType.NonConsumable)
        => new Cart(new CartItem(ProductFor(id, type)));

    private static Product ProductFor(string id, ProductType type)
        => (Product)Activator.CreateInstance(typeof(Product), BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { new ProductDefinition(id, type), new ProductMetadata("¥480", "広告を削除", "", "JPY", 480m) }, null);

    private static IOrderInfo Info()
        => (IOrderInfo)Activator.CreateInstance(typeof(Order).Assembly.GetType("UnityEngine.Purchasing.OrderInfo"),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new object[] { "{}", "test-transaction", "GooglePlay" }, null);
}
