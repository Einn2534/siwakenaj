# Powa image generation provenance

Mode: built-in image_gen (not CLI).
Reference: supplied idol.png, preserved as idol-reference.png.

Selected outputs:
- Assets/Art/Sprites/Characters/Powa/Gameplay/PowaParts.png: exec-13ee6961-6bc0-4b72-a36c-c84ba1d63f8d.png (second generation).
- Assets/Art/Sprites/Characters/Powa/Gameplay/PowaExpressions.png: exec-6673a6dd-5b27-4898-ad9f-10de305c572d.png.

Unity slices the sheets through sprite data providers and preserves the PNG files. PowaCutout.shader remaps alpha coverage at render time to remove matte fringe and restore opaque interiors.

## powaAtlasPrompt

Create ONE production-ready transparent PNG sprite atlas for the attached Powa witch character, using the attached idol.png as the strict visual identity reference. This is a technical cutout animation asset, not a poster. Preserve the same polished anime illustration, dark navy oversized bent witch hat, purple sleeveless dress, pale skin, brown boots, cyan orb on a brown staff. True alpha transparency everywhere outside the parts. No white background, no checkerboard, no shadows cast onto the canvas, no text, no labels, no borders. Output a SQUARE high-resolution atlas with an EXACT 3 by 3 equal-cell grid, one separate isolated body part centered in each cell, with generous clear gaps between cells. Each cell is 1/3 canvas width and height. Do not overlap cells. Keep all artwork within the inner 88% of its cell. Draw all parts in the same front-three-quarter viewpoint and idle pose as the reference, and complete the hidden joints so they can overlap when assembled.
Cell row1 col1: the ENTIRE large bent witch hat with broad brim, brown band and small cyan tip ornament, but REMOVE THE ROUND GOLD FACE BADGE entirely (plain uninterrupted brown band there). Same curling left tip and asymmetric broad brim as idol.png.
Cell row1 col2: the entire pale oval head including small ears and a short neck, NO EYES, NO HAIR, NO MOUTH, no hat; shade the upper half as hidden under the hat. A blank skin head plate.
Cell row1 col3: the complete purple sleeveless dress and torso, from collar to flowing wide skirt hem, with dark purple underside and same folds as reference. No arms, no legs, no head, no neck.
Cell row2 col1: character's arm seen at SCREEN LEFT, complete from rounded shoulder cap down to open relaxed hand; slender pale arm hanging diagonally down-left, elbow slightly bent. No clothing.
Cell row2 col2: character's arm seen at SCREEN RIGHT, complete from rounded shoulder cap down to hand, hanging diagonally down-right; fingers in a relaxed loose grip suitable for holding the staff. No clothing.
Cell row2 col3: character's leg seen at SCREEN LEFT, one complete pale thigh and knee plus tall brown boot, front supporting leg pointing down, like reference. No dress.
Cell row3 col1: character's leg seen at SCREEN RIGHT, one complete pale thigh and bent knee plus tall brown boot, lifted slightly and pointing down-left, like reference. No dress.
Cell row3 col2: exactly ONE complete straight slender brown magic staff with gold fitting and cyan orb, upright vertical orientation. No hands.
Cell row3 col3: the round orange-gold hat badge ONLY, with the exact happy closed eyes and wavy mouth of reference, front facing circle.
Do not include any assembled character, duplicates, extra objects or effect particles. All nine assets must be fully visible and separated. Clean antialiased alpha edges without colored fringe.

## powaAtlasCleanupPrompt

Edit this transparent Powa cutout sprite atlas into a CLEAN production texture. Keep exactly the same nine designs and illustration, same colors and shape. Keep true alpha transparency. Important layout correction: output a square canvas with an EXACT 3x3 equal grid. EACH of the nine parts must be completely inside the central 76% of its own cell, at least 12% cell width clear transparent margin on ALL four sides. Scale parts down as necessary, especially the hat, which currently crosses its cell boundary. There must be no part from any other cell inside another cell's rectangle. No text, no labels, no frame, no checkerboard. Order stays: top row hat / blank head / purple dress; middle row left arm / right arm / straight booted leg; bottom row bent booted leg / ONE staff / gold happy face badge. Remove all colored speckles, white halos, and red cyan magenta edge contamination around every silhouette. Clean smooth dark outlines with neutral antialiasing. The dress armholes must be EMPTY transparent openings, not skin-colored shapes. The head has no mouth. The hat has no gold face badge. Preserve all other details. High resolution.

## powaExpressionPrompt

Create ONE transparent PNG expression sprite atlas for the attached Powa character. Match idol.png exactly in polished anime illustration and line art. Real transparent alpha background, no white backdrop, no checkerboard, no text or labels. Square canvas, EXACT 3 columns by 2 rows of equal cells, ONE isolated symbol centered in each cell with plenty of transparent padding; nothing crosses a cell boundary.
Top left: only the tiny pink triangular neutral mouth with dark outline seen in idol.png, no face or skin.
Top center: only a happy open smiling mouth, rounded triangular shape, dark outline, pink inner mouth, no teeth, no face or skin.
Top right: only a tiny worried wavy black mouth line, no face or skin.
Bottom left: circular orange-gold hat badge matching the reference badge with closed smiling eyes and a simple joyful smiling mouth, full circular badge with polished warm shading.
Bottom center: identical size and shape orange-gold circular hat badge with squeezed sad eyes and a worried wavy mouth.
Bottom right: ONE small cyan tear droplet, long teardrop shape with white highlight and fine dark outline.
Do not add assembled characters, body parts, rings, duplicate badges, decorative stars or any other content. Clean dark outlines and no colored edge speckles. These will be sliced and displayed separately.
