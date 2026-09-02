using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison.Tests
{
    public sealed class InputUILayoutCatalogEditModeTests
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

        [Test]
        public void PlacementConditions_DifferOnlyByRepairAnchoredPosition()
        {
            ExperimentLayoutSpec near = ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.UpperFixed);
            ExperimentLayoutSpec bottom = ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed);

            foreach (ExperimentInput input in VehicleInputs)
            {
                AssertSameControl(near.GetControl(input), bottom.GetControl(input), input.ToString());
            }

            ExperimentControlSpec nearRepair = near.GetControl(ExperimentInput.Repair);
            ExperimentControlSpec bottomRepair = bottom.GetControl(ExperimentInput.Repair);
            Assert.That(nearRepair.RectTransform.AnchorMin, Is.EqualTo(bottomRepair.RectTransform.AnchorMin));
            Assert.That(nearRepair.RectTransform.AnchorMax, Is.EqualTo(bottomRepair.RectTransform.AnchorMax));
            Assert.That(nearRepair.RectTransform.Pivot, Is.EqualTo(bottomRepair.RectTransform.Pivot));
            Assert.That(nearRepair.RectTransform.SizeDelta, Is.EqualTo(new Vector2(470f, 112f)));
            Assert.That(bottomRepair.RectTransform.SizeDelta, Is.EqualTo(nearRepair.RectTransform.SizeDelta));
            Assert.That(nearRepair.RectTransform.AnchoredPosition, Is.EqualTo(new Vector2(540f, 800f)));
            Assert.That(bottomRepair.RectTransform.AnchoredPosition, Is.EqualTo(new Vector2(540f, 462f)));
            Assert.That(nearRepair.Style.ExactlyEquals(bottomRepair.Style), Is.True);

            foreach (ExperimentSpecialState state in Enum.GetValues(typeof(ExperimentSpecialState)))
            {
                ExperimentRepairPresentation nearPresentation = near.GetRepairPresentation(state);
                ExperimentRepairPresentation bottomPresentation = bottom.GetRepairPresentation(state);
                AssertSamePresentation(nearPresentation, bottomPresentation, state.ToString());
                Assert.That(nearPresentation.Visible, Is.True, state.ToString());
                Assert.That(nearPresentation.Interactable, Is.True, state.ToString());
            }
        }

        [TestCase(RepairAnchorMode.UpperFixed)]
        [TestCase(RepairAnchorMode.BottomFixed)]
        public void AssistConditions_KeepEveryControlRectAndStyle(RepairAnchorMode anchorMode)
        {
            ExperimentLayoutSpec reference = ExperimentLayoutCatalog.CreateStateAssist(
                RepairAssistMode.AlwaysActive,
                anchorMode);
            foreach (RepairAssistMode assistMode in Enum.GetValues(typeof(RepairAssistMode)))
            {
                ExperimentLayoutSpec candidate = ExperimentLayoutCatalog.CreateStateAssist(assistMode, anchorMode);
                foreach (ExperimentInput input in AllInputs)
                {
                    AssertSameControl(
                        reference.GetControl(input),
                        candidate.GetControl(input),
                        anchorMode + "/" + assistMode + "/" + input);
                }
            }
        }

        [Test]
        public void AssistPresentation_ImplementsSpecifiedCanvasGroupStates()
        {
            AssertPresentation(
                RepairAssistMode.AlwaysActive,
                ExperimentSpecialState.Normal,
                1f,
                true,
                true,
                ExperimentIgnoredReasons.None);
            AssertPresentation(
                RepairAssistMode.DisabledWhenNormal,
                ExperimentSpecialState.Normal,
                1f,
                false,
                true,
                ExperimentIgnoredReasons.Disabled);
            AssertPresentation(
                RepairAssistMode.BrokenOnly,
                ExperimentSpecialState.Normal,
                0f,
                false,
                false,
                ExperimentIgnoredReasons.HiddenOrEmpty);

            foreach (RepairAssistMode assistMode in Enum.GetValues(typeof(RepairAssistMode)))
            {
                AssertPresentation(
                    assistMode,
                    ExperimentSpecialState.Broken,
                    1f,
                    true,
                    true,
                    ExperimentIgnoredReasons.None);
            }

            foreach (ExperimentSpecialState nonBroken in new[]
                     {
                         ExperimentSpecialState.Normal,
                         ExperimentSpecialState.Express,
                         ExperimentSpecialState.Covered,
                     })
            {
                ExperimentRepairPresentation hidden = ExperimentLayoutCatalog.ResolveRepairPresentation(
                    RepairAssistMode.BrokenOnly,
                    nonBroken);
                Assert.That(hidden.ReservesLayout, Is.True, nonBroken.ToString());
                Assert.That(hidden.Visible, Is.False, nonBroken.ToString());
            }
        }

        [Test]
        public void BrokenOnly_CanvasGroupApplicationKeepsRectActiveAndReserved()
        {
            var gameObject = new GameObject("RepairPresentationTest", typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                RectTransform rect = gameObject.GetComponent<RectTransform>();
                ExperimentRectTransformSpec original = ExperimentLayoutCatalog
                    .CreateStateAssist(RepairAssistMode.BrokenOnly, RepairAnchorMode.UpperFixed)
                    .GetControl(ExperimentInput.Repair)
                    .RectTransform;
                original.ApplyTo(rect);
                Rect before = original.ToReferenceRect(ExperimentLayoutCatalog.ReferenceResolution);

                ExperimentRepairPresentation hidden = ExperimentLayoutCatalog.ResolveRepairPresentation(
                    RepairAssistMode.BrokenOnly,
                    ExperimentSpecialState.Normal);
                hidden.ApplyTo(gameObject.GetComponent<CanvasGroup>());

                Assert.That(gameObject.activeSelf, Is.True);
                Assert.That(gameObject.GetComponent<CanvasGroup>().alpha, Is.Zero);
                Assert.That(gameObject.GetComponent<CanvasGroup>().interactable, Is.False);
                Assert.That(gameObject.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(hidden.ReservesLayout, Is.True);
                Assert.That(
                    original.ToReferenceRect(ExperimentLayoutCatalog.ReferenceResolution),
                    Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequiredResolutions_ProjectAllCatalogLayoutsWithoutClippingOrOverlap()
        {
            Assert.That(ExperimentLayoutCatalog.RequiredResolutionCases, Has.Count.EqualTo(3));
            Assert.That(
                ExperimentLayoutCatalog.RequiredResolutionCases[0].Id,
                Is.EqualTo("1080x1920"));
            Assert.That(
                ExperimentLayoutCatalog.RequiredResolutionCases[1].Id,
                Is.EqualTo("1080x2340"));
            Assert.That(
                ExperimentLayoutCatalog.RequiredResolutionCases[2].Id,
                Is.EqualTo("720x1600"));
            Assert.That(ExperimentLayoutCatalog.ValidateCatalog(), Is.Empty);

            ExperimentReferenceFrameProjection tall = ExperimentLayoutCatalog.ProjectReferenceFrame(
                ExperimentLayoutCatalog.RequiredResolutionCases[1]);
            Assert.That(tall.ContentRect, Is.EqualTo(new Rect(0f, 210f, 1080f, 1920f)));

            ExperimentReferenceFrameProjection narrow = ExperimentLayoutCatalog.ProjectReferenceFrame(
                ExperimentLayoutCatalog.RequiredResolutionCases[2]);
            Assert.That(narrow.ContentRect.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(narrow.ContentRect.y, Is.EqualTo(160f).Within(0.001f));
            Assert.That(narrow.ContentRect.width, Is.EqualTo(720f).Within(0.001f));
            Assert.That(narrow.ContentRect.height, Is.EqualTo(1280f).Within(0.001f));
        }

        [Test]
        public void RushPresets_RetainLegacyPackageGeometryOnly()
        {
            ExperimentLayoutSpec current = ExperimentLayoutCatalog.CreateRushPackage(
                RushPackagePreset.CurrentEquivalent);
            ExperimentLayoutSpec fixedFour = ExperimentLayoutCatalog.CreateRushPackage(
                RushPackagePreset.FixedFourEquivalent);
            ExperimentLayoutSpec contextual = ExperimentLayoutCatalog.CreateRushPackage(
                RushPackagePreset.ContextualRepairEquivalent);

            AssertLegacyRush(current, ExperimentConditionIds.RushCurrentEquivalent);
            AssertLegacyRush(fixedFour, ExperimentConditionIds.RushFixedFourEquivalent);
            AssertLegacyRush(contextual, ExperimentConditionIds.RushContextualRepairEquivalent);

            Assert.That(
                current.GetControl(ExperimentInput.Repair).RectTransform.SizeDelta,
                Is.EqualTo(new Vector2(470f, 112f)));
            Assert.That(
                fixedFour.GetControl(ExperimentInput.Repair).RectTransform.AnchoredPosition,
                Is.EqualTo(new Vector2(940f, 175f)));
            Assert.That(
                fixedFour.GetControl(ExperimentInput.Repair).RectTransform.SizeDelta,
                Is.EqualTo(new Vector2(240f, 260f)));
            Assert.That(
                contextual.GetControl(ExperimentInput.Repair).RectTransform.AnchoredPosition,
                Is.EqualTo(new Vector2(540f, 800f)));
            Assert.That(
                contextual.GetControl(ExperimentInput.Repair).RectTransform.SizeDelta,
                Is.EqualTo(new Vector2(360f, 128f)));
        }

        private static void AssertLegacyRush(ExperimentLayoutSpec layout, string conditionId)
        {
            Assert.That(layout.ExperimentType, Is.EqualTo(ExperimentType.RushPackage));
            Assert.That(layout.ConditionId, Is.EqualTo(conditionId));
            Assert.That(layout.UsesLegacyGeometry, Is.True);
        }

        private static void AssertPresentation(
            RepairAssistMode mode,
            ExperimentSpecialState state,
            float alpha,
            bool interactable,
            bool blocksRaycasts,
            string ignoredReason)
        {
            ExperimentRepairPresentation actual = ExperimentLayoutCatalog.ResolveRepairPresentation(mode, state);
            Assert.That(actual.Alpha, Is.EqualTo(alpha), mode + "/" + state);
            Assert.That(actual.Interactable, Is.EqualTo(interactable), mode + "/" + state);
            Assert.That(actual.BlocksRaycasts, Is.EqualTo(blocksRaycasts), mode + "/" + state);
            Assert.That(actual.ReservesLayout, Is.True, mode + "/" + state);
            Assert.That(actual.RejectedInputReason, Is.EqualTo(ignoredReason), mode + "/" + state);
        }

        private static void AssertSameControl(
            ExperimentControlSpec first,
            ExperimentControlSpec second,
            string context)
        {
            Assert.That(first.RectTransform.ExactlyEquals(second.RectTransform), Is.True, context);
            Assert.That(first.Style.ExactlyEquals(second.Style), Is.True, context);
        }

        private static void AssertSamePresentation(
            ExperimentRepairPresentation first,
            ExperimentRepairPresentation second,
            string context)
        {
            Assert.That(first.Alpha, Is.EqualTo(second.Alpha), context);
            Assert.That(first.Interactable, Is.EqualTo(second.Interactable), context);
            Assert.That(first.BlocksRaycasts, Is.EqualTo(second.BlocksRaycasts), context);
            Assert.That(first.ReservesLayout, Is.EqualTo(second.ReservesLayout), context);
            Assert.That(first.RejectedInputReason, Is.EqualTo(second.RejectedInputReason), context);
        }
    }
}
