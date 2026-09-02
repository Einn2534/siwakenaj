using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison
{
    /// <summary>
    /// Immutable RectTransform data in the experiment's 1080 x 1920 reference frame.
    /// All current controls use a point anchor rather than stretch anchors.
    /// </summary>
    public readonly struct ExperimentRectTransformSpec
    {
        public ExperimentRectTransformSpec(
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
            Pivot = pivot;
            AnchoredPosition = anchoredPosition;
            SizeDelta = sizeDelta;
        }

        public Vector2 AnchorMin { get; }
        public Vector2 AnchorMax { get; }
        public Vector2 Pivot { get; }
        public Vector2 AnchoredPosition { get; }
        public Vector2 SizeDelta { get; }

        public Rect ToReferenceRect(Vector2 referenceResolution)
        {
            if (AnchorMin != AnchorMax)
            {
                throw new InvalidOperationException("Experiment controls must use a point anchor.");
            }

            Vector2 anchorPoint = Vector2.Scale(referenceResolution, AnchorMin);
            Vector2 pivotPoint = anchorPoint + AnchoredPosition;
            return new Rect(
                pivotPoint.x - SizeDelta.x * Pivot.x,
                pivotPoint.y - SizeDelta.y * Pivot.y,
                SizeDelta.x,
                SizeDelta.y);
        }

        public bool ExactlyEquals(ExperimentRectTransformSpec other)
        {
            return AnchorMin == other.AnchorMin
                && AnchorMax == other.AnchorMax
                && Pivot == other.Pivot
                && AnchoredPosition == other.AnchoredPosition
                && SizeDelta == other.SizeDelta;
        }

        public void ApplyTo(UnityEngine.RectTransform target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.anchorMin = AnchorMin;
            target.anchorMax = AnchorMax;
            target.pivot = Pivot;
            target.sizeDelta = SizeDelta;
            target.anchoredPosition = AnchoredPosition;
        }
    }

    /// <summary>
    /// Visual properties that must remain identical when layout or assist is the sole factor.
    /// The ids describe the existing neutral uGUI image treatment; no production sprite is implied.
    /// </summary>
    public readonly struct ExperimentControlStyleSpec
    {
        public ExperimentControlStyleSpec(
            string visualStyleId,
            string label,
            int fontSize,
            Color32 normalBackground,
            Color32 disabledBackground,
            Color32 normalLabel,
            Color32 disabledLabel,
            Color32 outline,
            Vector2 outlineDistance,
            float pressedScale)
        {
            VisualStyleId = visualStyleId;
            Label = label;
            FontSize = fontSize;
            NormalBackground = normalBackground;
            DisabledBackground = disabledBackground;
            NormalLabel = normalLabel;
            DisabledLabel = disabledLabel;
            Outline = outline;
            OutlineDistance = outlineDistance;
            PressedScale = pressedScale;
        }

        public string VisualStyleId { get; }
        public string Label { get; }
        public int FontSize { get; }
        public Color32 NormalBackground { get; }
        public Color32 DisabledBackground { get; }
        public Color32 NormalLabel { get; }
        public Color32 DisabledLabel { get; }
        public Color32 Outline { get; }
        public Vector2 OutlineDistance { get; }
        public float PressedScale { get; }

        public bool ExactlyEquals(ExperimentControlStyleSpec other)
        {
            return string.Equals(VisualStyleId, other.VisualStyleId, StringComparison.Ordinal)
                && string.Equals(Label, other.Label, StringComparison.Ordinal)
                && FontSize == other.FontSize
                && NormalBackground.Equals(other.NormalBackground)
                && DisabledBackground.Equals(other.DisabledBackground)
                && NormalLabel.Equals(other.NormalLabel)
                && DisabledLabel.Equals(other.DisabledLabel)
                && Outline.Equals(other.Outline)
                && OutlineDistance == other.OutlineDistance
                && PressedScale.Equals(other.PressedScale);
        }

        public void ApplyColors(
            UnityEngine.UI.Image image,
            UnityEngine.UI.Text label,
            bool interactable)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            image.color = interactable ? NormalBackground : DisabledBackground;
            if (label != null)
            {
                label.text = Label;
                label.fontSize = FontSize;
                label.color = interactable ? NormalLabel : DisabledLabel;
            }
        }
    }

    public sealed class ExperimentControlSpec
    {
        internal ExperimentControlSpec(
            ExperimentInput input,
            ExperimentRectTransformSpec rectTransform,
            ExperimentControlStyleSpec style)
        {
            Input = input;
            RectTransform = rectTransform;
            Style = style;
        }

        public ExperimentInput Input { get; }
        public ExperimentRectTransformSpec RectTransform { get; }
        public ExperimentControlStyleSpec Style { get; }
    }

    /// <summary>
    /// CanvasGroup-compatible state. ReservesLayout is always true: BrokenOnly hides the
    /// control visually and from raycasts without deleting its RectTransform.
    /// </summary>
    public readonly struct ExperimentRepairPresentation
    {
        public ExperimentRepairPresentation(
            float alpha,
            bool interactable,
            bool blocksRaycasts,
            bool reservesLayout,
            string rejectedInputReason)
        {
            Alpha = alpha;
            Interactable = interactable;
            BlocksRaycasts = blocksRaycasts;
            ReservesLayout = reservesLayout;
            RejectedInputReason = rejectedInputReason;
        }

        public float Alpha { get; }
        public bool Visible => Alpha > 0f;
        public bool Interactable { get; }
        public bool BlocksRaycasts { get; }
        public bool ReservesLayout { get; }
        public string RejectedInputReason { get; }

        public void ApplyTo(UnityEngine.CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) throw new ArgumentNullException(nameof(canvasGroup));
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = Alpha;
            canvasGroup.interactable = Interactable;
            canvasGroup.blocksRaycasts = BlocksRaycasts;
        }
    }

    public sealed class ExperimentLayoutSpec
    {
        private readonly ReadOnlyCollection<ExperimentControlSpec> _controls;

        internal ExperimentLayoutSpec(
            ExperimentType experimentType,
            string conditionId,
            RepairAnchorMode repairAnchorMode,
            RepairAssistMode repairAssistMode,
            VehicleButtonLayoutMode vehicleButtonLayoutMode,
            bool usesLegacyGeometry,
            ExperimentControlSpec[] controls)
        {
            ExperimentType = experimentType;
            ConditionId = conditionId;
            RepairAnchorMode = repairAnchorMode;
            RepairAssistMode = repairAssistMode;
            VehicleButtonLayoutMode = vehicleButtonLayoutMode;
            UsesLegacyGeometry = usesLegacyGeometry;
            _controls = Array.AsReadOnly(controls ?? throw new ArgumentNullException(nameof(controls)));
        }

        public ExperimentType ExperimentType { get; }
        public string ConditionId { get; }
        public RepairAnchorMode RepairAnchorMode { get; }
        public RepairAssistMode RepairAssistMode { get; }
        public VehicleButtonLayoutMode VehicleButtonLayoutMode { get; }
        public bool UsesLegacyGeometry { get; }
        public IReadOnlyList<ExperimentControlSpec> Controls => _controls;

        public ExperimentControlSpec GetControl(ExperimentInput input)
        {
            for (int i = 0; i < _controls.Count; i++)
            {
                if (_controls[i].Input == input) return _controls[i];
            }

            throw new KeyNotFoundException("No layout control for " + input + ".");
        }

        public ExperimentRepairPresentation GetRepairPresentation(ExperimentSpecialState state)
        {
            return ExperimentLayoutCatalog.ResolveRepairPresentation(RepairAssistMode, state);
        }
    }

    public readonly struct ExperimentResolutionCase
    {
        public ExperimentResolutionCase(string id, int width, int height, Rect safeArea)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Id = id;
            Width = width;
            Height = height;
            SafeArea = safeArea;
        }

        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public Rect SafeArea { get; }
    }

    public readonly struct ExperimentReferenceFrameProjection
    {
        public ExperimentReferenceFrameProjection(Rect screenRect, Rect safeArea, Rect contentRect, float scale)
        {
            ScreenRect = screenRect;
            SafeArea = safeArea;
            ContentRect = contentRect;
            Scale = scale;
        }

        public Rect ScreenRect { get; }
        public Rect SafeArea { get; }
        public Rect ContentRect { get; }
        public float Scale { get; }

        public Rect Project(Rect referenceRect)
        {
            return new Rect(
                ContentRect.xMin + referenceRect.xMin * Scale,
                ContentRect.yMin + referenceRect.yMin * Scale,
                referenceRect.width * Scale,
                referenceRect.height * Scale);
        }
    }

    /// <summary>
    /// Single source of truth for orthogonal UI geometry. Placement changes only the
    /// repair center; StateAssist changes only CanvasGroup/input-acceptance state.
    /// Legacy A/B geometry is available only through Rush package presets.
    /// </summary>
    public static class ExperimentLayoutCatalog
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        public static readonly Rect TargetMarkerReferenceRect = new Rect(5f, 995f, 290f, 250f);
        public static readonly Rect CarAtJudgmentReferenceRect = new Rect(15f, 1025f, 270f, 190f);

        private static readonly Vector2 BottomLeftAnchor = Vector2.zero;
        private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 CanonicalVehicleSize = new Vector2(308f, 315f);
        private static readonly Vector2 CanonicalRepairSize = new Vector2(470f, 112f);
        // Both Placement candidates are fixed screen coordinates. "UpperFixed"
        // deliberately avoids claiming that y=800 follows the moving target.
        private static readonly Vector2 UpperFixedRepairCenter = new Vector2(540f, 800f);
        private static readonly Vector2 BottomFixedRepairCenter = new Vector2(540f, 462f);

        private static readonly Color32 NormalBackground = new Color32(220, 224, 229, 255);
        private static readonly Color32 DisabledBackground = new Color32(225, 227, 230, 150);
        private static readonly Color32 NormalLabel = new Color32(31, 35, 40, 255);
        private static readonly Color32 DisabledLabel = new Color32(120, 124, 130, 255);
        private static readonly Color32 OutlineColor = new Color32(110, 116, 124, 255);

        private static readonly ReadOnlyCollection<ExperimentResolutionCase> ResolutionCases =
            Array.AsReadOnly(new[]
            {
                FullScreenResolution("1080x1920", 1080, 1920),
                FullScreenResolution("1080x2340", 1080, 2340),
                FullScreenResolution("720x1600", 720, 1600),
            });

        public static IReadOnlyList<ExperimentResolutionCase> RequiredResolutionCases => ResolutionCases;

        public static ExperimentLayoutSpec CreatePlacement(PlacementCondition condition)
        {
            RepairAnchorMode anchorMode;
            string conditionId;
            switch (condition)
            {
                case PlacementCondition.UpperFixed:
                    anchorMode = RepairAnchorMode.UpperFixed;
                    conditionId = ExperimentConditionIds.PlacementUpperFixed;
                    break;
                case PlacementCondition.BottomFixed:
                    anchorMode = RepairAnchorMode.BottomFixed;
                    conditionId = ExperimentConditionIds.PlacementBottomFixed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
            }

            return CreateStandardThree(
                ExperimentType.Placement,
                conditionId,
                anchorMode,
                RepairAssistMode.AlwaysActive,
                false,
                false);
        }

        public static ExperimentLayoutSpec CreateStateAssist(
            RepairAssistMode assistMode,
            RepairAnchorMode anchorMode)
        {
            return CreateStandardThree(
                ExperimentType.StateAssist,
                AssistConditionId(assistMode),
                anchorMode,
                assistMode,
                false,
                false);
        }

        public static ExperimentLayoutSpec CreateRushPackage(RushPackagePreset preset)
        {
            switch (preset)
            {
                case RushPackagePreset.CurrentEquivalent:
                    return CreateStandardThree(
                        ExperimentType.RushPackage,
                        ExperimentConditionIds.RushCurrentEquivalent,
                        RepairAnchorMode.BottomFixed,
                        RepairAssistMode.AlwaysActive,
                        true,
                        false);
                case RushPackagePreset.FixedFourEquivalent:
                    return CreateFixedFour(
                        ExperimentConditionIds.RushFixedFourEquivalent,
                        RepairAnchorMode.BottomFixed,
                        RepairAssistMode.DisabledWhenNormal,
                        true);
                case RushPackagePreset.ContextualRepairEquivalent:
                    return CreateStandardThree(
                        ExperimentType.RushPackage,
                        ExperimentConditionIds.RushContextualRepairEquivalent,
                        RepairAnchorMode.UpperFixed,
                        RepairAssistMode.BrokenOnly,
                        true,
                        true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }
        }

        public static ExperimentLayoutSpec CreateRushPackage(
            RepairAnchorMode anchorMode,
            RepairAssistMode assistMode,
            VehicleButtonLayoutMode vehicleButtonLayoutMode)
        {
            string conditionId = "Rush_Custom_" + anchorMode + "_" + assistMode + "_" + vehicleButtonLayoutMode;
            return vehicleButtonLayoutMode == VehicleButtonLayoutMode.FixedFour
                ? CreateFixedFour(conditionId, anchorMode, assistMode, false)
                : CreateStandardThree(
                    ExperimentType.RushPackage,
                    conditionId,
                    anchorMode,
                    assistMode,
                    false,
                    false);
        }

        public static ExperimentRepairPresentation ResolveRepairPresentation(
            RepairAssistMode assistMode,
            ExperimentSpecialState state)
        {
            bool broken = state == ExperimentSpecialState.Broken;
            switch (assistMode)
            {
                case RepairAssistMode.AlwaysActive:
                    return new ExperimentRepairPresentation(
                        1f, true, true, true, ExperimentIgnoredReasons.None);
                case RepairAssistMode.DisabledWhenNormal:
                    return broken
                        ? new ExperimentRepairPresentation(1f, true, true, true, ExperimentIgnoredReasons.None)
                        : new ExperimentRepairPresentation(1f, false, true, true, ExperimentIgnoredReasons.Disabled);
                case RepairAssistMode.BrokenOnly:
                    return broken
                        ? new ExperimentRepairPresentation(1f, true, true, true, ExperimentIgnoredReasons.None)
                        : new ExperimentRepairPresentation(0f, false, false, true, ExperimentIgnoredReasons.HiddenOrEmpty);
                default:
                    throw new ArgumentOutOfRangeException(nameof(assistMode), assistMode, null);
            }
        }

        public static ExperimentReferenceFrameProjection ProjectReferenceFrame(ExperimentResolutionCase resolution)
        {
            Rect screen = new Rect(0f, 0f, resolution.Width, resolution.Height);
            Rect safe = resolution.SafeArea;
            if (!Contains(screen, safe, 0.01f))
            {
                throw new ArgumentException("Safe Area must be inside the screen.", nameof(resolution));
            }

            float scale = Mathf.Min(
                safe.width / ReferenceResolution.x,
                safe.height / ReferenceResolution.y);
            float width = ReferenceResolution.x * scale;
            float height = ReferenceResolution.y * scale;
            var content = new Rect(
                safe.xMin + (safe.width - width) * 0.5f,
                safe.yMin + (safe.height - height) * 0.5f,
                width,
                height);
            return new ExperimentReferenceFrameProjection(screen, safe, content, scale);
        }

        public static Rect ProjectControlRect(
            ExperimentLayoutSpec layout,
            ExperimentInput input,
            ExperimentResolutionCase resolution)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            Rect referenceRect = layout.GetControl(input).RectTransform.ToReferenceRect(ReferenceResolution);
            return ProjectReferenceFrame(resolution).Project(referenceRect);
        }

        public static bool Overlaps(Rect first, Rect second, float clearance = 0f)
        {
            return first.xMin < second.xMax + clearance
                && first.xMax > second.xMin - clearance
                && first.yMin < second.yMax + clearance
                && first.yMax > second.yMin - clearance;
        }

        public static bool Contains(Rect outer, Rect inner, float tolerance = 0f)
        {
            return inner.xMin >= outer.xMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.yMax <= outer.yMax + tolerance;
        }

        public static IReadOnlyList<string> ValidateCatalog()
        {
            var errors = new List<string>();
            ValidatePlacementOrthogonality(errors);
            ValidateAssistOrthogonality(errors);

            var layouts = new List<ExperimentLayoutSpec>
            {
                CreatePlacement(PlacementCondition.UpperFixed),
                CreatePlacement(PlacementCondition.BottomFixed),
                CreateRushPackage(RushPackagePreset.CurrentEquivalent),
                CreateRushPackage(RushPackagePreset.FixedFourEquivalent),
                CreateRushPackage(RushPackagePreset.ContextualRepairEquivalent),
            };
            foreach (RepairAnchorMode anchor in Enum.GetValues(typeof(RepairAnchorMode)))
            {
                foreach (RepairAssistMode assist in Enum.GetValues(typeof(RepairAssistMode)))
                {
                    layouts.Add(CreateStateAssist(assist, anchor));
                }
            }

            for (int i = 0; i < layouts.Count; i++)
            {
                for (int resolutionIndex = 0; resolutionIndex < ResolutionCases.Count; resolutionIndex++)
                {
                    ValidateLayoutAtResolution(layouts[i], ResolutionCases[resolutionIndex], errors);
                }
            }

            return errors.AsReadOnly();
        }

        private static void ValidatePlacementOrthogonality(List<string> errors)
        {
            ExperimentLayoutSpec upper = CreatePlacement(PlacementCondition.UpperFixed);
            ExperimentLayoutSpec bottom = CreatePlacement(PlacementCondition.BottomFixed);
            foreach (ExperimentInput input in VehicleInputs())
            {
                RequireSameControl(upper, bottom, input, "Placement vehicle controls", errors);
            }

            ExperimentControlSpec upperRepair = upper.GetControl(ExperimentInput.Repair);
            ExperimentControlSpec bottomRepair = bottom.GetControl(ExperimentInput.Repair);
            ExperimentRectTransformSpec upperRect = upperRepair.RectTransform;
            ExperimentRectTransformSpec bottomRect = bottomRepair.RectTransform;
            if (upperRect.AnchorMin != bottomRect.AnchorMin
                || upperRect.AnchorMax != bottomRect.AnchorMax
                || upperRect.Pivot != bottomRect.Pivot
                || upperRect.SizeDelta != bottomRect.SizeDelta)
            {
                errors.Add("Placement repair RectTransform differs in more than anchoredPosition.");
            }
            if (upperRect.AnchoredPosition != UpperFixedRepairCenter
                || bottomRect.AnchoredPosition != BottomFixedRepairCenter)
            {
                errors.Add("Placement repair centers do not match the audited 800/462 reference positions.");
            }
            if (!upperRepair.Style.ExactlyEquals(bottomRepair.Style))
            {
                errors.Add("Placement repair style differs between conditions.");
            }

            foreach (ExperimentSpecialState state in Enum.GetValues(typeof(ExperimentSpecialState)))
            {
                ExperimentRepairPresentation upperState = upper.GetRepairPresentation(state);
                ExperimentRepairPresentation bottomState = bottom.GetRepairPresentation(state);
                if (!PresentationExactlyEquals(upperState, bottomState)
                    || !upperState.Visible
                    || !upperState.Interactable)
                {
                    errors.Add("Placement repair state differs or is not always active for " + state + ".");
                }
            }
        }

        private static void ValidateAssistOrthogonality(List<string> errors)
        {
            foreach (RepairAnchorMode anchor in Enum.GetValues(typeof(RepairAnchorMode)))
            {
                ExperimentLayoutSpec baseline = CreateStateAssist(RepairAssistMode.AlwaysActive, anchor);
                foreach (RepairAssistMode assist in Enum.GetValues(typeof(RepairAssistMode)))
                {
                    ExperimentLayoutSpec candidate = CreateStateAssist(assist, anchor);
                    foreach (ExperimentInput input in AllInputs())
                    {
                        RequireSameControl(baseline, candidate, input, "Assist controls", errors);
                    }
                }

                ExperimentRepairPresentation hidden = CreateStateAssist(
                    RepairAssistMode.BrokenOnly,
                    anchor).GetRepairPresentation(ExperimentSpecialState.Normal);
                if (hidden.Alpha != 0f
                    || hidden.Interactable
                    || hidden.BlocksRaycasts
                    || !hidden.ReservesLayout)
                {
                    errors.Add("BrokenOnly does not preserve its Rect with CanvasGroup hidden semantics at " + anchor + ".");
                }
            }
        }

        private static void ValidateLayoutAtResolution(
            ExperimentLayoutSpec layout,
            ExperimentResolutionCase resolution,
            List<string> errors)
        {
            ExperimentReferenceFrameProjection projection = ProjectReferenceFrame(resolution);
            var projected = new Dictionary<ExperimentInput, Rect>();
            for (int i = 0; i < layout.Controls.Count; i++)
            {
                ExperimentControlSpec control = layout.Controls[i];
                Rect rect = projection.Project(control.RectTransform.ToReferenceRect(ReferenceResolution));
                projected[control.Input] = rect;
                if (!Contains(projection.ContentRect, rect, 0.05f))
                {
                    errors.Add(layout.ConditionId + " / " + resolution.Id + " clips " + control.Input + ".");
                }
            }

            ExperimentInput[] inputs = AllInputs();
            for (int left = 0; left < inputs.Length; left++)
            {
                for (int right = left + 1; right < inputs.Length; right++)
                {
                    if (Overlaps(projected[inputs[left]], projected[inputs[right]]))
                    {
                        errors.Add(layout.ConditionId + " / " + resolution.Id + " overlaps "
                            + inputs[left] + " and " + inputs[right] + ".");
                    }
                }
            }

            Rect projectedTarget = projection.Project(TargetMarkerReferenceRect);
            Rect projectedCar = projection.Project(CarAtJudgmentReferenceRect);
            Rect projectedRepair = projected[ExperimentInput.Repair];
            if (Overlaps(projectedRepair, projectedTarget) || Overlaps(projectedRepair, projectedCar))
            {
                errors.Add(layout.ConditionId + " / " + resolution.Id + " repair overlaps the target region.");
            }
        }

        private static ExperimentLayoutSpec CreateStandardThree(
            ExperimentType experimentType,
            string conditionId,
            RepairAnchorMode anchorMode,
            RepairAssistMode assistMode,
            bool usesLegacyGeometry,
            bool legacyContextualRepairGeometry)
        {
            Vector2 repairCenter = RepairCenter(anchorMode);
            Vector2 repairSize = legacyContextualRepairGeometry
                ? new Vector2(360f, 128f)
                : CanonicalRepairSize;
            return new ExperimentLayoutSpec(
                experimentType,
                conditionId,
                anchorMode,
                assistMode,
                VehicleButtonLayoutMode.StandardThree,
                usesLegacyGeometry,
                new[]
                {
                    Control(ExperimentInput.Sports, "スポーツ", 29, new Vector2(211f, 190f), CanonicalVehicleSize),
                    Control(ExperimentInput.Compact, "コンパクト", 29, new Vector2(540f, 190f), CanonicalVehicleSize),
                    Control(ExperimentInput.LightTruck, "トラック", 29, new Vector2(869f, 190f), CanonicalVehicleSize),
                    Control(ExperimentInput.Repair, "整備", 27, repairCenter, repairSize),
                });
        }

        private static ExperimentLayoutSpec CreateFixedFour(
            string conditionId,
            RepairAnchorMode anchorMode,
            RepairAssistMode assistMode,
            bool usesLegacyGeometry)
        {
            Vector2 size = new Vector2(240f, 260f);
            return new ExperimentLayoutSpec(
                ExperimentType.RushPackage,
                conditionId,
                anchorMode,
                assistMode,
                VehicleButtonLayoutMode.FixedFour,
                usesLegacyGeometry,
                new[]
                {
                    Control(ExperimentInput.Sports, "スポーツ", 29, new Vector2(140f, 175f), size),
                    Control(ExperimentInput.Compact, "コンパクト", 29, new Vector2(405f, 175f), size),
                    Control(ExperimentInput.LightTruck, "トラック", 29, new Vector2(675f, 175f), size),
                    Control(ExperimentInput.Repair, "整備", 29, new Vector2(940f, 175f), size),
                });
        }

        private static ExperimentControlSpec Control(
            ExperimentInput input,
            string label,
            int fontSize,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var rect = new ExperimentRectTransformSpec(
                BottomLeftAnchor,
                BottomLeftAnchor,
                CenterPivot,
                anchoredPosition,
                size);
            var style = new ExperimentControlStyleSpec(
                "neutral_ugui_control_v2",
                label,
                fontSize,
                NormalBackground,
                DisabledBackground,
                NormalLabel,
                DisabledLabel,
                OutlineColor,
                new Vector2(2f, -2f),
                0.96f);
            return new ExperimentControlSpec(input, rect, style);
        }

        private static Vector2 RepairCenter(RepairAnchorMode anchorMode)
        {
            switch (anchorMode)
            {
                case RepairAnchorMode.UpperFixed: return UpperFixedRepairCenter;
                case RepairAnchorMode.BottomFixed: return BottomFixedRepairCenter;
                default: throw new ArgumentOutOfRangeException(nameof(anchorMode), anchorMode, null);
            }
        }

        private static string AssistConditionId(RepairAssistMode assistMode)
        {
            switch (assistMode)
            {
                case RepairAssistMode.AlwaysActive: return ExperimentConditionIds.AssistAlwaysActive;
                case RepairAssistMode.DisabledWhenNormal: return ExperimentConditionIds.AssistDisabledWhenNormal;
                case RepairAssistMode.BrokenOnly: return ExperimentConditionIds.AssistBrokenOnly;
                default: throw new ArgumentOutOfRangeException(nameof(assistMode), assistMode, null);
            }
        }

        private static ExperimentResolutionCase FullScreenResolution(string id, int width, int height)
        {
            return new ExperimentResolutionCase(id, width, height, new Rect(0f, 0f, width, height));
        }

        private static void RequireSameControl(
            ExperimentLayoutSpec first,
            ExperimentLayoutSpec second,
            ExperimentInput input,
            string context,
            List<string> errors)
        {
            ExperimentControlSpec left = first.GetControl(input);
            ExperimentControlSpec right = second.GetControl(input);
            if (!left.RectTransform.ExactlyEquals(right.RectTransform)
                || !left.Style.ExactlyEquals(right.Style))
            {
                errors.Add(context + " differ for " + input + ": "
                    + first.ConditionId + " vs " + second.ConditionId + ".");
            }
        }

        private static bool PresentationExactlyEquals(
            ExperimentRepairPresentation first,
            ExperimentRepairPresentation second)
        {
            return first.Alpha.Equals(second.Alpha)
                && first.Interactable == second.Interactable
                && first.BlocksRaycasts == second.BlocksRaycasts
                && first.ReservesLayout == second.ReservesLayout
                && string.Equals(first.RejectedInputReason, second.RejectedInputReason, StringComparison.Ordinal);
        }

        private static ExperimentInput[] VehicleInputs()
        {
            return new[] { ExperimentInput.Sports, ExperimentInput.Compact, ExperimentInput.LightTruck };
        }

        private static ExperimentInput[] AllInputs()
        {
            return new[]
            {
                ExperimentInput.Sports,
                ExperimentInput.Compact,
                ExperimentInput.LightTruck,
                ExperimentInput.Repair,
            };
        }
    }
}
