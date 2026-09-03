# DeepMeta

[![Release](https://img.shields.io/github/v/release/DeepMetaverseEngine/DeepMeta?color=blue&style=flat-square)](https://github.com/DeepMetaverseEngine/DeepMeta/releases)
[![License](https://img.shields.io/github/license/DeepMetaverseEngine/DeepMeta?style=flat-square)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey?style=flat-square)](https://github.com/DeepMetaverseEngine/DeepMeta/releases/tag/v1.0.0)

> **DeepMeta** 是专为高性能游戏客户端与服务器端架构设计的 CLI 命令行与核心工具箱。

---

该框架涵盖了几乎任何主流角色扮演类游戏。

- 提供全套大到MMORPG、ARPG、MOBA、卡牌、肉鸽、自走棋，小到微信小游戏的解决方案。
- 曾开发顶级3A级PBR渲染管线MMORPG。
- 10款以上商业游戏线上稳定运行，10年以上游戏引擎项目积累。
- 过往项目充分经过市场验证，月流水过亿，服务器百万人在线，AppStore排行榜第一。
- 最早采用高性能微服务架构的MMORPG引擎。
- 最早获得元宇宙核心技术引擎著作权 。
- 最早在移动端实现基于物理的渲染引擎。
- 普通玩家用户零代码即可上手，入门门槛极低。可以方便进行二次创作。
- 和主流图形引擎（Unreal Unity3D Godot）无缝衔接。
- 游戏类型支持丰富，支持卡牌，动作，回合，即时策略，MOBA，MMORPG，微信小游戏。
- 结合框架和编辑器，让AI Agent产生游戏内的相关内容。

<img width="1389" height="744" alt="image" src="https://github.com/user-attachments/assets/b0ec5031-1bc2-4d62-a939-a118616007ea" />

---

编辑器功能涵盖（战斗相关所有功能）场景编辑、单位编辑、技能编辑、法术编辑、BUFF编辑、物品编辑、光环编辑、行为树编辑、资源管理。
<img width="2670" height="1421" alt="image" src="https://github.com/user-attachments/assets/b01ca208-60ec-4ef3-a514-274ad63ffc7e" />

>编辑器本身和Unity没有关系，只是由Unity发布一个符合编辑器接口的PC运行时。该运行时内嵌在我们的编辑器窗口内。
>不涉及任何人员使用Unity编辑器，也可以大量节省Unity编辑器席位。如果是其他引擎比如Unreal，那么做法一样。
>编辑器运行时通过RPC和编辑器本身进行通信来打通操作环节。

---

## 🚀 快速下载与安装 (Download)

可以直接下载最新版本的 Windows 可执行文件：

| 版本 | 文件类型 | 下载链接 |
| :--- | :--- | :--- |
| **v1.0.0** (最新版) | Windows CLI (`.exe`) | [📥 点击下载 gamecli.exe](https://github.com/DeepMetaverseEngine/DeepMeta/releases/download/v1.0.0/gamecli.exe) |

---

## 💡 快速上手 (Quick Start)

`gamecli.exe` 为独立单文件程序，**放到任意目录执行即可自动初始化整个游戏工程**。

### 1. 下载工具
通过上述表格中的链接下载 `gamecli.exe`，或使用命令行（PowerShell）下载至你想要创建工程的目标目录：
```powershell
Invoke-WebRequest -Uri "[https://github.com/DeepMetaverseEngine/DeepMeta/releases/download/v1.0.0/gamecli.exe](https://github.com/DeepMetaverseEngine/DeepMeta/releases/download/v1.0.0/gamecli.exe)" -OutFile "gamecli.exe"
```
运行后，会自动创建游戏工程。前提条件是在Windows环境，你需要配置好你的.ssh证书。

比如我把`gamecli.exe`放到一个叫`Aserg`的空目录里，运行后会自动创建工程结构和VS工程。
<img width="2066" height="924" alt="image" src="https://github.com/user-attachments/assets/daac2eda-42a5-42a8-926b-6e13b0d7a282" />
<img width="630" height="565" alt="image" src="https://github.com/user-attachments/assets/ab9cc15e-c88d-4b71-a160-eee278022ff4" />

工具会自动帮你创建VisualStudio的SLN工程。
<img width="2036" height="1374" alt="image" src="https://github.com/user-attachments/assets/205241d9-5c6f-4933-8110-9e36ffbccf7d" />

编译工程后，会生成编辑器工程。
<img width="2193" height="1497" alt="image" src="https://github.com/user-attachments/assets/82fadb24-dc9c-4637-8e5d-acdfbc7b57ea" />

开始你的独立游戏之旅吧。


