# R2R2Rプロジェクト フォルダ構成ガイド

## 重要な更新（2026/02/20）
- RobotIndividualControllerの機能がRobotPersonalityに統合されました
- 物理パラメータの保護機能が実装されました
- フォルダ構造が整理され、ロボット関連のコア機能が集約されました

## 基本構造
```
Assets/
├── Scripts/          # 基本的なゲームロジック
│   ├── Robot/       # ロボット関連のコア機能
│   └── Utils/       # ユーティリティクラス
├── Editor/          # エディタ拡張スクリプト
├── Scenes/          # シーンファイル
│   └── Experimental/ # 実験用シーン
├── Prefabs/         # プレハブ
└── Settings/        # プロジェクト設定
```

## スクリプト配置ルール

1. コアスクリプト
   - 場所: Assets/Scripts/Robot/
   - 例: RobotPersonality.cs, ROSKinematicSync.cs

2. エディタツール
   - 場所: Assets/Editor/
   - 例: EmergencyFix.cs, RobotSetup.cs

3. 実験用シーン
   - 場所: Assets/Scenes/Experimental/
   - 命名規則: [機能名]_[日付].unity

## 物理パラメータの管理
- 物理パラメータは「憲法」として保護されています
- 固定値：
  - Stiffness: 20,000
  - Damping: 2,000
  - Force Limit: 1,000
  - Max Velocity: 100
  - Mass: 1.0
  - Angular Drag: 0.05
- これらの値は定数として実装され、実行時の変更は不可能です
- 値の変更が必要な場合は、新しいプレハブの作成が必要です

## RobotPersonality.cs の機能
1. 物理パラメータの保護
   - 起動時に自動的に正しい値を設定
   - すべての軸（X, Y, Z）に同一の値を適用
   - 実行時の変更を防止

2. 個別制御パラメータ
   - speedMultiplier: 動作速度の倍率設定（0.1-5.0）
   - angleOffset: 目標角度のオフセット値（±10度）
   - swingRange: 左右の振れ幅（0-90度）

3. 位置ベースの動作制御
   - 右側配置: 静止状態（オフセットのみ適用）
   - 左側配置: サイン波による動的な動き
