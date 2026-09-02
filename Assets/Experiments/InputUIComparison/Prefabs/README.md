# Prefab方針

本実験では本番Prefabを複製・変更していません。

Canvas、HUD、車カード、入力枠、質問票は `InputUIComparisonController` が実行時に中立的なuGUIとして生成します。これにより、3案の差分を同一のコード・同一の車両表示・同一のfeedback上で、control配置と状態表現だけに限定しています。

本番 `car.prefab`、`LaneInputController`、`GimmickHudController` はGameFlow・Scene callback・動的Canvas生成への依存が強く、比較Sceneへ直接持ち込むと本番状態や計測条件を汚すため使用していません。
