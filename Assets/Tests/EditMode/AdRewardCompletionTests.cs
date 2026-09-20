using System;
using NUnit.Framework;

public class AdRewardCompletionTests
{
    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(false, true, false)]
    public void Completion_RequiresEarnedRewardAndSuccessfulClose(bool earned, bool failed, bool expected)
    {
        int calls = 0;
        bool result = !expected;
        object completion = CoreReflection.New(CoreReflection.RequiredType("AdRewardCompletion"),
            (Action<bool>)(value => { calls++; result = value; }));
        if (earned) CoreReflection.Call(completion, "Earn");
        Assert.That(calls, Is.Zero, "Reward alone must not resume gameplay while the ad is open.");
        CoreReflection.Call(completion, failed ? "Fail" : "Close");
        CoreReflection.Call(completion, "Earn");
        CoreReflection.Call(completion, "Close");
        CoreReflection.Call(completion, "Fail");
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(result, Is.EqualTo(expected));
    }
}
