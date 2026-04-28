#!/usr/bin/env python
"""Slice generated SIWAKENJA UI sheets into transparent Unity-ready PNGs.

The source images from image generation have a checkerboard baked into the
background instead of a real alpha channel. This script removes that background,
groups nearby foreground pixels into individual UI assets, writes categorized
sprite PNGs, and records every crop in a manifest so the process can be audited.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
from collections import namedtuple
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple

import cv2
import numpy as np
from PIL import Image


SOURCE_GLOB = r"C:/Users/einn/Downloads/ChatGPT Image 2026年4月2*.png"
OUTPUT_ROOT = Path("Assets/Art/UI")
SPRITE_ROOT_NAME = "Sprites"
SOURCE_ROOT_NAME = "_SourceSheets"
MANIFEST_NAME = "ui_slice_manifest.json"

MIN_COMPONENT_AREA = 400
MIN_COMPONENT_SIZE = 20
DEFAULT_PADDING = 12
OUTPUT_SAFE_BORDER = 2
DEFAULT_GROUP_DILATION = 25
BACKGROUND_BORDER_SAMPLE = 24
BACKGROUND_DISTANCE_THRESHOLD = 20
NEUTRAL_MIN_VALUE = 220
NEUTRAL_MAX_CHANNEL_DELTA = 18
CHECKER_STD_WINDOW = 37
CHECKER_STD_THRESHOLD = 3.0


AssetSpec = namedtuple("AssetSpec", "category name")
DerivedSlice = namedtuple("DerivedSlice", "category name rect")


class SheetPlan:
    def __init__(
        self,
        default_category: str,
        specs: Optional[Sequence[AssetSpec]] = None,
        group_dilation: int = DEFAULT_GROUP_DILATION,
    ) -> None:
        self.default_category = default_category
        self.specs = list(specs or [])
        self.group_dilation = group_dilation


def spec(category: str, name: str) -> AssetSpec:
    return AssetSpec(category, name)


SHEET_PLANS: Dict[str, SheetPlan] = {
    "17_26_44 (3)": SheetPlan(
        "Result",
        [
            spec("HUD", "ui_title_logo_full"),
            spec("Buttons", "ui_button_start_yellow"),
            spec("StageSelect", "ui_stage_select_panel_comp"),
            spec("Result", "ui_result_panel_comp"),
            spec("Result", "ui_gameover_panel_comp"),
            spec("Buttons", "ui_result_buttons_stack_comp"),
            spec("StageSelect", "ui_stage_card_comp"),
            spec("HUD", "ui_hud_speech_bubble"),
        ],
    ),
    "17_26_44 (4)": SheetPlan("Icons"),
    "17_26_44 (2)": SheetPlan("Icons"),
    "21_57_51 (1)": SheetPlan(
        "HUD",
        [
            spec("HUD", "ui_hud_score_panel"),
            spec("HUD", "ui_hud_heart_panel_empty"),
            spec("HUD", "ui_hud_miss_badge"),
            spec("HUD", "ui_hud_stage_label"),
            spec("Buttons", "ui_button_pause"),
            spec("HUD", "ui_countdown_ready"),
            spec("HUD", "ui_countdown_3"),
            spec("HUD", "ui_countdown_2"),
            spec("HUD", "ui_countdown_1"),
            spec("HUD", "ui_countdown_go"),
            spec("HUD", "ui_judge_good_comp"),
            spec("HUD", "ui_judge_ok_comp"),
            spec("HUD", "ui_judge_miss_comp"),
            spec("HUD", "ui_howto_arrow_flow"),
        ],
    ),
    "21_57_52 (4)": SheetPlan(
        "VehicleSelect",
        [
            spec("VehicleSelect", "ui_vehicle_card_green_empty"),
            spec("VehicleSelect", "ui_vehicle_card_blue_empty"),
            spec("VehicleSelect", "ui_vehicle_card_red_empty"),
            spec("VehicleSelect", "ui_vehicle_icon_light_truck"),
            spec("VehicleSelect", "ui_vehicle_icon_compact_car"),
            spec("VehicleSelect", "ui_vehicle_icon_sports_car"),
            spec("VehicleSelect", "ui_vehicle_label_light_truck"),
            spec("VehicleSelect", "ui_vehicle_label_compact_car"),
            spec("VehicleSelect", "ui_vehicle_label_sports_car"),
            spec("VehicleSelect", "ui_vehicle_label_green_blank"),
            spec("VehicleSelect", "ui_vehicle_label_blue_blank"),
            spec("VehicleSelect", "ui_vehicle_label_red_blank"),
            spec("VehicleSelect", "ui_vehicle_card_locked_dark"),
            spec("VehicleSelect", "ui_vehicle_card_locked_gray_left"),
            spec("VehicleSelect", "ui_vehicle_card_locked_gray_right"),
            spec("Icons", "ui_icon_lock_vehicle_badge"),
        ],
    ),
    "21_57_52 (5)": SheetPlan("Result"),
    "21_57_52 (6)": SheetPlan("StageSelect"),
    "21_57_52 (7)": SheetPlan("Icons"),
    "01_31_27 (1)": SheetPlan("Result"),
    "01_31_28 (2)": SheetPlan(
        "Settings",
        [
            spec("Settings", "ui_settings_panel_bg"),
            spec("Settings", "ui_settings_title"),
            spec("Settings", "ui_settings_knob_bgm"),
            spec("Settings", "ui_settings_slider_track_bgm"),
            spec("Settings", "ui_settings_label_bgm"),
            spec("Settings", "ui_settings_knob_se"),
            spec("Settings", "ui_settings_slider_track_se"),
            spec("Settings", "ui_settings_label_se"),
            spec("Settings", "ui_settings_toggle_on"),
            spec("Settings", "ui_settings_toggle_off"),
            spec("Settings", "ui_settings_label_vibe"),
            spec("Settings", "ui_loading_spinner"),
            spec("Settings", "ui_loading_text"),
            spec("Buttons", "ui_button_back_small"),
            spec("Settings", "ui_loading_dot_large"),
            spec("Settings", "ui_loading_dot_small"),
        ],
        group_dilation=25,
    ),
    "01_31_28 (3)": SheetPlan(
        "Effects",
        [
            spec("Badges", "ui_badge_new_burst"),
            spec("Icons", "ui_icon_lock_open"),
            spec("Icons", "ui_icon_lock_closed"),
            spec("Buttons", "ui_button_unlock_blue"),
            spec("Effects", "ui_effect_unlock_burst"),
            spec("Effects", "ui_effect_unlock_glow_round"),
            spec("Effects", "ui_effect_light_streak"),
            spec("Effects", "ui_effect_sparkle_trail"),
            spec("Effects", "ui_effect_highlight_frame_blue"),
            spec("Buttons", "ui_button_arrow_left_large"),
            spec("Buttons", "ui_button_arrow_right_large"),
            spec("Buttons", "ui_button_arrow_left_small"),
            spec("Buttons", "ui_button_arrow_right_small"),
        ],
    ),
    "01_31_30 (4)": SheetPlan(
        "Buttons",
        [
            spec("Buttons", "ui_button_green_normal"),
            spec("Buttons", "ui_button_blue_normal"),
            spec("Buttons", "ui_button_red_normal"),
            spec("Buttons", "ui_button_green_pressed_selected"),
            spec("Buttons", "ui_button_blue_pressed_selected"),
            spec("Buttons", "ui_button_red_pressed_selected"),
            spec("HUD", "ui_heart_full_01"),
            spec("HUD", "ui_heart_full_02"),
            spec("HUD", "ui_heart_full_03"),
            spec("HUD", "ui_heart_empty"),
        ],
        group_dilation=25,
    ),
    "01_31_31 (5)": SheetPlan("StageSelect"),
    "01_31_31 (6)": SheetPlan(
        "HUD",
        [
            spec("Effects", "ui_judge_good_burst_bg"),
            spec("Effects", "ui_judge_ok_burst_bg"),
            spec("Effects", "ui_judge_miss_burst_bg"),
            spec("HUD", "ui_judge_ok_text"),
            spec("HUD", "ui_judge_good_text"),
            spec("HUD", "ui_judge_miss_text"),
            spec("Icons", "ui_icon_warning_triangle"),
            spec("Effects", "ui_effect_good_star_large"),
            spec("Effects", "ui_effect_ok_star_large"),
            spec("Effects", "ui_effect_good_accent_slashes"),
            spec("Effects", "ui_effect_ok_accent_slashes"),
            spec("Effects", "ui_effect_good_star_small"),
            spec("Effects", "ui_effect_ok_star_small"),
        ],
    ),
}


DERIVED_SLICES: Dict[str, List[DerivedSlice]] = {
    "ui_stageselect_sheet15_item03": [
        DerivedSlice("StageSelect", "ui_stage_thumb_city", (0, 0, 229, 271)),
        DerivedSlice("StageSelect", "ui_stage_thumb_overpass", (229, 0, 229, 271)),
        DerivedSlice("StageSelect", "ui_stage_thumb_crane", (458, 0, 228, 271)),
    ],
    "ui_stageselect_sheet15_item06": [
        DerivedSlice("StageSelect", "ui_stage_star_empty", (0, 0, 143, 167)),
    ],
    "ui_stageselect_sheet21_item18": [
        DerivedSlice("StageSelect", "ui_stage_star_filled", (0, 0, 137, 140)),
    ],
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--source-glob",
        action="append",
        default=None,
        help="Glob for source sheets. Can be passed multiple times.",
    )
    parser.add_argument(
        "--output-root",
        default=str(OUTPUT_ROOT),
        help="Unity asset output root. Defaults to Assets/Art/UI.",
    )
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Delete prior generated UI sprite/source folders before writing.",
    )
    parser.add_argument(
        "--padding",
        type=int,
        default=DEFAULT_PADDING,
        help="Transparent padding, in pixels, added around every crop.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output_root = Path(args.output_root)
    source_root = output_root / SOURCE_ROOT_NAME
    sprite_root = output_root / SPRITE_ROOT_NAME
    manifest_path = output_root / MANIFEST_NAME

    if args.clean:
        clean_generated_dirs(output_root, source_root, sprite_root, manifest_path)

    source_root.mkdir(parents=True, exist_ok=True)
    sprite_root.mkdir(parents=True, exist_ok=True)
    ensure_folder_meta(output_root)
    ensure_folder_meta(source_root)
    ensure_folder_meta(sprite_root)

    sources = collect_sources(args.source_glob or [SOURCE_GLOB])
    manifest = {
        "version": 1,
        "sourceGlobs": args.source_glob or [SOURCE_GLOB],
        "outputRoot": unity_path(output_root),
        "spritesRoot": unity_path(sprite_root),
        "sourceSheetsRoot": unity_path(source_root),
        "sheets": [],
        "verification": {},
    }

    seen_hashes: Dict[str, str] = {}
    used_names: Dict[str, int] = {}
    sheet_number = 0

    for source in sources:
        sheet_number += 1
        digest = sha1(source)
        source_slug = make_source_slug(source, sheet_number)
        copied_source = source_root / f"{source_slug}.png"
        shutil.copy2(str(source), str(copied_source))
        ensure_png_meta(copied_source, is_sprite=False, nine_slice=False)

        sheet_record = {
            "sheetIndex": sheet_number,
            "sourcePath": str(source),
            "sourceFileName": source.name,
            "sourceSha1": digest,
            "copiedSource": unity_path(copied_source),
            "sprites": [],
        }

        if digest in seen_hashes:
            sheet_record["duplicateOf"] = seen_hashes[digest]
            manifest["sheets"].append(sheet_record)
            continue

        seen_hashes[digest] = source.name
        plan = find_sheet_plan(source.name)
        image = Image.open(source).convert("RGBA")
        rgba = np.array(image)
        alpha = build_alpha_mask(rgba)
        components = find_components(alpha, plan.group_dilation)

        for item_index, component in enumerate(components, start=1):
            component_record = write_component(
                rgba,
                alpha,
                component,
                sprite_root,
                sheet_number,
                item_index,
                plan,
                used_names,
                args.padding,
            )
            sheet_record["sprites"].append(component_record)
            sheet_record["sprites"].extend(
                write_derived_slices(component_record, sprite_root, used_names)
            )

        manifest["sheets"].append(sheet_record)

    manifest["verification"] = verify_generated_sprites(sprite_root)
    output_root.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    ensure_text_meta(manifest_path, force=True)

    print(
        "Sliced {0} source sheets into {1} sprites. Manifest: {2}".format(
            len(sources), count_manifest_sprites(manifest), manifest_path
        )
    )
    print(
        "Verification: {0}".format(
            json.dumps(manifest["verification"], ensure_ascii=False, sort_keys=True)
        )
    )
    return 0


def collect_sources(patterns: Sequence[str]) -> List[Path]:
    sources: List[Path] = []
    for pattern in patterns:
        expanded = sorted(Path().glob(pattern) if not has_drive_or_root(pattern) else Path(pattern).parent.glob(Path(pattern).name))
        sources.extend(path for path in expanded if path.is_file())

    unique: List[Path] = []
    seen = set()
    for source in sources:
        resolved = source.resolve()
        if resolved not in seen:
            seen.add(resolved)
            unique.append(source)

    if not unique:
        raise FileNotFoundError("No source PNGs matched: {0}".format(", ".join(patterns)))

    return sorted(unique, key=lambda path: (path.stat().st_mtime, str(path)))


def has_drive_or_root(pattern: str) -> bool:
    path = Path(pattern)
    return bool(path.drive) or pattern.startswith("/") or pattern.startswith("\\")


def clean_generated_dirs(
    output_root: Path,
    source_root: Path,
    sprite_root: Path,
    manifest_path: Path,
) -> None:
    resolved_output = output_root.resolve()
    for target in (source_root, sprite_root):
        resolved_target = target.resolve()
        if resolved_output not in (resolved_target, *resolved_target.parents):
            raise RuntimeError("Refusing to clean outside output root: {0}".format(target))
        if target.exists():
            shutil.rmtree(str(target))

    if manifest_path.exists():
        manifest_path.unlink()


def sha1(path: Path) -> str:
    digest = hashlib.sha1()
    with path.open("rb") as file:
        for chunk in iter(lambda: file.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def make_source_slug(source: Path, index: int) -> str:
    match = re.search(
        r"2026年4月(?P<day>\d+)日 (?P<hour>\d+)_(?P<minute>\d+)_(?P<second>\d+)(?: \((?P<suffix>\d+)\))?",
        source.name,
    )
    if match:
        suffix = match.group("suffix") or "0"
        return "sheet{0:02d}_202604{1}_{2}{3}{4}_{5}".format(
            index,
            match.group("day").zfill(2),
            match.group("hour"),
            match.group("minute"),
            match.group("second"),
            suffix,
        )

    sanitized = re.sub(r"[^A-Za-z0-9_-]+", "_", source.stem).strip("_").lower()
    return "sheet{0:02d}_{1}".format(index, sanitized or "source")


def find_sheet_plan(source_name: str) -> SheetPlan:
    for key, plan in SHEET_PLANS.items():
        if key in source_name:
            return plan
    return SheetPlan("Misc")


def build_alpha_mask(rgba: np.ndarray) -> np.ndarray:
    rgb = rgba[:, :, :3].astype(np.int16)
    max_channel = rgb.max(axis=2)
    min_channel = rgb.min(axis=2)
    gray = rgb.mean(axis=2).astype(np.float32)

    neutral = (
        (max_channel - min_channel <= NEUTRAL_MAX_CHANNEL_DELTA)
        & (min_channel >= NEUTRAL_MIN_VALUE)
    )
    palette = build_background_palette(rgba, neutral)
    dist = np.full(neutral.shape, 1_000_000, dtype=np.int32)
    for color in palette:
        color_dist = ((rgb - color) ** 2).sum(axis=2)
        dist = np.minimum(dist, color_dist)

    bg_like = neutral & (dist <= BACKGROUND_DISTANCE_THRESHOLD * BACKGROUND_DISTANCE_THRESHOLD)
    external_bg = flood_external_background(bg_like)

    mean = cv2.blur(gray, (CHECKER_STD_WINDOW, CHECKER_STD_WINDOW))
    mean2 = cv2.blur(gray * gray, (CHECKER_STD_WINDOW, CHECKER_STD_WINDOW))
    std = np.sqrt(np.maximum(0, mean2 - mean * mean))
    checker_bg = bg_like & (std > CHECKER_STD_THRESHOLD)

    bg = external_bg | checker_bg
    alpha = np.where(bg, 0, 255).astype(np.uint8)

    # Remove tiny one-pixel flecks created by antialiasing in the background.
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
    return alpha


def build_background_palette(rgba: np.ndarray, neutral: np.ndarray) -> np.ndarray:
    height, width = neutral.shape
    edge = np.zeros_like(neutral, dtype=bool)
    border = min(BACKGROUND_BORDER_SAMPLE, max(1, height // 8), max(1, width // 8))
    edge[:border, :] = True
    edge[-border:, :] = True
    edge[:, :border] = True
    edge[:, -border:] = True

    colors = rgba[:, :, :3][neutral & edge]
    if colors.size == 0:
        colors = rgba[:, :, :3][neutral]
    if colors.size == 0:
        return np.array([[255, 255, 255]], dtype=np.int16)

    quantized = (colors // 4) * 4
    values, counts = np.unique(quantized.reshape(-1, 3), axis=0, return_counts=True)
    order = np.argsort(counts)[::-1][:16]
    return values[order].astype(np.int16)


def flood_external_background(bg_like: np.ndarray) -> np.ndarray:
    height, width = bg_like.shape
    source = bg_like.astype(np.uint8)
    external = np.zeros((height, width), dtype=np.uint8)
    seeds = [
        (0, 0),
        (width - 1, 0),
        (0, height - 1),
        (width - 1, height - 1),
        (width // 2, 0),
        (width // 2, height - 1),
        (0, height // 2),
        (width - 1, height // 2),
    ]

    for seed in seeds:
        x, y = seed
        if not source[y, x] or external[y, x]:
            continue

        filled = source.copy()
        mask = np.zeros((height + 2, width + 2), dtype=np.uint8)
        cv2.floodFill(filled, mask, seed, 2)
        external |= (filled == 2).astype(np.uint8)

    return external.astype(bool)


def find_components(alpha: np.ndarray, group_dilation: int) -> List[Tuple[int, int, int, int]]:
    seed = (alpha > 0).astype(np.uint8)
    if group_dilation > 1:
        kernel = np.ones((group_dilation, group_dilation), np.uint8)
        seed = cv2.dilate(seed, kernel, iterations=1)

    count, labels, stats, _ = cv2.connectedComponentsWithStats(seed, 8)
    boxes: List[Tuple[int, int, int, int]] = []
    for label in range(1, count):
        x, y, width, height, _ = stats[label]
        actual_area = int(((labels == label) & (alpha > 0)).sum())
        if actual_area < MIN_COMPONENT_AREA:
            continue
        if width < MIN_COMPONENT_SIZE or height < MIN_COMPONENT_SIZE:
            continue
        boxes.append((int(x), int(y), int(width), int(height)))

    boxes.sort(key=lambda rect: (rect[1], rect[0]))
    return boxes


def write_component(
    rgba: np.ndarray,
    alpha: np.ndarray,
    component: Tuple[int, int, int, int],
    sprite_root: Path,
    sheet_number: int,
    item_index: int,
    plan: SheetPlan,
    used_names: Dict[str, int],
    padding: int,
) -> Dict[str, object]:
    height, width = alpha.shape
    x, y, component_width, component_height = component
    left = x - padding
    top = y - padding
    right = x + component_width + padding
    bottom = y + component_height + padding

    src_left = max(0, left)
    src_top = max(0, top)
    src_right = min(width, right)
    src_bottom = min(height, bottom)

    output_width = right - left + (OUTPUT_SAFE_BORDER * 2)
    output_height = bottom - top + (OUTPUT_SAFE_BORDER * 2)
    offset_x = src_left - left + OUTPUT_SAFE_BORDER
    offset_y = src_top - top + OUTPUT_SAFE_BORDER

    spec_for_item = resolve_spec(plan, sheet_number, item_index)
    category = spec_for_item.category
    name = make_unique_name(spec_for_item.name, used_names)
    output_dir = sprite_root / category
    output_dir.mkdir(parents=True, exist_ok=True)
    ensure_folder_meta(output_dir)
    output_path = output_dir / f"{name}.png"

    crop = np.zeros((output_height, output_width, 4), dtype=np.uint8)
    source_crop = rgba[src_top:src_bottom, src_left:src_right].copy()
    source_crop[:, :, 3] = alpha[src_top:src_bottom, src_left:src_right]
    crop[
        offset_y : offset_y + source_crop.shape[0],
        offset_x : offset_x + source_crop.shape[1],
    ] = source_crop
    Image.fromarray(crop, "RGBA").save(output_path)
    ensure_png_meta(output_path, is_sprite=True, nine_slice=should_use_nine_slice(name))

    return {
        "sheetItemIndex": item_index,
        "category": category,
        "name": name,
        "outputPath": unity_path(output_path),
        "cropRect": {
            "x": left,
            "y": top,
            "width": output_width,
            "height": output_height,
            "safeBorder": OUTPUT_SAFE_BORDER,
        },
        "sourceRect": {
            "x": src_left,
            "y": src_top,
            "width": src_right - src_left,
            "height": src_bottom - src_top,
        },
        "componentRect": {
            "x": x,
            "y": y,
            "width": component_width,
            "height": component_height,
        },
    }


def write_derived_slices(
    component_record: Dict[str, object],
    sprite_root: Path,
    used_names: Dict[str, int],
) -> List[Dict[str, object]]:
    source_name = str(component_record["name"])
    if source_name not in DERIVED_SLICES:
        return []

    source_path = Path(str(component_record["outputPath"]))
    if not source_path.exists():
        return []

    image = Image.open(source_path).convert("RGBA")
    records: List[Dict[str, object]] = []
    for slice_def in DERIVED_SLICES[source_name]:
        x, y, width, height = slice_def.rect
        crop = image.crop((x, y, x + width, y + height))
        crop = trim_transparent_padding(crop, DEFAULT_PADDING + OUTPUT_SAFE_BORDER)

        output_dir = sprite_root / slice_def.category
        output_dir.mkdir(parents=True, exist_ok=True)
        ensure_folder_meta(output_dir)

        name = make_unique_name(slice_def.name, used_names)
        output_path = output_dir / f"{name}.png"
        crop.save(output_path)
        ensure_png_meta(output_path, is_sprite=True, nine_slice=should_use_nine_slice(name))

        records.append(
            {
                "sheetItemIndex": component_record["sheetItemIndex"],
                "category": slice_def.category,
                "name": name,
                "outputPath": unity_path(output_path),
                "derivedFrom": component_record["outputPath"],
                "derivedRect": {
                    "x": x,
                    "y": y,
                    "width": width,
                    "height": height,
                },
            }
        )

    return records


def trim_transparent_padding(image: Image.Image, padding: int) -> Image.Image:
    alpha = image.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        return image

    left, top, right, bottom = bounds
    left = max(0, left - padding)
    top = max(0, top - padding)
    right = min(image.width, right + padding)
    bottom = min(image.height, bottom + padding)
    trimmed = image.crop((left, top, right, bottom))
    output = Image.new("RGBA", (trimmed.width + OUTPUT_SAFE_BORDER * 2, trimmed.height + OUTPUT_SAFE_BORDER * 2), (0, 0, 0, 0))
    output.alpha_composite(trimmed, (OUTPUT_SAFE_BORDER, OUTPUT_SAFE_BORDER))
    return output


def resolve_spec(plan: SheetPlan, sheet_number: int, item_index: int) -> AssetSpec:
    if item_index <= len(plan.specs):
        return plan.specs[item_index - 1]

    category = plan.default_category
    category_slug = category.lower()
    if category == "Misc":
        name = "ui_misc_sheet{0:02d}_item{1:02d}".format(sheet_number, item_index)
    else:
        name = "ui_{0}_sheet{1:02d}_item{2:02d}".format(
            category_slug,
            sheet_number,
            item_index,
        )
    return spec(category, name)


def make_unique_name(name: str, used_names: Dict[str, int]) -> str:
    count = used_names.get(name, 0)
    used_names[name] = count + 1
    if count == 0:
        return name
    return "{0}_{1:02d}".format(name, count + 1)


def verify_generated_sprites(sprite_root: Path) -> Dict[str, object]:
    sprites = sorted(sprite_root.glob("*/*.png"))
    corner_failures = []
    opaque_pixel_count = 0
    for sprite in sprites:
        image = Image.open(sprite).convert("RGBA")
        alpha = image.getchannel("A")
        opaque_pixel_count += int(np.count_nonzero(np.array(alpha)))
        width, height = image.size
        corners = [
            alpha.getpixel((0, 0)),
            alpha.getpixel((width - 1, 0)),
            alpha.getpixel((0, height - 1)),
            alpha.getpixel((width - 1, height - 1)),
        ]
        if any(value != 0 for value in corners):
            corner_failures.append(unity_path(sprite))

    return {
        "spriteCount": len(sprites),
        "opaquePixelCount": opaque_pixel_count,
        "cornerAlphaFailures": corner_failures,
        "cornerAlphaPass": len(corner_failures) == 0,
    }


def ensure_folder_meta(folder: Path) -> None:
    meta_path = Path(str(folder) + ".meta")
    if meta_path.exists():
        return

    meta_path.write_text(
        "\n".join(
            [
                "fileFormatVersion: 2",
                "guid: {0}".format(stable_guid(folder)),
                "folderAsset: yes",
                "DefaultImporter:",
                "  externalObjects: {}",
                "  userData: ",
                "  assetBundleName: ",
                "  assetBundleVariant: ",
                "",
            ]
        ),
        encoding="utf-8",
    )


def ensure_png_meta(path: Path, is_sprite: bool, nine_slice: bool) -> None:
    meta_path = Path(str(path) + ".meta")
    if meta_path.exists():
        return

    texture_type = 8 if is_sprite else 0
    sprite_mode = 1 if is_sprite else 0
    alpha_is_transparency = 1 if is_sprite else 0
    border = "{x: 24, y: 24, z: 24, w: 24}" if nine_slice else "{x: 0, y: 0, z: 0, w: 0}"
    meta_path.write_text(
        "\n".join(
            [
                "fileFormatVersion: 2",
                "guid: {0}".format(stable_guid(path)),
                "TextureImporter:",
                "  internalIDToNameTable: []",
                "  externalObjects: {}",
                "  serializedVersion: 13",
                "  mipmaps:",
                "    mipMapMode: 0",
                "    enableMipMap: 0",
                "    sRGBTexture: 1",
                "    linearTexture: 0",
                "    fadeOut: 0",
                "    borderMipMap: 0",
                "    mipMapsPreserveCoverage: 0",
                "    alphaTestReferenceValue: 0.5",
                "    mipMapFadeDistanceStart: 1",
                "    mipMapFadeDistanceEnd: 3",
                "  bumpmap:",
                "    convertToNormalMap: 0",
                "    externalNormalMap: 0",
                "    heightScale: 0.25",
                "    normalMapFilter: 0",
                "    flipGreenChannel: 0",
                "  isReadable: 0",
                "  streamingMipmaps: 0",
                "  streamingMipmapsPriority: 0",
                "  vTOnly: 0",
                "  ignoreMipmapLimit: 0",
                "  grayScaleToAlpha: 0",
                "  generateCubemap: 6",
                "  cubemapConvolution: 0",
                "  seamlessCubemap: 0",
                "  textureFormat: 1",
                "  maxTextureSize: 2048",
                "  textureSettings:",
                "    serializedVersion: 2",
                "    filterMode: 1",
                "    aniso: 1",
                "    mipBias: 0",
                "    wrapU: 1",
                "    wrapV: 1",
                "    wrapW: 1",
                "  nPOTScale: 0",
                "  lightmap: 0",
                "  compressionQuality: 50",
                "  spriteMode: {0}".format(sprite_mode),
                "  spriteExtrude: 1",
                "  spriteMeshType: 1",
                "  alignment: 0",
                "  spritePivot: {x: 0.5, y: 0.5}",
                "  spritePixelsToUnits: 100",
                "  spriteBorder: {0}".format(border),
                "  spriteGenerateFallbackPhysicsShape: 1",
                "  alphaUsage: 1",
                "  alphaIsTransparency: {0}".format(alpha_is_transparency),
                "  spriteTessellationDetail: -1",
                "  textureType: {0}".format(texture_type),
                "  textureShape: 1",
                "  singleChannelComponent: 0",
                "  flipbookRows: 1",
                "  flipbookColumns: 1",
                "  maxTextureSizeSet: 0",
                "  compressionQualitySet: 0",
                "  textureFormatSet: 0",
                "  ignorePngGamma: 0",
                "  applyGammaDecoding: 0",
                "  swizzle: 50462976",
                "  cookieLightType: 0",
                "  platformSettings:",
                "  - serializedVersion: 4",
                "    buildTarget: DefaultTexturePlatform",
                "    maxTextureSize: 2048",
                "    resizeAlgorithm: 0",
                "    textureFormat: -1",
                "    textureCompression: 1",
                "    compressionQuality: 50",
                "    crunchedCompression: 0",
                "    allowsAlphaSplitting: 0",
                "    overridden: 0",
                "    ignorePlatformSupport: 0",
                "    androidETC2FallbackOverride: 0",
                "    forceMaximumCompressionQuality_BC6H_BC7: 0",
                "  spriteSheet:",
                "    serializedVersion: 2",
                "    sprites: []",
                "    outline: []",
                "    physicsShape: []",
                "    bones: []",
                "    spriteID: ",
                "    internalID: 0",
                "    vertices: []",
                "    indices: ",
                "    edges: []",
                "    weights: []",
                "    secondaryTextures: []",
                "    nameFileIdTable: {}",
                "  mipmapLimitGroupName: ",
                "  pSDRemoveMatte: 0",
                "  userData: ",
                "  assetBundleName: ",
                "  assetBundleVariant: ",
                "",
            ]
        ),
        encoding="utf-8",
    )


def ensure_text_meta(path: Path, force: bool = False) -> None:
    meta_path = Path(str(path) + ".meta")
    if meta_path.exists() and not force:
        return

    meta_path.write_text(
        "\n".join(
            [
                "fileFormatVersion: 2",
                "guid: {0}".format(stable_guid(path)),
                "TextScriptImporter:",
                "  externalObjects: {}",
                "  userData: ",
                "  assetBundleName: ",
                "  assetBundleVariant: ",
                "",
            ]
        ),
        encoding="utf-8",
    )


def should_use_nine_slice(name: str) -> bool:
    lower = name.lower()
    return any(token in lower for token in ("button", "panel", "card", "frame", "bubble", "bg"))


def stable_guid(path: Path) -> str:
    normalized = unity_path(path).lower()
    return hashlib.sha1(("siwakenja-ui:" + normalized).encode("utf-8")).hexdigest()[:32]


def count_manifest_sprites(manifest: Dict[str, object]) -> int:
    return sum(len(sheet.get("sprites", [])) for sheet in manifest["sheets"])


def unity_path(path: Path) -> str:
    return str(path).replace("\\", "/")


if __name__ == "__main__":
    raise SystemExit(main())
