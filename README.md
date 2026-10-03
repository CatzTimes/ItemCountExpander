# ItemCountExpander

[English](#english) | [中文](#中文)

基于 [GazziFX/ItemCountExpander](https://github.com/GazziFX/ItemCountExpander) 的增强 fork —— 一个 Unturned 原生模块，把背包/储物箱等页面的物品格上限从 `200` 扩展到 `255`。纯服务端模块，无需 Rocket 等任何插件框架。

---

<a name="中文"></a>

## 中文

### 简介

Unturned 在三处对"单页物品数量"做了硬编码护栏 `getItemCount() >= 200`。当容器/背包资产的网格面积超过 200 格时，第 201 件物品会被这层护栏拒绝。本模块把护栏放宽到 `255`（1 字节的理论上限），让超大容量资产能真正用满全部格子。

### 工作原理

游戏内所有动态添加物品的服务端路径（拾取、`/give`、拖拽、空投、合成、NPC 奖励……共 30+ 调用点）最终都收敛到以下三个方法：

| 方法 | 覆盖的路径 |
|---|---|
| `Items.tryAddItem(Item, bool)` | 所有自动放置 |
| `PlayerInventory.tryAddItem(Item, byte, byte, byte, byte)` | 指定坐标放置 |
| `PlayerInventory.ReceiveDragItem` | 客户端拖拽 RPC |

模块在加载早期用 Harmony Transpiler 把这三个方法体内的 `ldc.i4 200` 改写为 `ldc.i4 255`。物品数量在内存、网络同步（`WriteUInt8`）与存档（`writeByte`）中均为 1 字节，因此 **255 是安全上限**，vanilla 客户端可以完整接收并显示，无需安装任何客户端模组。

### 本 Fork 的改进内容

相对上游 GazziFX/ItemCountExpander v1.x：

1. **失效不再静默**：游戏更新若导致方法改名/删除，模块会输出明确的 error 日志（含游戏版本号）并整体放弃打补丁，而不是被 `ModuleHook` 吞掉异常后假装初始化成功、让 200 上限悄悄回来。启动成功时也会输出自检报告（`patched 3 method(s), replaced 3 count guard site(s)`）。
2. **全有全无（fail-fast）**：三个补丁目标必须全部解析成功才动手，杜绝"部分生效"的行为不一致。
3. **精确签名过滤**：全部目标使用显式参数签名解析，官方未来加重载不会引发 `AmbiguousMatchException`；方法名尽量用 `nameof` 绑定，游戏改名会在**编译期**暴露。
4. **上下文感知 Transpiler + 计数断言**：只改写"前导 `call/callvirt getItemCount`、后随条件分支"的 `ldc.i4 200`（指令三元组匹配，兼容独立 if 与复合 `||` 条件两种版本形态），方法内出现的无关 200 常量不可能被误伤；每个目标的实际替换数与期望值（1 处）比对，漏改/多改都会在日志中报错。
5. **模块化架构**：按职责划分为 `Configurations`（常量配置）、`Models`（目标定义与结果模型）、`Services`（目标解析 / Transpiler / 补丁装配）、`Monitors`（完整性自检日志），入口 `Module.cs` 只做装配。
6. **NuGet 依赖迁移**：用 [`RocketModFix.Unturned.Redist.Server`](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Server)（游戏程序集）与 [`RocketModFix.UnityEngine.Redist`](https://www.nuget.org/packages/RocketModFix.UnityEngine.Redist)（基类链解析所需）替代无 HintPath 的裸 `Assembly-CSharp` 引用，clone 即可编译；二者均 `ExcludeAssets="runtime"`，不会把游戏/引擎 dll 复制进输出。
7. **Lib.Harmony 2.2.2 → 2.4.2**：获得 MonoMod.Core 三代升级带来的 Mono hook 稳定性修复（含 try/catch 方法 patch 修复）。
8. **部署配置纳入版本管理**：`ItemCountExpander.module`（Role=Server）随仓库提供。

### 构建

```shell
dotnet build -c Release
```

产物位于 `bin/Release/net48/`：`ItemCountExpander.dll` + `0Harmony.dll`。

### 安装（服务端）

将以下 3 个文件放入服务器的 `Unturned/Modules/ItemCountExpander/` 目录：

```
Unturned/Modules/ItemCountExpander/
    ItemCountExpander.module
    ItemCountExpander.dll
    0Harmony.dll
```

专用服务器原生模块系统会自动加载，无需任何配置。启动日志中搜索 `ItemCountExpander` 可确认：

```
ItemCountExpander: patched 3 method(s), replaced 3 count guard site(s); per-page item limit is now 255 (game version 3.x.x.x).
```

### 兼容性

- 仅支持使用新 RPC 命名（`ReceiveDragItem` 等）的游戏版本；对旧版本会明确报错而非静默失效。
- 纯服务端补丁，客户端保持 vanilla；与 BattleEye 无冲突。
- 255 是 byte 全链路的硬上限（内存/网络/存档），无法再提高；`byte.MaxValue` 的哨兵语义（"未找到"、"自动找位"）不受影响。
- 实际收益需要容器/背包资产的网格面积超过 200 格（`Storage_X/Storage_Y`、`Width/Height`）。

---

<a name="english"></a>

## English

### Introduction

An enhanced fork of [GazziFX/ItemCountExpander](https://github.com/GazziFX/ItemCountExpander) — a native Unturned module that raises the per-page item-slot cap (backpack, storage, etc.) from `200` to `255`. Server-side only; no plugin framework required.

### How it works

Every server-side path that dynamically adds items (pickup, `/give`, drag & drop, airdrops, crafting, NPC rewards — 30+ call sites) converges into three methods:

| Method | Covered paths |
|---|---|
| `Items.tryAddItem(Item, bool)` | all auto-placement |
| `PlayerInventory.tryAddItem(Item, byte, byte, byte, byte)` | explicit-coordinate placement |
| `PlayerInventory.ReceiveDragItem` | client drag & drop RPC |

At load time a Harmony transpiler rewrites `ldc.i4 200` into `ldc.i4 255` inside these methods. Item counts are one byte wide everywhere (memory, network sync via `WriteUInt8`, save files via `writeByte`), so **255 is the safe ceiling** and vanilla clients receive and display everything without any client-side install.

### Improvements in this fork

Compared to upstream v1.x:

1. **No more silent failures**: if a game update renames/removes a target method, the module logs a clear error (including the game version) and skips patching entirely, instead of being swallowed by `ModuleHook`'s catch-all while the vanilla 200 limit quietly returns. A success self-check is logged on startup (`patched 3 method(s), replaced 3 count guard site(s)`).
2. **All-or-nothing (fail-fast)**: patches are applied only when all three targets resolve, preventing inconsistent "partially patched" behavior.
3. **Explicit signature filtering**: every target is resolved with exact parameter types, so future game overloads cannot cause `AmbiguousMatchException`; method names use `nameof` where possible so renames fail at build time.
4. **Context-aware transpiler + counted assertion**: only an `ldc.i4 200` preceded by a `call/callvirt getItemCount` and followed by a conditional branch is rewritten (instruction-triplet matching, compatible with both the standalone-if and compound-`||` shapes across game versions); unrelated 200 constants can never be hit, and the actual replacement count per target is compared against the expectation (1).
5. **Modular architecture**: responsibilities split into `Configurations`, `Models`, `Services` (target resolution / transpiler / patch application) and `Monitors` (integrity logging); the `Module.cs` entry point only wires things together.
6. **NuGet dependency migration**: [`RocketModFix.Unturned.Redist.Server`](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Server) (game assemblies) and [`RocketModFix.UnityEngine.Redist`](https://www.nuget.org/packages/RocketModFix.UnityEngine.Redist) (needed for base-type chain resolution) replace the bare `Assembly-CSharp` reference — the repo builds right after cloning. Both use `ExcludeAssets="runtime"` so no game/engine dll leaks into the output.
7. **Lib.Harmony 2.2.2 → 2.4.2**: picks up three generations of MonoMod.Core improvements for Mono hook stability (including the try/catch method patching fix).
8. **Deployment config in version control**: `ItemCountExpander.module` (Role=Server) is committed.

### Building

```shell
dotnet build -c Release
```

Output lands in `bin/Release/net48/`: `ItemCountExpander.dll` + `0Harmony.dll`.

### Installation (server)

Copy these 3 files into the server's `Unturned/Modules/ItemCountExpander/` directory:

```
Unturned/Modules/ItemCountExpander/
    ItemCountExpander.module
    ItemCountExpander.dll
    0Harmony.dll
```

The vanilla module system loads it automatically — no configuration needed. Verify via `ItemCountExpander` in the startup log:

```
ItemCountExpander: patched 3 method(s), replaced 3 count guard site(s); per-page item limit is now 255 (game version 3.x.x.x).
```

### Compatibility

- Requires game versions using the modern RPC naming (`ReceiveDragItem` etc.); older versions produce an explicit error, never a silent failure.
- Pure server-side patch; clients stay vanilla; no BattleEye conflicts.
- 255 is the hard byte-width ceiling (memory/network/save); it cannot be raised further, and the `byte.MaxValue` sentinel semantics ("not found", "auto-find slot") are untouched.
- Real-world benefit requires container/bag assets with a grid area above 200 cells (`Storage_X/Storage_Y`, `Width/Height`).

---

## Credits / 致谢

- [GazziFX](https://github.com/GazziFX) — original ItemCountExpander
- [RocketModFix](https://github.com/RocketModFix) — maintained RocketMod fork & redist NuGet packages
- [pardeike/Harmony](https://github.com/pardeike/Harmony) — the Harmony library
