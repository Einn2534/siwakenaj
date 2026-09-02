using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison.Editor
{
    /// <summary>
    /// Deterministic engineering verification for the orthogonal layout catalog.
    /// This verifies geometry and state contracts; it is not evidence of UX superiority.
    /// </summary>
    public static class InputUIComparisonVerificationRunner
    {
        public const string ReportPath =
            "Assets/Experiments/InputUIComparison/Results/Verification/layout-catalog-verification.md";

        [UnityEditor.MenuItem("Tools/しわ賢者/Input UI Comparison/Verify Orthogonal Layout Catalog")]
        public static void RunAndWriteReport()
        {
            IReadOnlyList<string> errors = ExperimentLayoutCatalog.ValidateCatalog();
            string report = BuildReport(errors);
            string directory = Path.GetDirectoryName(ReportPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Verification report directory is invalid.");
            }

            Directory.CreateDirectory(directory);
            File.WriteAllText(ReportPath, report, new UTF8Encoding(true));
            UnityEditor.AssetDatabase.ImportAsset(ReportPath, UnityEditor.ImportAssetOptions.ForceUpdate);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Input UI layout verification failed with " + errors.Count
                    + " error(s). See " + ReportPath + ".");
            }

            Debug.Log("[InputUIComparison] Orthogonal layout catalog verified: " + ReportPath);
        }

        // Batch-mode entry point:
        // -executeMethod Siwakenja.Experiments.InputUIComparison.Editor.InputUIComparisonVerificationRunner.RunFromCommandLine
        public static void RunFromCommandLine()
        {
            RunAndWriteReport();
        }

        public static string BuildReport(IReadOnlyList<string> errors = null)
        {
            errors = errors ?? ExperimentLayoutCatalog.ValidateCatalog();
            var builder = new StringBuilder(8192);
            builder.AppendLine("# Input UI layout catalog verification");
            builder.AppendLine();
            builder.AppendLine("Generated (UTC): " + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine();
            builder.AppendLine(errors.Count == 0
                ? "Result: **PASS** — catalog geometry and presentation contracts are internally consistent."
                : "Result: **FAIL** — see errors below.");
            builder.AppendLine();
            builder.AppendLine(
                "This is deterministic engineering evidence only. It does not compare human UX outcomes and does not recommend a UI.");
            builder.AppendLine();

            AppendPlacementSection(builder);
            AppendAssistSection(builder);
            AppendRushSection(builder);
            AppendResolutionSection(builder);
            AppendErrors(builder, errors);
            return builder.ToString();
        }

        private static void AppendPlacementSection(StringBuilder builder)
        {
            ExperimentLayoutSpec upper = ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.UpperFixed);
            ExperimentLayoutSpec bottom = ExperimentLayoutCatalog.CreatePlacement(PlacementCondition.BottomFixed);
            builder.AppendLine("## Placement experiment");
            builder.AppendLine();
            builder.AppendLine("| condition | vehicle row | repair RectTransform | repair state |");
            builder.AppendLine("|---|---|---|---|");
            AppendPlacementRow(builder, upper);
            AppendPlacementRow(builder, bottom);
            builder.AppendLine();
            builder.AppendLine(
                "The three vehicle controls, repair size/style, and repair presentation are exact matches; only repair anchoredPosition differs.");
            builder.AppendLine();
        }

        private static void AppendPlacementRow(StringBuilder builder, ExperimentLayoutSpec layout)
        {
            ExperimentControlSpec sports = layout.GetControl(ExperimentInput.Sports);
            ExperimentControlSpec repair = layout.GetControl(ExperimentInput.Repair);
            builder.Append("| ").Append(layout.ConditionId)
                .Append(" | centers 211/540/869 @ y190, size ")
                .Append(Vector(sports.RectTransform.SizeDelta))
                .Append(" | center ").Append(Vector(repair.RectTransform.AnchoredPosition))
                .Append(", size ").Append(Vector(repair.RectTransform.SizeDelta))
                .AppendLine(" | always visible + interactable |");
        }

        private static void AppendAssistSection(StringBuilder builder)
        {
            builder.AppendLine("## State-assist experiment");
            builder.AppendLine();
            builder.AppendLine(
                "All controls use the same RectTransforms for a selected immutable RepairAnchorMode. The table below is independent of anchor choice.");
            builder.AppendLine();
            builder.AppendLine("| condition | non-Broken | Broken | layout reserved while hidden |");
            builder.AppendLine("|---|---|---|---|");
            foreach (RepairAssistMode mode in Enum.GetValues(typeof(RepairAssistMode)))
            {
                ExperimentLayoutSpec layout = ExperimentLayoutCatalog.CreateStateAssist(
                    mode,
                    RepairAnchorMode.BottomFixed);
                ExperimentRepairPresentation normal = layout.GetRepairPresentation(ExperimentSpecialState.Normal);
                ExperimentRepairPresentation broken = layout.GetRepairPresentation(ExperimentSpecialState.Broken);
                builder.Append("| ").Append(layout.ConditionId)
                    .Append(" | ").Append(Presentation(normal))
                    .Append(" | ").Append(Presentation(broken))
                    .Append(" | ").Append(normal.ReservesLayout ? "yes" : "no")
                    .AppendLine(" |");
            }
            builder.AppendLine();
        }

        private static void AppendRushSection(StringBuilder builder)
        {
            builder.AppendLine("## Rush package presets");
            builder.AppendLine();
            builder.AppendLine("| preset | condition | anchor | assist | vehicle layout | legacy geometry |");
            builder.AppendLine("|---|---|---|---|---|---|");
            foreach (RushPackagePreset preset in Enum.GetValues(typeof(RushPackagePreset)))
            {
                ExperimentLayoutSpec layout = ExperimentLayoutCatalog.CreateRushPackage(preset);
                builder.Append("| ").Append(preset)
                    .Append(" | ").Append(layout.ConditionId)
                    .Append(" | ").Append(layout.RepairAnchorMode)
                    .Append(" | ").Append(layout.RepairAssistMode)
                    .Append(" | ").Append(layout.VehicleButtonLayoutMode)
                    .Append(" | ").Append(layout.UsesLegacyGeometry ? "yes" : "no")
                    .AppendLine(" |");
            }
            builder.AppendLine();
            builder.AppendLine(
                "Rush presets are package comparisons. Their results cannot attribute an effect to an individual layout or assist factor.");
            builder.AppendLine(
                "All three presets are retained for P00 technical rehearsal only. Human Rush testing must use only two candidates preregistered after State Assist participant-level review.");
            builder.AppendLine();
        }

        private static void AppendResolutionSection(StringBuilder builder)
        {
            builder.AppendLine("## Required resolution projections");
            builder.AppendLine();
            builder.AppendLine("| screen | Safe Area | projected 9:16 content | scale |");
            builder.AppendLine("|---|---|---|---:|");
            for (int i = 0; i < ExperimentLayoutCatalog.RequiredResolutionCases.Count; i++)
            {
                ExperimentResolutionCase resolution = ExperimentLayoutCatalog.RequiredResolutionCases[i];
                ExperimentReferenceFrameProjection projection =
                    ExperimentLayoutCatalog.ProjectReferenceFrame(resolution);
                builder.Append("| ").Append(resolution.Id)
                    .Append(" | ").Append(Rectangle(resolution.SafeArea))
                    .Append(" | ").Append(Rectangle(projection.ContentRect))
                    .Append(" | ").Append(projection.Scale.ToString("0.####", CultureInfo.InvariantCulture))
                    .AppendLine(" |");
            }
            builder.AppendLine();
            builder.AppendLine(
                "For every causal condition and retained Rush preset, all four control rects are contained by the projected content frame and do not overlap each other, the target marker, or the car-at-judgment rect.");
            builder.AppendLine();
            builder.AppendLine(
                "These are mathematical projections. Game View screenshots and Android Safe Area checks remain separate visual/P00 evidence.");
            builder.AppendLine();
        }

        private static void AppendErrors(StringBuilder builder, IReadOnlyList<string> errors)
        {
            builder.AppendLine("## Validation errors");
            builder.AppendLine();
            if (errors.Count == 0)
            {
                builder.AppendLine("None.");
                return;
            }

            for (int i = 0; i < errors.Count; i++)
            {
                builder.Append("- ").AppendLine(errors[i]);
            }
        }

        private static string Presentation(ExperimentRepairPresentation state)
        {
            return "alpha=" + state.Alpha.ToString("0", CultureInfo.InvariantCulture)
                + ", interactable=" + state.Interactable.ToString().ToLowerInvariant()
                + ", blocksRaycasts=" + state.BlocksRaycasts.ToString().ToLowerInvariant();
        }

        private static string Vector(Vector2 value)
        {
            return value.x.ToString("0.##", CultureInfo.InvariantCulture)
                + "x" + value.y.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string Rectangle(Rect value)
        {
            return "(" + value.x.ToString("0.##", CultureInfo.InvariantCulture)
                + "," + value.y.ToString("0.##", CultureInfo.InvariantCulture)
                + ") " + value.width.ToString("0.##", CultureInfo.InvariantCulture)
                + "x" + value.height.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
