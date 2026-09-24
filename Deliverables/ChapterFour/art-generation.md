# 第4章の絵柄

12枚の透過PNGは内蔵の `image_gen` で個別に生成した。元画像は Codex の `generated_images` に保存され、採用画像は `Assets/Art/Sprites/SortingThemes/` に配置した。いずれも正方形、単体、透明背景、濃紺の太い輪郭、明るい2Dゲーム用の配色、縮小時にも区別できる大きな形を共通指定した。画像内の文字、背景、小物の追加は指定していない。

| ファイル | 個別の絵柄指定 |
| --- | --- |
| `green-bean.png` | 細長く緩く曲がったインゲンの莢。 |
| `snow-pea.png` | 幅広く平たいサヤエンドウの莢。 |
| `edamame.png` | ふっくらした複数の豆が見える枝豆の莢。 |
| `lizard.png` | 細身で長い尾のトカゲ。 |
| `gecko.png` | 吸盤のある広がった指と大きな目のヤモリ。 |
| `leopard-gecko.png` | 斑点模様と太い尾を持つレオパ。 |
| `handstand.png` | 両手だけで体を支える、服を着た成人の逆立ち。 |
| `tripod-headstand.png` | 頭と両手の三点で支える、服を着た成人の三点倒立。 |
| `bridge-pose.png` | 両手と両足で体を反らせて支える、服を着た成人のブリッジ。 |
| `ancient-person.png` | 毛皮風の服と石器を持つ原始人。 |
| `samurai.png` | 和装と刀を持つ侍。 |
| `modern-person.png` | 現代の普段着とスマートフォンを持つ人物。 |

`hiragana-me.png`、`hiragana-no.png`、`hiragana-yu.png` は画像生成で文字を作らず、既存の `Assets/Fonts/NotoSansCJKjp-Regular.otf` から `Tools/Build-HiraganaSprites.py` で描画した。これにより「め」「の」「ゆ」の字形を正確に保つ。
