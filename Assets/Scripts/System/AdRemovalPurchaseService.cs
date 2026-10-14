using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

public enum AdRemovalStoreState
{
    Unavailable, Connecting, FetchingProducts, FetchingPurchases,
    Ready, Purchasing, Restoring, Deferred, Failed
}

/// <summary>Unity IAP 5 purchase and restore flow for the Google Play release.</summary>
public sealed class AdRemovalPurchaseService : MonoBehaviour
{
    private static AdRemovalPurchaseService _instance;
    private StoreController _store;
    private Product _product;
    private bool _connecting;
    private bool _connected;
    private bool _deferred;

    public static AdRemovalPurchaseService Instance { get { EnsureInstance(); return _instance; } }
    public AdRemovalStoreState State { get; private set; } = AdRemovalStoreState.Unavailable;
    public string Message { get; private set; } = "ストアに接続して購入情報を確認できます。";
    public string LocalizedPrice => _product?.metadata?.localizedPriceString ?? string.Empty;
    public bool IsBusy => _connecting || State == AdRemovalStoreState.FetchingProducts
        || State == AdRemovalStoreState.FetchingPurchases || State == AdRemovalStoreState.Purchasing
        || State == AdRemovalStoreState.Restoring;
    public bool CanPurchase => State == AdRemovalStoreState.Ready && !IsBusy && !_deferred
        && !AdRemovalEntitlement.IsOwned && _product != null && _product.availableToPurchase
        && !string.IsNullOrWhiteSpace(LocalizedPrice);
    public bool CanRestore => _connected && _product != null && !IsBusy;
    public event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (_instance != null) return;
        new GameObject(nameof(AdRemovalPurchaseService)).AddComponent<AdRemovalPurchaseService>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Initialize();
#elif UNITY_EDITOR
        SetState(AdRemovalStoreState.Unavailable, "テストストアに接続すると購入・復元を確認できます。実際の請求はありません。");
#else
        SetState(AdRemovalStoreState.Unavailable, "広告削除の購入はGoogle Play版で利用できます。");
#endif
    }

    public async void Initialize()
    {
#if !UNITY_ANDROID && !UNITY_EDITOR
        return;
#else
        if (IsBusy) return;
        if (_connected)
        {
            if (_product != null) FetchPurchases(false); else FetchProducts();
            return;
        }

        if (_store == null)
        {
            _store = UnityIAPServices.StoreController();
            _store.SetStoreReconnectionRetryPolicyOnDisconnection(null);
            // Handle each fetched pending order ourselves, exactly like a new purchase.
            _store.ProcessPendingOrdersOnPurchasesFetched(false);
            _store.OnStoreDisconnected += OnStoreDisconnected;
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;
        }

        _connecting = true;
        SetState(AdRemovalStoreState.Connecting, "ストアに接続しています…");
        try
        {
            await _store.Connect();
            if (this == null || State != AdRemovalStoreState.Connecting) return;
            _connected = true;
            _connecting = false;
            FetchProducts();
        }
        catch (Exception exception)
        {
            if (this != null) Fail("ストアに接続できませんでした。通信を確認して再接続してください。", exception);
        }
        finally
        {
            if (this != null) { _connecting = false; Changed?.Invoke(); }
        }
#endif
    }

    private void FetchProducts()
    {
        SetState(AdRemovalStoreState.FetchingProducts, "商品の価格を確認しています…");
        try
        {
            _store.FetchProductsWithNoRetries(new List<ProductDefinition>
            {
                new(AdRemovalEntitlement.ProductId, ProductType.NonConsumable)
            });
        }
        catch (Exception exception) { Fail("商品を取得できませんでした。再接続してください。", exception); }
    }

    private void OnProductsFetched(List<Product> products)
    {
        _product = products?.FirstOrDefault(product => product.definition.id == AdRemovalEntitlement.ProductId
            && product.definition.type == ProductType.NonConsumable);
        if (_product == null || !_product.availableToPurchase)
        {
            Fail("広告削除の商品がストアで見つかりませんでした。時間をおいて再接続してください。");
            return;
        }
        FetchPurchases(false);
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
        => Fail("商品を取得できませんでした。通信を確認して再接続してください。");

    public void Purchase()
    {
        if (!CanPurchase) return;
        SetState(AdRemovalStoreState.Purchasing, "購入手続きを進めています…");
        try { _store.PurchaseProduct(_product); }
        catch (Exception exception) { Fail("購入を開始できませんでした。再接続してお試しください。", exception); }
    }

    public void Restore()
    {
        if (!CanRestore) return;
        FetchPurchases(true);
    }

    private void FetchPurchases(bool restoring)
    {
        SetState(restoring ? AdRemovalStoreState.Restoring : AdRemovalStoreState.FetchingPurchases,
            restoring ? "購入を復元しています…" : "購入情報を確認しています…");
        try { _store.FetchPurchases(); }
        catch (Exception exception) { Fail("購入情報を確認できませんでした。再接続してください。", exception); }
    }

    private void OnPurchasesFetched(Orders orders)
    {
        bool restoring = State == AdRemovalStoreState.Restoring;
        bool owned = orders?.ConfirmedOrders?.Any(ContainsProduct) == true;
        _deferred = orders?.DeferredOrders?.Any(ContainsProduct) == true;
        if (owned) AdRemovalEntitlement.SetOwned(true);
        if (orders?.PendingOrders != null)
        {
            foreach (PendingOrder order in orders.PendingOrders)
            {
                if (!ContainsProduct(order)) continue;
                owned = true;
                OnPurchasePending(order);
            }
        }

        // IAP 5.0.3 can report an empty Google list when a query fails. Do not
        // erase a previously granted benefit on an empty list or network error.
        if (_deferred && !owned)
        {
            SetState(AdRemovalStoreState.Deferred, "購入は承認待ちです。承認後に反映されます。");
            return;
        }
        SetState(AdRemovalStoreState.Ready, AdRemovalEntitlement.IsOwned
            ? "購入済みです。すべての広告が非表示になり、復活も広告なしで利用できます。"
            : restoring ? "復元できる購入が見つかりません。同じGoogleアカウントでお試しください。"
            : "買い切りで、すべての広告を削除できます。");
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        => Fail(AdRemovalEntitlement.IsOwned
            ? "購入情報を確認できませんでした。購入済みの広告削除は引き続き有効です。"
            : "購入情報を確認できませんでした。通信を確認して再接続してください。");

    private void OnPurchasePending(PendingOrder order)
    {
        if (!ContainsProduct(order)) return;
        AdRemovalEntitlement.SetOwned(true);
        _deferred = false;
        SetState(AdRemovalStoreState.Ready, "購入が完了しました。すべての広告を削除しました。");
        try { _store?.ConfirmPurchase(order); }
        catch (Exception exception)
        {
            // The saved benefit remains active; the store can redeliver this order.
            Debug.LogWarning("Ad removal acknowledgement: " + exception.GetType().Name);
            SetState(AdRemovalStoreState.Ready, "広告削除は有効です。購入記録の同期は次回起動時に再試行します。");
        }
    }

    private void OnPurchaseConfirmed(Order order)
    {
        if (!ContainsProduct(order)) return;
        if (order is FailedOrder)
        {
            SetState(AdRemovalStoreState.Ready, "広告削除は有効です。購入記録の同期は次回起動時に再試行します。");
        }
        else if (order is ConfirmedOrder)
        {
            AdRemovalEntitlement.SetOwned(true);
            _deferred = false;
            SetState(AdRemovalStoreState.Ready, "購入済みです。すべての広告を削除しました。");
        }
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        if (!ContainsProduct(order)) return;
        if (order.FailureReason == PurchaseFailureReason.DuplicateTransaction)
        {
            FetchPurchases(true);
            return;
        }
        SetState(AdRemovalStoreState.Ready, order.FailureReason == PurchaseFailureReason.UserCancelled
            ? "購入をキャンセルしました。" : "購入を完了できませんでした。もう一度お試しください。");
    }

    private void OnPurchaseDeferred(DeferredOrder order)
    {
        if (!ContainsProduct(order)) return;
        _deferred = true;
        SetState(AdRemovalStoreState.Deferred, "購入は承認待ちです。承認後に反映されます。");
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        _connected = false;
        Fail("ストアとの接続が切れました。通信を確認して再接続してください。");
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && CanRestore && State != AdRemovalStoreState.Unavailable) FetchPurchases(false);
    }

    private static bool ContainsProduct(Order order)
        => order?.CartOrdered?.Items()?.Any(item => item?.Product?.definition?.id == AdRemovalEntitlement.ProductId
            && item.Product.definition.type == ProductType.NonConsumable) == true;

    private void Fail(string message, Exception exception = null)
    {
        if (exception != null) Debug.LogWarning("Ad removal store: " + exception.GetType().Name);
        SetState(AdRemovalStoreState.Failed, message);
    }

    private void SetState(AdRemovalStoreState state, string message)
    {
        State = state;
        Message = message;
        Changed?.Invoke();
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        if (_store != null)
        {
            _store.OnStoreDisconnected -= OnStoreDisconnected;
            _store.OnProductsFetched -= OnProductsFetched;
            _store.OnProductsFetchFailed -= OnProductsFetchFailed;
            _store.OnPurchasesFetched -= OnPurchasesFetched;
            _store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            _store.OnPurchasePending -= OnPurchasePending;
            _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _store.OnPurchaseFailed -= OnPurchaseFailed;
            _store.OnPurchaseDeferred -= OnPurchaseDeferred;
        }
        _instance = null;
    }
}
