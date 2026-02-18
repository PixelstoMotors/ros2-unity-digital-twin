# Pixels to Motors - R2R2R Robotics UX

<p align="center">
  <img src="docs/images/r2r2r_thumbnail.png" alt="R2R2R Methodology Overview" width="800">
</p>

<p align="center">
  <strong>From Digital Pixels to Real-World Motion</strong><br>
  デザイナー歴29年の知見を活かした、AI時代のロボットUX実装
</p>

---

## 🎯 Vision - R2R2R Methodology

**R2R2R（Real-to-Real-to-Real）** は、AI時代の新しいロボット開発サイクルです：

```
Real (Scan) → Render (Unity) → Train/Simulate → Real (Deploy)
     ↓              ↓                 ↓              ↓
  3DGS撮影     リッチUX環境       AI学習/ROS      実機展開
```

<p align="center">
  <img src="docs/images/r2r2r_flow.png" alt="R2R2R Flow" width="600">
</p>

### 私たちの強み

**29年間培ってきた「外さないUX実装力」**

- デザイナーとしての審美眼とユーザー体験への深い理解
- AIエージェントによる爆速開発サイクル
- 身体性AI（Embodied AI）の実践的アプローチ

---

## 🛠 Technical Stack

| Technology | Purpose |
|-----------|---------|
| **3DGS (Gaussian Splatting)** | 高精度现场スキャン |
| **Unity 6** | リッチなリアルタイムレンダリング |
| **Docker + ROS2** | ロボティクス基盤 |
| **AI Agents** | 爆速プロトタイピング |

---

## 📁 Project Structure

```
Robotics_UX_Project/
├── docs/
│   ├── images/              # サムネイル画像
│   ├── technical_demo_video_plan.md  # デモ動画構成
│   ├── unity_ros2_setup.md          # 環境構築
│   └── SETUP_NOTES.md              # セットアップメモ
├── Assets/
│   ├── ROSKinematicSync.cs         # ROS-Unity同期
│   └── ...
└── README.md
```

---

## 🚀 Quick Start

### Docker (ROS2) 起動
```bash
docker start ros2_sim
docker exec -it ros2_sim bash
source /opt/ros/humble/setup.bash
ros2 launch ...
```

### Unity 実行
1. Unity 2022.3+ を開く
2. Robotics_UX_Unity プロジェクトを選択
3. Play ボタンで実行

---

## 🔗 Links

- **GitHub**: https://github.com/PixelstoMotors/ros2-unity-digital-twin
- **技術ドキュメント**: `docs/` フォルダを参照

---

<p align="center">
© 2026 Pixels to Motors / ADAPT DESIGN<br>
デザイナー歴29年の誇りを，胸に。
</p>
