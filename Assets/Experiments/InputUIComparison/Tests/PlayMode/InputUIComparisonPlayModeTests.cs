using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Siwakenja.Experiments.InputUIComparison.Tests
{
    public sealed class InputUIComparisonPlayModeTests
    {
        private static readonly ExperimentInput[] AllInputs =
        {
            ExperimentInput.Sports,
            ExperimentInput.Compact,
            ExperimentInput.LightTruck,
            ExperimentInput.Repair,
        };

        private static readonly ExperimentInput[] VehicleInputs =
        {
            ExperimentInput.Sports,
            ExperimentInput.Compact,
            ExperimentInput.LightTruck,
        };

        private GameObject _root;
        private InputUIComparisonController _controller;

        [UnitySetUp]
        public IEnumerator BuildExperimentRuntime()
        {
            InputUIComparisonController[] existingControllers = UnityEngine.Object
                .FindObjectsByType<InputUIComparisonController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < existingControllers.Length; i++)
            {
                UnityEngine.Object.Destroy(existingControllers[i].gameObject);
            }

            EventSystem[] existingGeneratedSystems = UnityEngine.Object
                .FindObjectsByType<EventSystem>(FindObjectsSortMode.None)
                .Where(value => value.name == "InputUIComparisonEventSystem")
                .ToArray();
            for (int i = 0; i < existingGeneratedSystems.Length; i++)
            {
                UnityEngine.Object.Destroy(existingGeneratedSystems[i].gameObject);
            }

            yield return null;

            _root = new GameObject("InputUIComparisonTestRoot");
            _controller = _root.AddComponent<InputUIComparisonController>();
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator TearDownExperimentRuntime()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);

            EventSystem[] generatedSystems = UnityEngine.Object
                .FindObjectsByType<EventSystem>(FindObjectsSortMode.None)
                .Where(value => value.name == "InputUIComparisonEventSystem")
                .ToArray();
            for (int i = 0; i < generatedSystems.Length; i++)
            {
                UnityEngine.Object.Destroy(generatedSystems[i].gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RuntimeBootstrapsExactlyOneCanvasAndOneEventSystem()
        {
            Canvas[] canvases = _controller.GetComponentsInChildren<Canvas>(true);
            EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

            Assert.That(canvases, Has.Length.EqualTo(1));
            Assert.That(canvases[0].renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvases[0].GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);
            Assert.That(eventSystems, Has.Length.EqualTo(1));
            Assert.That(_controller.GetComponents<GlobalRawPointerCapture>(), Has.Length.EqualTo(1));
            Assert.That(_controller.RawCaptureForTests, Is.SameAs(GlobalRawPointerCapture.ActiveInstance));
            UnityEngine.UI.Text intro = _controller.GetComponentsInChildren<UnityEngine.UI.Text>(true)
                .Single(value => value.name == "Intro");
            UnityEngine.UI.Text title = _controller.GetComponentsInChildren<UnityEngine.UI.Text>(true)
                .Single(value => value.name == "Title");
            Assert.That(intro.rectTransform.rect.height, Is.GreaterThanOrEqualTo(intro.preferredHeight),
                "The staged-test warning must not be vertically truncated.");
            Assert.That(title.text, Does.Contain(InputUIExperimentSchema.CurrentExperimentVersion));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacementConditionsChangeOnlyRepairPosition()
        {
            ExperimentLayoutSpec upper = ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.UpperFixed);
            ExperimentLayoutSpec bottom = ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed);

            foreach (ExperimentInput input in VehicleInputs)
            {
                ExperimentControlSpec first = upper.GetControl(input);
                ExperimentControlSpec second = bottom.GetControl(input);
                Assert.That(first.RectTransform.ExactlyEquals(second.RectTransform), Is.True, input.ToString());
                Assert.That(first.Style.ExactlyEquals(second.Style), Is.True, input.ToString());
            }

            ExperimentControlSpec upperRepair = upper.GetControl(ExperimentInput.Repair);
            ExperimentControlSpec bottomRepair = bottom.GetControl(ExperimentInput.Repair);
            Assert.That(upperRepair.RectTransform.AnchorMin, Is.EqualTo(bottomRepair.RectTransform.AnchorMin));
            Assert.That(upperRepair.RectTransform.AnchorMax, Is.EqualTo(bottomRepair.RectTransform.AnchorMax));
            Assert.That(upperRepair.RectTransform.Pivot, Is.EqualTo(bottomRepair.RectTransform.Pivot));
            Assert.That(upperRepair.RectTransform.SizeDelta, Is.EqualTo(bottomRepair.RectTransform.SizeDelta));
            Assert.That(upperRepair.Style.ExactlyEquals(bottomRepair.Style), Is.True);
            Assert.That(upperRepair.RectTransform.AnchoredPosition, Is.EqualTo(new Vector2(540f, 800f)));
            Assert.That(bottomRepair.RectTransform.AnchoredPosition, Is.EqualTo(new Vector2(540f, 462f)));

            foreach (ExperimentSpecialState state in Enum.GetValues(typeof(ExperimentSpecialState)))
            {
                AssertPresentationEqual(upper.GetRepairPresentation(state), bottom.GetRepairPresentation(state));
            }

            BeginSynthetic(upper, ExperimentSpecialState.Normal, ExperimentVehicle.Sports, "PLACEMENT-UPPER");
            RectTransform upperRuntimeRepair = Control(ExperimentInput.Repair).GetComponent<RectTransform>();
            Vector2 upperRuntimeSize = upperRuntimeRepair.sizeDelta;
            Vector2 upperRuntimePosition = upperRuntimeRepair.anchoredPosition;

            BeginSynthetic(bottom, ExperimentSpecialState.Normal, ExperimentVehicle.Sports, "PLACEMENT-BOTTOM");
            RectTransform bottomRuntimeRepair = Control(ExperimentInput.Repair).GetComponent<RectTransform>();
            Assert.That(bottomRuntimeRepair, Is.SameAs(upperRuntimeRepair), "The runtime must reuse the same control hierarchy.");
            Assert.That(bottomRuntimeRepair.sizeDelta, Is.EqualTo(upperRuntimeSize));
            Assert.That(bottomRuntimeRepair.anchoredPosition, Is.Not.EqualTo(upperRuntimePosition));
            Assert.That(bottomRuntimeRepair.anchoredPosition, Is.EqualTo(new Vector2(540f, 462f)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StateAssistConditionsKeepAnchorAndOnlyChangeRepairPresentation()
        {
            const RepairAnchorMode anchor = RepairAnchorMode.UpperFixed;
            ExperimentLayoutSpec always = ExperimentLayoutCatalog.CreateStateAssist(RepairAssistMode.AlwaysActive, anchor);
            ExperimentLayoutSpec disabled = ExperimentLayoutCatalog.CreateStateAssist(RepairAssistMode.DisabledWhenNormal, anchor);
            ExperimentLayoutSpec brokenOnly = ExperimentLayoutCatalog.CreateStateAssist(RepairAssistMode.BrokenOnly, anchor);

            foreach (ExperimentInput input in AllInputs)
            {
                Assert.That(always.GetControl(input).RectTransform.ExactlyEquals(disabled.GetControl(input).RectTransform), Is.True, input.ToString());
                Assert.That(always.GetControl(input).RectTransform.ExactlyEquals(brokenOnly.GetControl(input).RectTransform), Is.True, input.ToString());
                Assert.That(always.GetControl(input).Style.ExactlyEquals(disabled.GetControl(input).Style), Is.True, input.ToString());
                Assert.That(always.GetControl(input).Style.ExactlyEquals(brokenOnly.GetControl(input).Style), Is.True, input.ToString());
            }

            BeginSynthetic(always, ExperimentSpecialState.Normal, ExperimentVehicle.Compact, "ASSIST-A");
            ExperimentPointerInput repair = Control(ExperimentInput.Repair);
            RectTransform repairRect = repair.GetComponent<RectTransform>();
            Vector2 anchoredPosition = repairRect.anchoredPosition;
            Vector2 sizeDelta = repairRect.sizeDelta;
            Assert.That(repair.IsVisible, Is.True);
            Assert.That(repair.IsInteractable, Is.True);
            Assert.That(repair.BlocksRaycasts, Is.True);

            BeginSynthetic(disabled, ExperimentSpecialState.Normal, ExperimentVehicle.Compact, "ASSIST-D");
            Assert.That(Control(ExperimentInput.Repair), Is.SameAs(repair));
            Assert.That(repairRect.anchoredPosition, Is.EqualTo(anchoredPosition));
            Assert.That(repairRect.sizeDelta, Is.EqualTo(sizeDelta));
            Assert.That(repair.gameObject.activeInHierarchy, Is.True);
            Assert.That(repair.IsVisible, Is.True);
            Assert.That(repair.IsInteractable, Is.False);
            Assert.That(repair.BlocksRaycasts, Is.True);

            BeginSynthetic(brokenOnly, ExperimentSpecialState.Normal, ExperimentVehicle.Compact, "ASSIST-H");
            Assert.That(Control(ExperimentInput.Repair), Is.SameAs(repair));
            Assert.That(repairRect.anchoredPosition, Is.EqualTo(anchoredPosition));
            Assert.That(repairRect.sizeDelta, Is.EqualTo(sizeDelta));
            Assert.That(repair.gameObject.activeInHierarchy, Is.True, "BrokenOnly must reserve the same RectTransform.");
            Assert.That(repair.IsVisible, Is.False);
            Assert.That(repair.IsInteractable, Is.False);
            Assert.That(repair.BlocksRaycasts, Is.False);

            BeginSynthetic(brokenOnly, ExperimentSpecialState.Broken, ExperimentVehicle.Compact, "ASSIST-BROKEN");
            Assert.That(repairRect.anchoredPosition, Is.EqualTo(anchoredPosition));
            Assert.That(repair.IsVisible, Is.True);
            Assert.That(repair.IsInteractable, Is.True);
            Assert.That(repair.BlocksRaycasts, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GlobalRawCaptureWritesOneRowAndButtonCallbackDoesNotDuplicateIt()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "RAW-TARGET");
            ExperimentPointerInput sports = Control(ExperimentInput.Sports);
            Vector2 position = sports.ScreenCenter();
            int before = _controller.RawEventsForTests.Count;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                pointerId = 41,
                position = position,
            };
            ExecuteEvents.Execute(sports.gameObject, pointerData, ExecuteEvents.pointerDownHandler);
            Assert.That(_controller.RawEventsForTests.Count, Is.EqualTo(before), "uGUI callback must never write a raw row.");

            _controller.RawCaptureForTests.InjectPointerDownForTest(41, position, 1000d, 501);
            Assert.That(_controller.RawEventsForTests.Count, Is.EqualTo(before + 1));
            _controller.FlushInputFrameForTests();
            ExecuteEvents.Execute(sports.gameObject, pointerData, ExecuteEvents.pointerUpHandler);

            ExperimentInputEventRecord row = _controller.RawEventsForTests.Single();
            Assert.That(row.EventIndex, Is.EqualTo(1));
            Assert.That(row.Input, Is.EqualTo(ExperimentInput.Sports));
            Assert.That(row.WasInputAccepted, Is.True);
            Assert.That(row.AcceptedInput, Is.EqualTo(ExperimentInput.Sports));
            yield return null;
        }

        [Test]
        public void PureArbitrator_FinalRejectedPhysicalInputNeverFallsBackToEarlierValidInput()
        {
            InputArbitrationContext context = ArbitrationContext();
            InputArbitrationCandidate valid = ArbitrationCandidate(
                1,
                ExperimentInput.Sports,
                "sports",
                true,
                true,
                1000d);
            InputArbitrationCandidate disabled = ArbitrationCandidate(
                2,
                ExperimentInput.Repair,
                "repair",
                true,
                false,
                1001d);
            InputArbitrationFrameResult disabledLast = InputArbitrator.ArbitrateFrame(
                new[] { valid, disabled },
                context);

            Assert.That(disabledLast.HasAcceptedInput, Is.False);
            Assert.That(disabledLast.Results[0].IgnoredReason,
                Is.EqualTo(ExperimentIgnoredReasons.SameFrameOverwritten));
            Assert.That(disabledLast.Results[1].IgnoredReason,
                Is.EqualTo(ExperimentIgnoredReasons.Disabled));

            InputArbitrationCandidate empty = ArbitrationCandidate(
                3,
                ExperimentInput.None,
                string.Empty,
                false,
                false,
                1002d);
            InputArbitrationFrameResult emptyLast = InputArbitrator.ArbitrateFrame(
                new[] { valid, empty },
                context);

            Assert.That(emptyLast.HasAcceptedInput, Is.False);
            Assert.That(emptyLast.Results[0].IgnoredReason,
                Is.EqualTo(ExperimentIgnoredReasons.SameFrameOverwritten));
            Assert.That(emptyLast.Results[1].IgnoredReason,
                Is.EqualTo(ExperimentIgnoredReasons.HiddenOrEmpty));
        }

        [Test]
        public void PureArbitrator_InvalidThenValidAcceptsOnlyFinalPhysicalInput()
        {
            InputArbitrationCandidate disabled = ArbitrationCandidate(
                1,
                ExperimentInput.Repair,
                "repair",
                true,
                false,
                1000d);
            InputArbitrationCandidate valid = ArbitrationCandidate(
                2,
                ExperimentInput.Sports,
                "sports",
                true,
                true,
                1001d);

            InputArbitrationFrameResult result = InputArbitrator.ArbitrateFrame(
                new[] { disabled, valid },
                ArbitrationContext());

            Assert.That(result.HasAcceptedInput, Is.True);
            Assert.That(result.AcceptedEventSequence, Is.EqualTo(2L));
            Assert.That(result.Results[0].IgnoredReason,
                Is.EqualTo(ExperimentIgnoredReasons.SameFrameOverwritten));
            Assert.That(result.Results[1].WasInputAccepted, Is.True);
            Assert.That(result.Results[1].AcceptedInput, Is.EqualTo(ExperimentInput.Sports));
        }

        [UnityTest]
        public IEnumerator NativeTouchSuppressesSyntheticMouseButNeverSuppressesIndependentTouches()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "NATIVE-DEDUPE");
            Vector2 sports = Control(ExperimentInput.Sports).ScreenCenter();

            PhysicalPointerDown touch = _controller.RawCaptureForTests.InjectNativeLikePointerDownForTest(
                1,
                101,
                PhysicalPointerKind.Touch,
                sports,
                1000d,
                505);
            PhysicalPointerDown syntheticMouse = _controller.RawCaptureForTests.InjectNativeLikePointerDownForTest(
                GlobalRawPointerCapture.MousePointerId,
                102,
                PhysicalPointerKind.Mouse,
                sports + Vector2.one,
                1034d,
                506);

            Assert.That(touch.Sequence, Is.GreaterThan(0L));
            Assert.That(syntheticMouse.Sequence, Is.EqualTo(0L),
                "The synthesized mouse copy must be suppressed even when its callback lands in the next frame.");
            Assert.That(_controller.RawEventsForTests, Has.Count.EqualTo(1));

            PhysicalPointerDown laterMouse = _controller.RawCaptureForTests.InjectNativeLikePointerDownForTest(
                GlobalRawPointerCapture.MousePointerId,
                102,
                PhysicalPointerKind.Mouse,
                sports + Vector2.one,
                1043d,
                506);
            Assert.That(laterMouse.Sequence, Is.GreaterThan(0L),
                "A mouse outside the conservative one-frame window must remain an independent Raw input.");
            Assert.That(_controller.RawEventsForTests, Has.Count.EqualTo(2));

            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "MULTI-TOUCH");
            _controller.RawCaptureForTests.InjectNativeLikePointerDownForTest(
                2,
                101,
                PhysicalPointerKind.Touch,
                sports,
                1100d,
                506);
            _controller.RawCaptureForTests.InjectNativeLikePointerDownForTest(
                3,
                101,
                PhysicalPointerKind.Touch,
                sports,
                1101d,
                506);

            Assert.That(_controller.RawEventsForTests, Has.Count.EqualTo(2),
                "Two physical touches must remain two Raw rows even at the same position/frame.");
            _controller.FlushInputFrameForTests();
            Assert.That(_controller.RawEventsForTests[0].IgnoredReason,
                Is.EqualTo(ExperimentIgnoredReasons.SameFrameOverwritten));
            Assert.That(_controller.RawEventsForTests[1].WasInputAccepted, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledRepairTapIsLoggedAndRejected()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreateStateAssist(RepairAssistMode.DisabledWhenNormal, RepairAnchorMode.UpperFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.LightTruck,
                "DISABLED-TARGET");

            ExperimentInputEventRecord row = TapAndFlush(ExperimentInput.Repair, 1000d, 510);
            Assert.That(_controller.RawEventsForTests, Has.Count.EqualTo(1));
            Assert.That(row.HitButtonId, Is.EqualTo("repair"));
            Assert.That(row.WasButtonVisible, Is.True);
            Assert.That(row.WasButtonInteractable, Is.False);
            Assert.That(row.WasInputAccepted, Is.False);
            Assert.That(row.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.Disabled));
            Assert.That(_controller.TrialRecordsForTests, Is.Empty);

            ExperimentInputEventRecord correct = TapAndFlush(ExperimentInput.LightTruck, 1200d, 511);
            Assert.That(correct.WasInputAccepted, Is.True);
            ExperimentTrialRecord record = _controller.TrialRecordsForTests.Single();
            Assert.That(record.ActualInput, Is.EqualTo(ExperimentInput.LightTruck),
                "The first accepted input is correct even though the first Raw touch was an error.");
            Assert.That(record.IsCorrect, Is.True);
            Assert.That(record.InvalidButtonInputCount, Is.EqualTo(1));
            Assert.That(record.TimeToFirstRawTouchMs, Is.GreaterThanOrEqualTo(0d));
            Assert.That(record.TimeToFirstAcceptedInputMs, Is.GreaterThanOrEqualTo(record.TimeToFirstRawTouchMs));
            Assert.That(record.CorrectionCostMs, Is.GreaterThanOrEqualTo(0d));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HiddenRepairAreaTapIsLoggedAsHiddenOrEmpty()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreateStateAssist(RepairAssistMode.BrokenOnly, RepairAnchorMode.UpperFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.LightTruck,
                "HIDDEN-TARGET");

            ExperimentInputEventRecord row = TapAndFlush(ExperimentInput.Repair, 1000d, 511);
            Assert.That(_controller.RawEventsForTests, Has.Count.EqualTo(1));
            Assert.That(row.Input, Is.EqualTo(ExperimentInput.Repair), "Geometry is retained even while visually hidden.");
            Assert.That(row.HitButtonId, Is.EqualTo("repair"));
            Assert.That(row.WasButtonVisible, Is.False);
            Assert.That(row.WasButtonInteractable, Is.False);
            Assert.That(row.IsInsideCurrentRepairRect, Is.True);
            Assert.That(row.IsInsidePreviousRepairRect, Is.False);
            Assert.That(row.WasInputAccepted, Is.False);
            Assert.That(row.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.HiddenOrEmpty));

            ExperimentInputEventRecord correct = TapAndFlush(ExperimentInput.LightTruck, 1200d, 512);
            Assert.That(correct.WasInputAccepted, Is.True);
            ExperimentTrialRecord record = _controller.TrialRecordsForTests.Single();
            Assert.That(record.ActualInput, Is.EqualTo(ExperimentInput.LightTruck));
            Assert.That(record.IsCorrect, Is.True);
            Assert.That(record.TimeToFirstRawTouchMs, Is.GreaterThanOrEqualTo(0d));
            Assert.That(record.CorrectionCostMs, Is.GreaterThanOrEqualTo(0d));
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptyAreaTapIsLoggedAsHiddenOrEmpty()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.LightTruck,
                "EMPTY-TARGET");

            Vector2 emptyPoint = FindEmptyControlPoint();
            _controller.RawCaptureForTests.InjectPointerDownForTest(52, emptyPoint, 1000d, 512);
            _controller.FlushInputFrameForTests();

            ExperimentInputEventRecord row = _controller.RawEventsForTests.Single();
            Assert.That(row.Input, Is.EqualTo(ExperimentInput.None));
            Assert.That(row.HitButtonId, Is.Empty);
            Assert.That(row.WasButtonVisible, Is.False);
            Assert.That(row.WasButtonInteractable, Is.False);
            Assert.That(row.IsInsideCurrentRepairRect, Is.False,
                "Generic empty area must remain distinguishable from the hidden repair Rect.");
            Assert.That(row.WasInputAccepted, Is.False);
            Assert.That(row.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.HiddenOrEmpty));
            yield return null;
        }

        [UnityTest]
        public IEnumerator NonControlVehicleTargetAndHudTapsAreNotGenericBlankArea()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.LightTruck,
                "SEMANTIC-NONCONTROL");

            RectTransform car = RectTransformNamed("CarCard1");
            RectTransform target = RectTransformNamed("TargetMarker");
            RectTransform hud = RectTransformNamed("BlockLabel");
            Vector2 targetTop = RectLocalPointToScreen(target, new Vector2(0f, target.rect.yMax - 4f));

            ExperimentInputEventRecord carTap = TapPointAndFlush(RectCenterToScreen(car), 1000d, 514);
            ExperimentInputEventRecord targetTap = TapPointAndFlush(targetTop, 1100d, 515);
            ExperimentInputEventRecord hudTap = TapPointAndFlush(RectCenterToScreen(hud), 1200d, 516);

            Assert.That(carTap.HitUiElement, Is.EqualTo("CarCard1"));
            Assert.That(targetTap.HitUiElement, Is.EqualTo("TargetMarker"));
            Assert.That(hudTap.HitUiElement, Is.EqualTo("BlockLabel"));
            Assert.That(new[] { carTap, targetTap, hudTap }
                .All(row => row.IgnoredReason == ExperimentIgnoredReasons.HiddenOrEmpty), Is.True);

            ExperimentInputEventRecord correct = TapAndFlush(ExperimentInput.LightTruck, 1400d, 517);
            Assert.That(correct.WasInputAccepted, Is.True);
            List<ExperimentParticipantConditionMetric> metrics =
                InputUIExperimentAnalytics.SummarizeParticipantConditions(
                    _controller.TrialRecordsForTests,
                    _controller.RawEventsForTests,
                    Array.Empty<ExperimentSurveyRecord>(),
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    ExperimentType.Placement);
            Assert.That(metrics.Single(row =>
                    row.Metric == ExperimentMetricIds.GenericEmptyAreaTapRatePerTrial).Value,
                Is.EqualTo(0d));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PreviousFixedRepairAreaRemainsIdentifiableAfterAnchorChange()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.UpperFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.LightTruck,
                "STALE-AREA-UPPER");
            Vector2 previousUpperRepairCenter = Control(ExperimentInput.Repair).ScreenCenter();

            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.LightTruck,
                "STALE-AREA-BOTTOM");
            _controller.RawCaptureForTests.InjectPointerDownForTest(
                53,
                previousUpperRepairCenter,
                1000d,
                513);
            _controller.FlushInputFrameForTests();

            ExperimentInputEventRecord row = _controller.RawEventsForTests.Single();
            Assert.That(row.Input, Is.EqualTo(ExperimentInput.None));
            Assert.That(row.IsInsideCurrentRepairRect, Is.False);
            Assert.That(row.IsInsidePreviousRepairRect, Is.True,
                "The old fixed repair location must be available for stale-area trial metrics.");
            Assert.That(row.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.HiddenOrEmpty));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CooldownTapIsLoggedAndRejected()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "COOLDOWN-TARGET");

            ExperimentInputEventRecord first = TapAndFlush(ExperimentInput.Compact, 1000d, 520);
            ExperimentInputEventRecord second = TapAndFlush(ExperimentInput.Sports, 1050d, 521);

            Assert.That(first.WasInputAccepted, Is.True);
            Assert.That(first.AcceptedInput, Is.EqualTo(ExperimentInput.Compact));
            Assert.That(second.WasInputAccepted, Is.False);
            Assert.That(second.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.Cooldown));
            Assert.That(_controller.RawEventsForTests, Has.Count.EqualTo(2));
            Assert.That(_controller.TrialRecordsForTests, Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SameFrameUsesFinalPhysicalInputAndMarksEarlierTapOverwritten()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "SAME-FRAME-TARGET");

            _controller.RawCaptureForTests.InjectPointerDownForTest(61, Control(ExperimentInput.Compact).ScreenCenter(), 1000d, 530);
            _controller.RawCaptureForTests.InjectPointerDownForTest(62, Control(ExperimentInput.Sports).ScreenCenter(), 1001d, 530);
            Assert.That(_controller.RawEventsForTests, Has.Count.EqualTo(2));
            _controller.FlushInputFrameForTests();

            ExperimentInputEventRecord first = _controller.RawEventsForTests[0];
            ExperimentInputEventRecord second = _controller.RawEventsForTests[1];
            Assert.That(first.WasInputAccepted, Is.False);
            Assert.That(first.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.SameFrameOverwritten));
            Assert.That(second.WasInputAccepted, Is.True);
            Assert.That(second.AcceptedInput, Is.EqualTo(ExperimentInput.Sports));
            Assert.That(_controller.TrialRecordsForTests, Has.Count.EqualTo(1));
            Assert.That(_controller.TrialRecordsForTests[0].SameFrameCoalescedCount, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SameFrameUsesFinalPhysicalInputEvenWhenItIsRejected()
        {
            ExperimentLayoutSpec layout = ExperimentLayoutCatalog.CreateStateAssist(
                RepairAssistMode.DisabledWhenNormal,
                RepairAnchorMode.UpperFixed);
            BeginSynthetic(layout, ExperimentSpecialState.Normal, ExperimentVehicle.Sports, "LAST-PHYSICAL-A");

            _controller.RawCaptureForTests.InjectPointerDownForTest(
                63,
                Control(ExperimentInput.Sports).ScreenCenter(),
                1000d,
                532);
            _controller.RawCaptureForTests.InjectPointerDownForTest(
                64,
                Control(ExperimentInput.Repair).ScreenCenter(),
                1001d,
                532);
            _controller.FlushInputFrameForTests();

            Assert.That(_controller.RawEventsForTests[0].IgnoredReason,
                Is.EqualTo(ExperimentIgnoredReasons.SameFrameOverwritten));
            Assert.That(_controller.RawEventsForTests[0].WasInputAccepted, Is.False);
            Assert.That(_controller.RawEventsForTests[1].IgnoredReason,
                Is.EqualTo(ExperimentIgnoredReasons.Disabled));
            Assert.That(_controller.RawEventsForTests[1].WasInputAccepted, Is.False);
            Assert.That(_controller.TrialRecordsForTests, Is.Empty);

            BeginSynthetic(layout, ExperimentSpecialState.Normal, ExperimentVehicle.Sports, "LAST-PHYSICAL-B");
            _controller.RawCaptureForTests.InjectPointerDownForTest(
                65,
                Control(ExperimentInput.Repair).ScreenCenter(),
                1200d,
                533);
            _controller.RawCaptureForTests.InjectPointerDownForTest(
                66,
                Control(ExperimentInput.Sports).ScreenCenter(),
                1201d,
                533);
            _controller.FlushInputFrameForTests();

            Assert.That(_controller.RawEventsForTests[0].IgnoredReason,
                Is.EqualTo(ExperimentIgnoredReasons.SameFrameOverwritten));
            Assert.That(_controller.RawEventsForTests[1].WasInputAccepted, Is.True);
            Assert.That(_controller.TrialRecordsForTests, Has.Count.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PendingInputsFromDifferentFramesAreArbitratedSeparately()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "FRAME-BOUNDARY");

            _controller.RawCaptureForTests.InjectPointerDownForTest(
                67,
                Control(ExperimentInput.Compact).ScreenCenter(),
                1000d,
                534);
            _controller.RawCaptureForTests.InjectPointerDownForTest(
                68,
                Control(ExperimentInput.Sports).ScreenCenter(),
                1200d,
                535);
            _controller.FlushInputFrameForTests();

            Assert.That(_controller.RawEventsForTests, Has.Count.EqualTo(2));
            Assert.That(_controller.RawEventsForTests.All(row => row.WasInputAccepted), Is.True);
            Assert.That(_controller.RawEventsForTests.Any(row =>
                row.IgnoredReason == ExperimentIgnoredReasons.SameFrameOverwritten), Is.False);
            ExperimentTrialRecord record = _controller.TrialRecordsForTests.Single();
            Assert.That(record.InputCount, Is.EqualTo(2));
            Assert.That(record.ActualInput, Is.EqualTo(ExperimentInput.Compact));
            Assert.That(record.IsCorrect, Is.False);
            Assert.That(record.EventualCorrect, Is.True);
            Assert.That(record.SameFrameCoalescedCount, Is.EqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoveredReactionClockStartsAtRevealAndPrematureTouchStaysDiagnostic()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Covered,
                ExperimentVehicle.LightTruck,
                "COVERED-TARGET");

            ExperimentInputEventRecord premature = TapAndFlush(ExperimentInput.LightTruck, 1000d, 540);
            Assert.That(premature.WasInputAccepted, Is.True, "Production-compatible Covered input is accepted before reveal.");
            Assert.That(premature.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.None));
            Assert.That(premature.ActionableTimeMs, Is.LessThan(0d));
            Assert.That(_controller.TrialRecordsForTests, Is.Empty);

            _controller.SetCoveredRevealedForTests(true);
            ExperimentInputEventRecord afterReveal = TapAndFlush(ExperimentInput.LightTruck, 1200d, 541);
            Assert.That(afterReveal.WasInputAccepted, Is.True);
            Assert.That(afterReveal.ActionableTimeMs, Is.GreaterThanOrEqualTo(0d));
            Assert.That(_controller.TrialRecordsForTests, Has.Count.EqualTo(1));
            ExperimentTrialRecord record = _controller.TrialRecordsForTests[0];
            Assert.That(record.SpecialState, Is.EqualTo(ExperimentSpecialState.Covered));
            Assert.That(record.ActualInput, Is.EqualTo(ExperimentInput.LightTruck));
            Assert.That(record.IsCorrect, Is.False, "The first accepted input remains the primary outcome.");
            Assert.That(record.EventualCorrect, Is.True);
            Assert.That(record.SpecialStateMissed, Is.True);
            Assert.That(record.InformationAvailableAtMs, Is.GreaterThanOrEqualTo(0d));
            Assert.That(record.TimeToFirstRawTouchMs, Is.GreaterThanOrEqualTo(0d));
            Assert.That(record.TimeToFirstAcceptedInputMs, Is.GreaterThanOrEqualTo(0d));
            Assert.That(record.TimeToCorrectResolutionMs, Is.GreaterThanOrEqualTo(0d));
            Assert.That(record.CorrectionCostMs, Is.EqualTo(
                record.TimeToCorrectResolutionMs - record.TimeToFirstRawTouchMs).Within(0.001d));
            Assert.That(record.TimeToFirstRawTouchMs, Is.LessThanOrEqualTo(record.TimeToCorrectResolutionMs));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoveredTouchCapturedBeforeRevealStaysPrematureWhenFlushedAfterReveal()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Covered,
                ExperimentVehicle.LightTruck,
                "COVERED-BOUNDARY");

            _controller.RawCaptureForTests.InjectPointerDownForTest(
                69,
                Control(ExperimentInput.LightTruck).ScreenCenter(),
                1000d,
                536);
            ExperimentInputEventRecord boundary = _controller.RawEventsForTests.Single();
            Assert.That(boundary.ActionableTimeMs, Is.LessThan(0d));

            _controller.SetCoveredRevealedForTests(true);
            _controller.FlushInputFrameForTests();

            Assert.That(boundary.WasInputAccepted, Is.True,
                "Premature production input remains an accepted diagnostic input.");
            Assert.That(_controller.TrialRecordsForTests, Is.Empty,
                "A down captured before reveal must not become correct merely because reveal preceded LateUpdate.");

            ExperimentInputEventRecord afterReveal = TapAndFlush(ExperimentInput.LightTruck, 1200d, 537);
            Assert.That(afterReveal.ActionableTimeMs, Is.GreaterThanOrEqualTo(0d));
            ExperimentTrialRecord record = _controller.TrialRecordsForTests.Single();
            Assert.That(record.InputCount, Is.EqualTo(2));
            Assert.That(record.IsCorrect, Is.False);
            Assert.That(record.EventualCorrect, Is.True);
            Assert.That(record.TimeToFirstRawTouchMs, Is.GreaterThanOrEqualTo(0d));
            Assert.That(record.TimeToFirstAcceptedInputMs, Is.GreaterThanOrEqualTo(0d));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BrokenVehicleButtonIsAcceptedAsWrongThenRepairResolves()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.UpperFixed),
                ExperimentSpecialState.Broken,
                ExperimentVehicle.LightTruck,
                "BROKEN-TARGET");

            ExperimentInputEventRecord vehicleTap = TapAndFlush(ExperimentInput.LightTruck, 1000d, 550);
            Assert.That(vehicleTap.WasInputAccepted, Is.True);
            Assert.That(vehicleTap.AcceptedInput, Is.EqualTo(ExperimentInput.LightTruck));
            Assert.That(_controller.TrialRecordsForTests, Is.Empty);

            ExperimentInputEventRecord repairTap = TapAndFlush(ExperimentInput.Repair, 1200d, 551);
            Assert.That(repairTap.WasInputAccepted, Is.True);
            Assert.That(_controller.TrialRecordsForTests, Has.Count.EqualTo(1));
            ExperimentTrialRecord record = _controller.TrialRecordsForTests[0];
            Assert.That(record.ExpectedInput, Is.EqualTo(ExperimentInput.Repair));
            Assert.That(record.ActualInput, Is.EqualTo(ExperimentInput.LightTruck));
            Assert.That(record.WrongButtonType, Is.EqualTo(ExperimentInput.LightTruck.ToString()));
            Assert.That(record.IsCorrect, Is.False);
            Assert.That(record.EventualCorrect, Is.True);
            Assert.That(record.SpecialStateMissed, Is.True);
            Assert.That(record.InformationAvailableAtMs, Is.EqualTo(0d));
            Assert.That(record.TimeToFirstRawTouchMs, Is.GreaterThanOrEqualTo(0d));
            Assert.That(record.TimeToFirstAcceptedInputMs, Is.GreaterThanOrEqualTo(record.TimeToFirstRawTouchMs));
            Assert.That(record.TimeToCorrectResolutionMs, Is.GreaterThanOrEqualTo(record.TimeToFirstAcceptedInputMs));
            Assert.That(record.CorrectionCostMs, Is.EqualTo(
                record.TimeToCorrectResolutionMs - record.TimeToFirstRawTouchMs).Within(0.001d));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TargetChangeBetweenTouchAndFlushRejectsStaleInput()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreateRushPackage(RushPackagePreset.CurrentEquivalent),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "RUSH-OLD");

            _controller.RawCaptureForTests.InjectPointerDownForTest(
                71,
                Control(ExperimentInput.Sports).ScreenCenter(),
                1000d,
                560);
            _controller.ForceTargetVehicleIdForTests("RUSH-NEW");
            _controller.FlushInputFrameForTests();

            ExperimentInputEventRecord stale = _controller.RawEventsForTests.Single();
            Assert.That(stale.TargetVehicleIdAtTouch, Is.EqualTo("RUSH-OLD"));
            Assert.That(stale.TargetVehicleIdAtAccept, Is.EqualTo("RUSH-NEW"));
            Assert.That(stale.TargetChangedBetweenTouchAndAccept, Is.True);
            Assert.That(stale.ResolvedVehicleId, Is.Empty);
            Assert.That(stale.WasInputAccepted, Is.False);
            Assert.That(stale.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.StaleTarget));
            Assert.That(_controller.TrialRecordsForTests, Is.Empty);

            ExperimentInputEventRecord current = TapAndFlush(ExperimentInput.Sports, 1200d, 561);
            Assert.That(current.WasInputAccepted, Is.True);
            Assert.That(current.TargetVehicleIdAtTouch, Is.EqualTo("RUSH-NEW"));
            Assert.That(current.ResolvedVehicleId, Is.EqualTo("RUSH-NEW"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GapInputIsSavedAsNonTrialAndCannotBeAttributedToMeasuredTrial()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "GAP-TARGET");

            ExperimentInputEventRecord resolved = TapAndFlush(ExperimentInput.Sports, 1000d, 562);
            Assert.That(resolved.WasInputAccepted, Is.True);
            Assert.That(_controller.TrialRecordsForTests, Has.Count.EqualTo(1));

            _controller.RawCaptureForTests.InjectPointerDownForTest(
                72,
                Control(ExperimentInput.Sports).ScreenCenter(),
                1200d,
                563);
            _controller.FlushInputFrameForTests();

            ExperimentInputEventRecord gap = _controller.RawEventsForTests.Last();
            Assert.That(gap.TrialNumber, Is.EqualTo(0));
            Assert.That(gap.TrialSetId, Is.Empty);
            Assert.That(gap.ProblemSet, Is.EqualTo(-1));
            Assert.That(gap.IsPractice, Is.False);
            Assert.That(gap.ExpectedInput, Is.EqualTo(ExperimentInput.None));
            Assert.That(gap.TargetVehicleIdAtTouch, Is.Empty);
            Assert.That(gap.StimulusTimeMs, Is.LessThan(0d));
            Assert.That(gap.ActionableTimeMs, Is.LessThan(0d));
            Assert.That(gap.WasInputAccepted, Is.False);
            Assert.That(gap.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.SessionNotRunning));
            Assert.That(_controller.TrialRecordsForTests, Has.Count.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CheckpointSnapshotIncludesCurrentAndPreviousRepairRects()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.UpperFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "RECT-UPPER");
            Rect upper = Control(ExperimentInput.Repair).ScreenRect();

            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "RECT-BOTTOM");
            Rect bottom = Control(ExperimentInput.Repair).ScreenRect();
            ExperimentSessionCheckpoint checkpoint = _controller.CreateCheckpointForTests();

            Assert.That(checkpoint.CurrentRepairRect, Is.Not.Null);
            Assert.That(checkpoint.PreviousRepairRect, Is.Not.Null);
            AssertRectEqual(checkpoint.CurrentRepairRect.ToRect(), bottom);
            AssertRectEqual(checkpoint.PreviousRepairRect.ToRect(), upper);
            Assert.That(checkpoint.RepairRectChangedAgeMs, Is.GreaterThanOrEqualTo(0d));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimeoutResolution_CommitsNextTrialIndexBeforeCheckpoint()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.UpperFixed),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "TIMEOUT-TRANSITION");

            _controller.ResolveTimeoutForTests();
            ExperimentSessionCheckpoint checkpoint = _controller.CreateCheckpointForTests();

            Assert.That(checkpoint.TrialRecords, Has.Count.EqualTo(1));
            Assert.That(checkpoint.TrialRecords[0].IsTimeout, Is.True);
            Assert.That(checkpoint.TrialIndex, Is.EqualTo(1));
            Assert.That(checkpoint.TrialResolving, Is.True);
            Assert.That(checkpoint.CurrentTrialRecord, Is.Null);
            Assert.That(checkpoint.RemainingGapMs, Is.GreaterThanOrEqualTo(0d));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FinalResolvedCheckpoint_AdvancesToSurveyWithoutRepeatingTrial()
        {
            string temporaryRoot = Path.Combine(
                Path.GetTempPath(),
                "siwakenja-inputui-final-gap-" + Guid.NewGuid().ToString("N"));
            const string participantId = "P00";
            string sessionId = "FINAL-GAP-" + Guid.NewGuid().ToString("N");
            string exportedSessionDirectory = Path.Combine(
                Application.persistentDataPath,
                "InputUIComparison",
                "v2",
                InputUIExperimentSchema.CurrentExperimentVersion,
                sessionId);
            try
            {
                ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule(participantId, ExperimentType.Placement);
                ExperimentConditionAssignment assignment = schedule.Assignments[0];
                ExperimentSessionManifest manifest = SessionPersistence.CreateManifestSnapshot(
                    participantId,
                    sessionId,
                    ExperimentType.Placement,
                    schedule.Assignments,
                    assignment.RepairAnchorMode,
                    assignment.RepairAssistMode,
                    assignment.VehicleButtonLayoutMode,
                    true,
                    experimentVersion: schedule.ExperimentVersion,
                    referenceConditionId: schedule.ReferenceConditionId,
                    conditionDefinitions: schedule.ConditionDefinitions);
                var persistence = new SessionPersistence(
                    participantId,
                    sessionId,
                    schedule.ExperimentVersion,
                    temporaryRoot);
                persistence.BeginSession(manifest);
                ExperimentTrial completedTrial = SyntheticTrial(
                    1,
                    ExperimentVehicle.Sports,
                    ExperimentSpecialState.Normal,
                    ExperimentSpeed.Normal);
                completedTrial.TrialSetId = assignment.TrialSetId;
                var timeoutRecord = new ExperimentTrialRecord
                {
                    SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                    ExperimentVersion = schedule.ExperimentVersion,
                    ParticipantId = participantId,
                    SessionId = sessionId,
                    ExperimentType = ExperimentType.Placement,
                    Condition = assignment.Condition,
                    ConditionOrder = assignment.ConditionOrder,
                    TrialSetId = assignment.TrialSetId,
                    TrialNumber = 1,
                    ExpectedInput = ExperimentInput.Sports,
                    IsTimeout = true,
                    EventualCorrect = false,
                };
                var checkpoint = new ExperimentSessionCheckpoint
                {
                    Manifest = manifest,
                    Phase = "Measured",
                    ConditionIndex = 0,
                    TrialIndex = 1,
                    EventIndex = 0,
                    RemainingGapMs = 1d,
                    TrialResolving = true,
                    CoveredRevealed = true,
                    ActiveTrials = new List<ExperimentTrial> { completedTrial },
                    TrialRecords = new List<ExperimentTrialRecord> { timeoutRecord },
                };
                persistence.SaveCheckpoint(checkpoint);

                _controller.RestoreCheckpointForTests(persistence, manifest, checkpoint);
                yield return new WaitForSecondsRealtime(0.15f);

                Assert.That(_controller.PhaseForTests, Is.EqualTo("Survey"));
                Assert.That(_controller.CurrentTrialNumberForTests, Is.EqualTo(0));
                Assert.That(_controller.ActiveVehicleCountForTests, Is.EqualTo(0));
                Assert.That(_controller.TrialRecordsForTests, Has.Count.EqualTo(1));
                Assert.That(_controller.TrialRecordsForTests[0].TrialNumber, Is.EqualTo(1));
            }
            finally
            {
                DeleteOwnedTemporaryDirectory(temporaryRoot, Path.GetTempPath(), "siwakenja-inputui-final-gap-");
                DeleteOwnedTemporaryDirectory(
                    exportedSessionDirectory,
                    Path.Combine(Application.persistentDataPath, "InputUIComparison", "v2", InputUIExperimentSchema.CurrentExperimentVersion),
                    "FINAL-GAP-");
            }
        }

        [UnityTest]
        public IEnumerator CoveredRevealTransition_IsImmediatelyCheckpointed()
        {
            string temporaryRoot = Path.Combine(
                Path.GetTempPath(),
                "siwakenja-inputui-covered-checkpoint-" + Guid.NewGuid().ToString("N"));
            const string participantId = "P00";
            string sessionId = "COVERED-CP-" + Guid.NewGuid().ToString("N");
            string exportedSessionDirectory = Path.Combine(
                Application.persistentDataPath,
                "InputUIComparison",
                "v2",
                InputUIExperimentSchema.CurrentExperimentVersion,
                sessionId);
            try
            {
                ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule(participantId, ExperimentType.Placement);
                ExperimentConditionAssignment assignment = schedule.Assignments[0];
                ExperimentSessionManifest manifest = SessionPersistence.CreateManifestSnapshot(
                    participantId,
                    sessionId,
                    ExperimentType.Placement,
                    schedule.Assignments,
                    assignment.RepairAnchorMode,
                    assignment.RepairAssistMode,
                    assignment.VehicleButtonLayoutMode,
                    true,
                    experimentVersion: schedule.ExperimentVersion,
                    referenceConditionId: schedule.ReferenceConditionId,
                    conditionDefinitions: schedule.ConditionDefinitions);
                var persistence = new SessionPersistence(
                    participantId,
                    sessionId,
                    schedule.ExperimentVersion,
                    temporaryRoot);
                persistence.BeginSession(manifest);
                ExperimentTrial coveredTrial = SyntheticTrial(
                    1,
                    ExperimentVehicle.Compact,
                    ExperimentSpecialState.Covered,
                    ExperimentSpeed.Normal);
                coveredTrial.TrialSetId = assignment.TrialSetId;
                var currentRecord = new ExperimentTrialRecord
                {
                    SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                    ExperimentVersion = schedule.ExperimentVersion,
                    ParticipantId = participantId,
                    SessionId = sessionId,
                    ExperimentType = ExperimentType.Placement,
                    Condition = assignment.Condition,
                    ConditionOrder = assignment.ConditionOrder,
                    TrialSetId = assignment.TrialSetId,
                    TrialNumber = 1,
                    Vehicle = ExperimentVehicle.Compact,
                    SpecialState = ExperimentSpecialState.Covered,
                    ExpectedInput = ExperimentInput.Compact,
                    InformationAvailableAtMs = -1d,
                };
                var checkpoint = new ExperimentSessionCheckpoint
                {
                    Manifest = manifest,
                    Phase = "Measured",
                    ConditionIndex = 0,
                    TrialIndex = 0,
                    TrialElapsedMs = 100d,
                    ActionableElapsedMs = -1d,
                    CoveredRevealed = false,
                    TrialResolving = false,
                    ActiveTrials = new List<ExperimentTrial> { coveredTrial },
                    CurrentTrialRecord = currentRecord,
                };
                persistence.SaveCheckpoint(checkpoint);

                _controller.RestoreCheckpointForTests(persistence, manifest, checkpoint);
                _controller.SetCoveredRevealedForTests(true);
                Assert.That(persistence.TryLoadLatestCheckpoint(out ExperimentSessionCheckpoint revealed), Is.True);
                Assert.That(revealed.CoveredRevealed, Is.True);
                Assert.That(revealed.ActionableElapsedMs, Is.GreaterThanOrEqualTo(0d));
                Assert.That(revealed.CurrentTrialRecord.InformationAvailableAtMs, Is.GreaterThanOrEqualTo(0d));
            }
            finally
            {
                DeleteOwnedTemporaryDirectory(temporaryRoot, Path.GetTempPath(), "siwakenja-inputui-covered-checkpoint-");
                DeleteOwnedTemporaryDirectory(
                    exportedSessionDirectory,
                    Path.Combine(Application.persistentDataPath, "InputUIComparison", "v2", InputUIExperimentSchema.CurrentExperimentVersion),
                    "COVERED-CP-");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SubmittedSurveyCheckpoint_AdvancesWithoutDuplicateResponse()
        {
            string temporaryRoot = Path.Combine(
                Path.GetTempPath(),
                "siwakenja-inputui-survey-transition-" + Guid.NewGuid().ToString("N"));
            const string participantId = "P00";
            string sessionId = "SURVEY-CP-" + Guid.NewGuid().ToString("N");
            string exportedSessionDirectory = Path.Combine(
                Application.persistentDataPath,
                "InputUIComparison",
                "v2",
                InputUIExperimentSchema.CurrentExperimentVersion,
                sessionId);
            try
            {
                ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule(participantId, ExperimentType.Placement);
                ExperimentConditionAssignment first = schedule.Assignments[0];
                ExperimentSessionManifest manifest = SessionPersistence.CreateManifestSnapshot(
                    participantId,
                    sessionId,
                    ExperimentType.Placement,
                    schedule.Assignments,
                    first.RepairAnchorMode,
                    first.RepairAssistMode,
                    first.VehicleButtonLayoutMode,
                    true,
                    experimentVersion: schedule.ExperimentVersion,
                    referenceConditionId: schedule.ReferenceConditionId,
                    conditionDefinitions: schedule.ConditionDefinitions);
                var persistence = new SessionPersistence(
                    participantId,
                    sessionId,
                    schedule.ExperimentVersion,
                    temporaryRoot);
                persistence.BeginSession(manifest);
                var submitted = new ExperimentSurveyRecord
                {
                    SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                    ExperimentVersion = schedule.ExperimentVersion,
                    ParticipantId = participantId,
                    SessionId = sessionId,
                    ExperimentType = ExperimentType.Placement,
                    Condition = first.Condition,
                    ConditionOrder = first.ConditionOrder,
                    TrialSetId = first.TrialSetId,
                    IsPilot = true,
                };
                var checkpoint = new ExperimentSessionCheckpoint
                {
                    Manifest = manifest,
                    Phase = "Survey",
                    ConditionIndex = 0,
                    TrialIndex = InputUITrialPlan.MeasuredTrialsPerVariant,
                    SurveyRecords = new List<ExperimentSurveyRecord> { submitted },
                };
                persistence.SaveCheckpoint(checkpoint);

                _controller.RestoreCheckpointForTests(persistence, manifest, checkpoint);

                Assert.That(_controller.PhaseForTests, Is.EqualTo("BlockIntro"));
                Assert.That(_controller.CurrentConditionForTests, Is.EqualTo(schedule.Assignments[1].Condition));
                Assert.That(_controller.SurveyRecordCountForTests, Is.EqualTo(1));
            }
            finally
            {
                DeleteOwnedTemporaryDirectory(temporaryRoot, Path.GetTempPath(), "siwakenja-inputui-survey-transition-");
                DeleteOwnedTemporaryDirectory(
                    exportedSessionDirectory,
                    Path.Combine(Application.persistentDataPath, "InputUIComparison", "v2", InputUIExperimentSchema.CurrentExperimentVersion),
                    "SURVEY-CP-");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CheckpointRoundTripRestoresControllerAndAcceptanceState()
        {
            string temporaryRoot = Path.Combine(
                Path.GetTempPath(),
                "siwakenja-inputui-playmode-" + Guid.NewGuid().ToString("N"));
            string participantId = "P00";
            string sessionId = "PLAYMODE-" + Guid.NewGuid().ToString("N");
            string exportedSessionDirectory = Path.Combine(
                Application.persistentDataPath,
                "InputUIComparison",
                "v2",
                InputUIExperimentSchema.CurrentExperimentVersion,
                sessionId);

            try
            {
                ExperimentSchedule schedule = InputUITrialPlan.BuildSchedule(participantId, ExperimentType.Placement);
                ExperimentConditionAssignment assignment = schedule.Assignments[0];
                ExperimentSessionManifest manifest = SessionPersistence.CreateManifestSnapshot(
                    participantId,
                    sessionId,
                    ExperimentType.Placement,
                    schedule.Assignments,
                    assignment.RepairAnchorMode,
                    assignment.RepairAssistMode,
                    assignment.VehicleButtonLayoutMode,
                    true,
                    "playmode-test");
                var persistence = new SessionPersistence(
                    participantId,
                    sessionId,
                    InputUIExperimentSchema.CurrentExperimentVersion,
                    temporaryRoot);
                persistence.BeginSession(manifest);
                BeginSynthetic(
                    ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.UpperFixed),
                    ExperimentSpecialState.Normal,
                    ExperimentVehicle.Compact,
                    "PREPARE-REAL-RECT");
                Rect checkpointCurrentRepairRect = Control(ExperimentInput.Repair).ScreenRect();

                ExperimentTrial activeTrial = SyntheticTrial(
                    7,
                    ExperimentVehicle.Compact,
                    ExperimentSpecialState.Normal,
                    ExperimentSpeed.High);
                activeTrial.TrialSetId = assignment.TrialSetId;
                var currentRecord = new ExperimentTrialRecord
                {
                    ParticipantId = participantId,
                    SessionId = sessionId,
                    ExperimentType = ExperimentType.Placement,
                    Condition = assignment.Condition,
                    ConditionOrder = assignment.ConditionOrder,
                    TrialSetId = assignment.TrialSetId,
                    TrialNumber = activeTrial.TrialNumber,
                    Vehicle = activeTrial.Vehicle,
                    SpecialState = activeTrial.SpecialState,
                    Speed = activeTrial.Speed,
                    ExpectedInput = ExperimentInput.Compact,
                    ActualInput = ExperimentInput.None,
                    FinalInput = ExperimentInput.None,
                    InformationAvailableAtMs = 0d,
                    TimeToFirstRawTouchMs = -1d,
                    TimeToFirstAcceptedInputMs = -1d,
                    TimeToCorrectResolutionMs = -1d,
                    CorrectionCostMs = -1d,
                    ResponseTimeMs = -1d,
                    ActionableResponseTimeMs = -1d,
                    FinalResponseTimeMs = -1d,
                };
                var checkpoint = new ExperimentSessionCheckpoint
                {
                    Manifest = manifest,
                    Phase = "Measured",
                    ConditionIndex = 0,
                    TrialIndex = 0,
                    EventIndex = 12,
                    Score = 30,
                    Misses = 2,
                    TrialElapsedMs = 425d,
                    ActionableElapsedMs = 425d,
                    LastAcceptedEventTimeMs = 900d,
                    CurrentTargetVehicleId = "RESUME-TARGET",
                    CoveredRevealed = true,
                    TrialResolving = false,
                    CurrentRepairRect = ExperimentRectCheckpoint.FromRect(checkpointCurrentRepairRect),
                    PreviousRepairRect = ExperimentRectCheckpoint.FromRect(new Rect(2f, 2f, 12f, 12f)),
                    RepairRectChangedAgeMs = 375d,
                    ActiveTrials = new List<ExperimentTrial> { activeTrial },
                    CurrentTrialRecord = currentRecord,
                    TrialRecords = new List<ExperimentTrialRecord>
                    {
                        new ExperimentTrialRecord
                        {
                            ParticipantId = participantId,
                            SessionId = sessionId,
                            ExperimentType = ExperimentType.Placement,
                            Condition = assignment.Condition,
                            TrialNumber = 6,
                            ExpectedInput = ExperimentInput.Sports,
                            ActualInput = ExperimentInput.Sports,
                            FinalInput = ExperimentInput.Sports,
                            IsCorrect = true,
                            EventualCorrect = true,
                            InformationAvailableAtMs = 0d,
                            TimeToFirstRawTouchMs = 300d,
                            TimeToFirstAcceptedInputMs = 300d,
                            TimeToCorrectResolutionMs = 300d,
                            CorrectionCostMs = 0d,
                        },
                    },
                    InputEventRecords = new List<ExperimentInputEventRecord>
                    {
                        new ExperimentInputEventRecord
                        {
                            ParticipantId = participantId,
                            SessionId = sessionId,
                            EventIndex = 12,
                            WasInputAccepted = true,
                            AcceptedInput = ExperimentInput.Sports,
                            IgnoredReason = ExperimentIgnoredReasons.None,
                            Disposition = "accepted",
                            TargetVehicleIdAtTouch = "RESUME-TARGET",
                        },
                        new ExperimentInputEventRecord
                        {
                            ParticipantId = participantId,
                            SessionId = sessionId,
                            EventIndex = 13,
                            TrialNumber = 7,
                            Input = ExperimentInput.Compact,
                            ExpectedInput = ExperimentInput.Compact,
                            TargetVehicleIdAtTouch = "RESUME-TARGET",
                            Disposition = "raw",
                        },
                    },
                    PendingInputRecords = new List<ExperimentInputEventRecord>
                    {
                        new ExperimentInputEventRecord
                        {
                            ParticipantId = participantId,
                            SessionId = sessionId,
                            EventIndex = 13,
                            TrialNumber = 7,
                            Input = ExperimentInput.Compact,
                            ExpectedInput = ExperimentInput.Compact,
                            TargetVehicleIdAtTouch = "RESUME-TARGET",
                            Disposition = "raw",
                        },
                    },
                };

                Assert.That(persistence.SaveCheckpoint(checkpoint), Is.EqualTo(1L));
                checkpoint.Score = 40;
                checkpoint.EventIndex = 13;
                Assert.That(persistence.SaveCheckpoint(checkpoint), Is.EqualTo(2L));

                // WAL keeps the capture-time copy of every physical down. The
                // checkpoint-finalized event 12 must win over its raw WAL copy;
                // journal-only event 14 must still survive the simulated kill.
                persistence.AppendRawEventJournal(JournalEvent(
                    participantId,
                    sessionId,
                    assignment,
                    12,
                    ExperimentInput.Sports,
                    910d));
                persistence.AppendRawEventJournal(JournalEvent(
                    participantId,
                    sessionId,
                    assignment,
                    14,
                    ExperimentInput.Compact,
                    920d));

                Assert.That(
                    SessionPersistence.TryFindLatestIncompleteSession(
                        participantId,
                        out ExperimentSessionManifest discovered,
                        InputUIExperimentSchema.CurrentExperimentVersion,
                        temporaryRoot),
                    Is.True);
                Assert.That(discovered.SessionId, Is.EqualTo(sessionId));
                Assert.That(discovered.CompletionStatus, Is.EqualTo(SessionPersistence.StatusInProgress));

                var resumedPersistence = new SessionPersistence(
                    discovered.ParticipantId,
                    discovered.SessionId,
                    discovered.ExperimentVersion,
                    temporaryRoot);
                Assert.That(resumedPersistence.TryLoadLatestCheckpoint(out ExperimentSessionCheckpoint resumed), Is.True);
                Assert.That(resumed.Revision, Is.EqualTo(2L));
                Assert.That(resumed.Score, Is.EqualTo(40));
                Assert.That(resumed.EventIndex, Is.EqualTo(13));
                Assert.That(resumed.CurrentTargetVehicleId, Is.EqualTo("RESUME-TARGET"));
                AssertRectEqual(resumed.CurrentRepairRect.ToRect(), checkpointCurrentRepairRect);
                AssertRectEqual(resumed.PreviousRepairRect.ToRect(), new Rect(2f, 2f, 12f, 12f));
                Assert.That(resumed.RepairRectChangedAgeMs, Is.EqualTo(375d));
                Assert.That(resumed.ActiveTrials, Has.Count.EqualTo(1));
                Assert.That(resumed.CurrentTrialRecord.ExpectedInput, Is.EqualTo(ExperimentInput.Compact));

                _controller.RestoreCheckpointForTests(resumedPersistence, discovered, resumed);
                Assert.That(_controller.SessionConfigurationLockedForTests, Is.True);
                Assert.That(_controller.CurrentTargetVehicleIdForTests, Is.EqualTo("RESUME-TARGET"));
                Assert.That(_controller.HasCurrentRepairRectForTests, Is.True);
                Assert.That(_controller.HasPreviousRepairRectForTests, Is.True);
                AssertRectEqual(_controller.CurrentRepairRectForTests, checkpointCurrentRepairRect);
                AssertRectEqual(_controller.PreviousRepairRectForTests, new Rect(2f, 2f, 12f, 12f));
                Assert.That(_controller.ActiveVehicleCountForTests, Is.EqualTo(1));
                Assert.That(_controller.RawEventsForTests, Has.Count.EqualTo(3));
                Assert.That(_controller.RawEventsForTests.Count(row => row.EventIndex == 12), Is.EqualTo(1));
                Assert.That(_controller.RawEventsForTests.Single(row => row.EventIndex == 12).WasInputAccepted, Is.True,
                    "A finalized checkpoint row must take precedence over its Raw WAL copy.");
                Assert.That(_controller.TrialRecordsForTests.Select(row => row.TrialNumber),
                    Is.EquivalentTo(new[] { 6 }), "Completed trials must survive exactly once across restore.");
                ExperimentInputEventRecord interruptedPending = _controller.RawEventsForTests
                    .Single(row => row.EventIndex == 13);
                Assert.That(interruptedPending.WasInputAccepted, Is.False);
                Assert.That(interruptedPending.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.StaleTarget));
                ExperimentInputEventRecord journalOnly = _controller.RawEventsForTests
                    .Single(row => row.EventIndex == 14);
                Assert.That(journalOnly.WasInputAccepted, Is.False);
                Assert.That(journalOnly.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.StaleTarget));

                _controller.RawCaptureForTests.InjectPointerDownForTest(73, new Vector2(8f, 8f), 925d, 564);
                _controller.FlushInputFrameForTests();
                ExperimentInputEventRecord restoredPreviousRectTap = _controller.RawEventsForTests.Last();
                Assert.That(restoredPreviousRectTap.EventIndex, Is.EqualTo(15));
                Assert.That(restoredPreviousRectTap.IsInsidePreviousRepairRect, Is.True);
                Assert.That(restoredPreviousRectTap.IsInsideCurrentRepairRect, Is.False);

                ExperimentInputEventRecord firstAfterResume = TapAndFlush(ExperimentInput.Sports, 950d, 565);
                Assert.That(firstAfterResume.WasInputAccepted, Is.True,
                    "A process-local monotonic cooldown timestamp must not suppress post-relaunch input.");
                Assert.That(firstAfterResume.IgnoredReason, Is.EqualTo(ExperimentIgnoredReasons.None));
                Assert.That(_controller.TrialRecordsForTests.Select(row => row.TrialNumber),
                    Is.EquivalentTo(new[] { 6 }));
            }
            finally
            {
                DeleteOwnedTemporaryDirectory(temporaryRoot, Path.GetTempPath(), "siwakenja-inputui-playmode-");
                DeleteOwnedTemporaryDirectory(
                    exportedSessionDirectory,
                    Path.Combine(Application.persistentDataPath, "InputUIComparison", "v2", InputUIExperimentSchema.CurrentExperimentVersion),
                    "PLAYMODE-");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RushShowsThreeVehiclesAndLogsCurrentTargetId()
        {
            BeginSynthetic(
                ExperimentLayoutCatalog.CreateRushPackage(RushPackagePreset.CurrentEquivalent),
                ExperimentSpecialState.Normal,
                ExperimentVehicle.Sports,
                "RUSH-CURRENT");

            Assert.That(_controller.ActiveVehicleCountForTests, Is.EqualTo(3));
            Assert.That(_controller.ActiveVehicleIdsForTests.Count, Is.EqualTo(3));
            Assert.That(_controller.ActiveVehicleIdsForTests.Distinct().Count(), Is.EqualTo(3));
            Assert.That(_controller.ActiveVehicleIdsForTests[0], Does.EndWith("M001"));
            Assert.That(_controller.ActiveVehicleIdsForTests[1], Does.EndWith("M002"));
            Assert.That(_controller.ActiveVehicleIdsForTests[2], Does.EndWith("M003"));
            Assert.That(_controller.CurrentTargetVehicleIdForTests, Is.EqualTo("RUSH-CURRENT"));

            RectTransform[] activeCarCards = _controller.GetComponentsInChildren<RectTransform>(true)
                .Where(value => value.name.StartsWith("CarCard", StringComparison.Ordinal) && value.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(activeCarCards, Has.Length.EqualTo(3));
            Assert.That(activeCarCards.Select(value => value.anchoredPosition.x).Distinct().Count(), Is.EqualTo(3));

            ExperimentInputEventRecord row = TapAndFlush(ExperimentInput.Sports, 1000d, 570);
            Assert.That(row.WasInputAccepted, Is.True);
            Assert.That(row.TargetVehicleIdAtTouch, Is.EqualTo("RUSH-CURRENT"));
            Assert.That(row.TargetVehicleIdAtAccept, Is.EqualTo("RUSH-CURRENT"));
            Assert.That(row.ResolvedVehicleId, Is.EqualTo("RUSH-CURRENT"));
            yield return null;
        }

        private void BeginSynthetic(
            ExperimentLayoutSpec layout,
            ExperimentSpecialState state,
            ExperimentVehicle vehicle,
            string targetVehicleId)
        {
            _controller.BeginSyntheticTrialForTests(layout, state, vehicle, targetVehicleId);
            Canvas.ForceUpdateCanvases();
        }

        private ExperimentPointerInput Control(ExperimentInput input)
        {
            return _controller.GetControlForTests(input);
        }

        private ExperimentInputEventRecord TapAndFlush(ExperimentInput input, double eventTimeMs, int frame)
        {
            int before = _controller.RawEventsForTests.Count;
            _controller.RawCaptureForTests.InjectPointerDownForTest(
                100 + before,
                Control(input).ScreenCenter(),
                eventTimeMs,
                frame);
            Assert.That(_controller.RawEventsForTests.Count, Is.EqualTo(before + 1));
            _controller.FlushInputFrameForTests();
            return _controller.RawEventsForTests[before];
        }

        private ExperimentInputEventRecord TapPointAndFlush(Vector2 screenPosition, double eventTimeMs, int frame)
        {
            int before = _controller.RawEventsForTests.Count;
            _controller.RawCaptureForTests.InjectPointerDownForTest(
                200 + before,
                screenPosition,
                eventTimeMs,
                frame);
            Assert.That(_controller.RawEventsForTests.Count, Is.EqualTo(before + 1));
            _controller.FlushInputFrameForTests();
            return _controller.RawEventsForTests[before];
        }

        private RectTransform RectTransformNamed(string objectName)
        {
            return _controller.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == objectName);
        }

        private static Vector2 RectCenterToScreen(RectTransform rect)
        {
            return RectLocalPointToScreen(rect, rect.rect.center);
        }

        private static Vector2 RectLocalPointToScreen(RectTransform rect, Vector2 localPoint)
        {
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(localPoint));
        }

        private static InputArbitrationContext ArbitrationContext()
        {
            return new InputArbitrationContext
            {
                SessionRunning = true,
                TrialActionable = true,
                CoveredNotRevealed = false,
                AcceptCoveredPrematureInput = true,
                TargetVehicleIdAtAccept = "TARGET",
                LastAcceptedEventTimeMs = -1d,
                CooldownDurationMs = 80d,
            };
        }

        private static InputArbitrationCandidate ArbitrationCandidate(
            long sequence,
            ExperimentInput input,
            string buttonId,
            bool visible,
            bool interactable,
            double eventTimeMs)
        {
            return new InputArbitrationCandidate
            {
                EventSequence = sequence,
                EventTimeMs = eventTimeMs,
                Input = input,
                HitButtonId = buttonId,
                WasButtonVisible = visible,
                WasButtonInteractable = interactable,
                TargetVehicleIdAtTouch = "TARGET",
                WasTrialRunningAtTouch = true,
                WasCoveredRevealedAtTouch = true,
            };
        }

        private Vector2 FindEmptyControlPoint()
        {
            Rect[] occupied = AllInputs.Select(input => Control(input).ScreenRect()).ToArray();
            for (int yStep = 1; yStep < 10; yStep++)
            {
                for (int xStep = 1; xStep < 10; xStep++)
                {
                    var candidate = new Vector2(Screen.width * xStep / 10f, Screen.height * yStep / 10f);
                    if (occupied.All(rect => !rect.Contains(candidate))) return candidate;
                }
            }

            Assert.Fail("Could not find an on-screen point outside all experiment controls.");
            return Vector2.zero;
        }

        private static ExperimentInputEventRecord JournalEvent(
            string participantId,
            string sessionId,
            ExperimentConditionAssignment assignment,
            int eventIndex,
            ExperimentInput input,
            double eventTimeMs)
        {
            return new ExperimentInputEventRecord
            {
                SchemaVersion = InputUIExperimentSchema.CurrentSchemaVersion,
                ExperimentVersion = InputUIExperimentSchema.CurrentExperimentVersion,
                ParticipantId = participantId,
                SessionId = sessionId,
                ExperimentType = assignment.ExperimentType,
                Condition = assignment.Condition,
                ConditionOrder = assignment.ConditionOrder,
                TrialSetId = assignment.TrialSetId,
                TrialNumber = 7,
                EventIndex = eventIndex,
                EventTimeMs = eventTimeMs,
                Input = input,
                ExpectedInput = ExperimentInput.Compact,
                TargetVehicleIdAtTouch = "RESUME-TARGET",
                IgnoredReason = ExperimentIgnoredReasons.None,
                Disposition = "raw",
            };
        }

        private static ExperimentTrial SyntheticTrial(
            int trialNumber,
            ExperimentVehicle vehicle,
            ExperimentSpecialState state,
            ExperimentSpeed speed)
        {
            return new ExperimentTrial
            {
                TrialNumber = trialNumber,
                ProblemSet = 0,
                TrialSetId = InputUITrialPlan.Set01,
                IsPractice = false,
                Vehicle = vehicle,
                SpecialState = state,
                Speed = speed,
                CoveredRevealProgress = InputUIComparisonRules.CoveredRevealProgress,
                GapAfterSeconds = 0.22f,
                PaceContext = "playmode-test",
                SequenceContext = "playmode-test",
            };
        }

        private static void AssertPresentationEqual(
            ExperimentRepairPresentation first,
            ExperimentRepairPresentation second)
        {
            Assert.That(second.Alpha, Is.EqualTo(first.Alpha));
            Assert.That(second.Interactable, Is.EqualTo(first.Interactable));
            Assert.That(second.BlocksRaycasts, Is.EqualTo(first.BlocksRaycasts));
            Assert.That(second.ReservesLayout, Is.EqualTo(first.ReservesLayout));
            Assert.That(second.RejectedInputReason, Is.EqualTo(first.RejectedInputReason));
        }

        private static void AssertRectEqual(Rect actual, Rect expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.01f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.01f));
            Assert.That(actual.width, Is.EqualTo(expected.width).Within(0.01f));
            Assert.That(actual.height, Is.EqualTo(expected.height).Within(0.01f));
        }

        private static void DeleteOwnedTemporaryDirectory(string path, string expectedParent, string expectedPrefix)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
            string fullPath = Path.GetFullPath(path);
            string fullParent = Path.GetFullPath(expectedParent)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            Assert.That(fullPath.StartsWith(fullParent, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(fullPath).StartsWith(expectedPrefix, StringComparison.Ordinal), Is.True);
            Directory.Delete(fullPath, true);
        }
    }
}
