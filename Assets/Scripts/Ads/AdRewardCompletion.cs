using System;

/// <summary>Only an SDK reward event earns a continue; closing an ad alone does not.</summary>
public sealed class AdRewardCompletion
{
    private Action<bool> _callback;
    private bool _earned;
    private bool _finished;
    public AdRewardCompletion(Action<bool> callback) => _callback = callback;
    public void Earn() { if (!_finished) _earned = true; }
    public void Close() => Finish(_earned);
    public void Fail() => Finish(false);
    private void Finish(bool earned)
    {
        if (_finished) return;
        _finished = true;
        var callback = _callback;
        _callback = null;
        callback?.Invoke(earned);
    }
}
