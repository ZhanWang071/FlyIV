# FlyIV 2.0 — Client/Assets 代码说明

> 本文档用于帮助理解项目结构、运行管线，以及 **Classroom（教室）** 与 **City（城市）** 两个场景中 **8 个任务（T1–T8）** 的执行方式，方便后续阅读和修改代码。

---

## 1. 项目概览

这是一个基于 Unity 的 **VR 沉浸式数据可视化创作系统**（Oculus/Meta XR + XR Interaction Toolkit），核心思路是：

1. 用户通过**语音**（或键盘模拟）下达自然语言指令；
2. 系统通过 **LLM** 将指令翻译成一段 **Skill API 调用序列**（例如 `CREATE(...)`、`EMBED(...)`、`LAYOUT(...)`）；
3. `ActionExecutor` 用 **Roslyn** 在运行时动态加载 `StreamingAssets/Skills/` 下的 C# 脚本并逐条执行；
4. 最终在 VR 场景中创建/修改图表：**2D 图表用 XCharts**（挂在 Canvas 上），**3D 图表用 DxR**（实例化 DxRVis prefab）。

主场景为 `Scenes/DemoScene01.unity`，它同时包含 **Classroom** 和 **City** 两个子场景 GameObject，通过 `UserStudyController` 切换显示。

---

## 2. 关键目录

| 路径 | 作用 |
|---|---|
| `Scenes/DemoScene01.unity` | 主场景：同时含 Classroom 与 City，挂载全部核心脚本（Evaluation / ActionExecutor / Orchestrator / UserStudyController） |
| `Classroom/` | 教室场景独立资源（旧场景 `Classroom/Scenes/Classroom.unity`、`Classroom/Scripts/`） |
| `Scripts/` | 核心 C# 脚本（见下表） |
| `StreamingAssets/Skills/` | 动态 Skill 脚本：通用、`XCharts/`（2D）、`DxR/`（3D） |
| `StreamingAssets/DxRData/` | 数据文件：`education/`（学生成绩）、`city/`（建筑能耗） |
| `Resources/` | 测试用例 `TestCases*.json`、LLM 提示词 `prompts/*.txt`、场景对象数据 `TestCases/TestCase1.txt` |
| `Logs/` | 运行/评测日志（`Logs/Test/v1~v3`、`Logs/UserStudy` 等） |

---

## 3. 核心脚本一览

| 脚本 | 职责 |
|---|---|
| `Scripts/ActionExecutor.cs` | **任务核心**：定义 T1–T8 的 skill 序列；解析并动态执行 Skill API |
| `Scripts/Evaluation.cs` | 评测入口：按键盘 **1–8** 模拟“录音→思考→执行”并触发 T1–T8；另有 LLM 全自动评测（`RunAllTests`） |
| `Scripts/Orchestrator.cs` | 正式语音工作流：STT → VLM → 场景图 → LLM 生成序列 → 执行；负责 UI 反馈与日志 |
| `Scripts/SkillController.cs` | 调用 LLM 生成 Skill 序列；按场景扫描数据文件 |
| `Scripts/UserStudyController.cs` | 场景切换（Classroom / City / Reproduction）、传送点 |
| `Scripts/SpeechToText.cs` | 语音识别（STT） |
| `Scripts/VLMFocus.cs` | VLM 识别用户注视/指向的物体 |
| `Scripts/RelationDetection.cs` | 构建场景图（物体位置、包围盒、空间关系） |
| `Scripts/InteractionTracker.cs` | 手柄/手部射线与 hit point 追踪 |
| `Scripts/BlendCanvas.cs` | 将 2D Canvas 贴到物体表面的工具（Embed 相关） |
| `Scripts/ApiConfig.cs` | LLM API Key / 模型配置 |
| `Scripts/ClientServerCommunication.cs` | 与外部 Python 服务通信（测试用） |

---

## 4. 运行管线

### 4.1 正式语音流程（Orchestrator）

```
语音输入 (SpeechToText)
   │  OnSpeechStarted
   ▼
VLM 识别注视物体 (VLMFocus.IdentifyFocusedObject)
   │
   ▼
构建 userPrompt JSON（用户位置 + 场景图 + hit points + 指令 + 数据文件信息）
   │
   ▼
LLM 生成 Skill 序列 (SkillController.GenerateSkills)
   │  generateSequence 开关（Inspector）
   ▼
ActionExecutor.ExecuteSkillSequence(APICalls)
   │  sequenceToExecutor 开关（Inspector）
   ▼
Roslyn 动态执行 Skills/*.cs → 场景中出现图表
```

> **注意**：完整语音流程依赖 Orchestrator Inspector 上的三个开关——`voiceSendtoVLM`（转写完成后触发工作流，必须开启）、`generateSequence`（调用 LLM 生成序列）、`sequenceToExecutor`（执行生成的序列）。其中 `voiceSendtoVLM` 若关闭，语音转写后流程不会继续（只会在 Console 打印转写结果）。

### 4.2 评测流程（Evaluation）

- **手动评测**：在 `DemoScene01.unity` 中 Play 后按键盘 `1`–`8`，每个数字对应一个任务。`Evaluation.VideoRecording()` 会先模拟“Recording... → Translating... → 显示用户指令 → Thinking...”，再调用 `ActionExecutor.TestCaseT1()~T8()`。
- **LLM 自动评测**：`Evaluation.RunAllTests()` 读取 `Resources/TestCasesComplete.json`（100 条用例，也可切到 `TestCases.json` 的 30 条），向 LLM 发送提示词后执行生成结果，并输出日志到 `Logs/Test/v3/`。

### 4.3 控制器交互（左右手对称）

左右手控制器均可正常使用，交互逻辑为双手对称设计：

| 输入 | 功能 | 实现位置 |
|---|---|---|
| 左手 X / 右手 A（Button.One）按住 | 语音指令 push-to-talk：按住录音，松开发送 | `SpeechToText.cs` |
| 左手 Y / 右手 B（Button.Two）单击 | 循环切换当前场景的传送点 | `UserStudyController.cs` |
| 左手 Y / 右手 B 长按（默认 0.8s） | 重置对话 + 清空当前场景所有可视化 | `UserStudyController.cs` |
| 左摇杆 | 平滑移动（以头显朝向为基准） | `InteractionTracker.cs` |
| 右摇杆（左右推） | 平滑原地转向（`turnSpeed` 默认 40°/s，带指数平滑 `turnSmoothing`，降低眩晕） | `InteractionTracker.cs` |
| 左右手射线 | 指向高亮；语音期间**双手命中物体都记录**为 hit points（供 LLM 理解"这个/那个"） | `InteractionTracker.cs`（`rightPointer` 已绑定 RightHandAnchor） |

说明：`HandLocomotionController` 的"双手捏合前进"属于手部追踪模式，戴控制器时通常不生效，不影响上述手柄操作。键盘等效键：空格=按住说话、WASD+鼠标=移动视角、↓=切换传送点。

**传送点高度说明**：`TeleportToCurrentPoint` 先把 Rig 旋转到目标朝向，再把"眼睛"（CenterEyeAnchor）精确平移到传送点位置。由于 OVRCameraRig 每帧把 `CenterEyeAnchor.localPosition` 设为头显追踪位姿（FloorLevel 下 Y 含离地眼高），传送时必须扣除该偏移，否则视角会比设定点高约一个眼高（表现为"很高的视角"）。如果 Play 后视角仍偏高，请先确认 Meta 守护者/地板高度校准正确。

---

## 5. 两个场景

### 5.1 场景结构

- **Classroom（教室）**：黑板 `Blackboard`、课桌椅 `DeskAndChair_S001` ~ `S025`（任务用到 S001–S012）、时钟、书柜等。
- **City（城市）**：建筑 `building_001` ~ `building_018`（18 栋）。
- **`VisObject`**：所有可视化图表的父容器，场景切换/重置时会清空其子物体（`Orchestrator.ClearAllVisualizations`）。
- 标签约定：XCharts 图表标记为 `Visualization_2D`，DxR 图表标记为 `Visualization_3D`。

### 5.2 场景切换

`UserStudyController`（枚举 `SceneType { Reproduction, Classroom, City }`）通过显示/隐藏 `classroom` / `city` 两个 GameObject 切换场景；切换时会重置对话并清空可视化。左手 Y 键或键盘 ↓ 可循环切换传送点。

---

## 6. 8 个任务（T1–T8）详解

所有任务的 skill 序列都硬编码在 `Scripts/ActionExecutor.cs` 中，方法名与 ContextMenu 对应关系如下：

| 键盘 | ContextMenu | 方法 | 场景 | 说明 |
|---|---|---|---|---|
| `1` | `C1-T1` | `TestCaseT1()` | Classroom | 全班数学成绩柱状图 → 嵌入黑板 |
| `2` | `C1-T2` | `TestCaseT2()` | Classroom | 追加科学、英语成绩系列 |
| `3` | `C1-T3` | `TestCaseT3()` | Classroom | 每个学生课桌上的科目成绩图 |
| `4` | `C1-T4` | `TestCaseT4()` | Classroom | 给所有学生图表改颜色 |
| `5` | `C2-T1` | `TestCaseT5()` | City | 全城用电 **3D 柱状景观**（总览） |
| `6` | `C2-T2` | `TestCaseT6()` | City | 电-水-气 **3D 散点图**（关联分析） |
| `7` | `C2-T3` | `TestCaseT7()` | City | 单栋建筑的电/水/气 3D 柱状图（钻取） |
| `8` | `C2-T4` | `TestCaseT8()` | City | 双建筑用电 **2D 多折线对比**（对比） |

> 任务间存在依赖：**T2 依赖 T1、T4 依赖 T3**（需要先创建目标图表才能追加/改色）。T5–T8 形成一条数据分析链路：**总览（T5）→ 关联分析（T6）→ 钻取（T7）→ 对比（T8）**，建议按顺序执行以获得完整分析体验。

### 6.1 C1-T1（键盘 1）— 全班数学成绩 → 黑板

- 模拟指令：`Show all students' math scores on the blackboard.`
- Skill 序列（XCharts 2D）：

```csharp
CREATE("AllStudentsMainScoresChart", "education/student_scores.json", "2d_bar", "student_id", "math_score");
EMBED("AllStudentsMainScoresChart", "Blackboard");
```

- 效果：创建全班学生数学成绩柱状图，并平面映射到黑板表面。
- 涉及 Skill：`XCharts/Create.cs`、`Embed.cs`。

### 6.2 C1-T2（键盘 2）— 追加科学/英语成绩

- 模拟指令：`Append science and English scores to the math chart.`
- Skill 序列（XCharts 2D）：

```csharp
APPEND_SERIES("AllStudentsMainScoresChart", "education/student_scores.json", "student_id", "science_score", "bar");
APPEND_SERIES("AllStudentsMainScoresChart", "education/student_scores.json", "student_id", "english_score", "bar");
```

- 效果：在 T1 的图表上追加 science_score 与 english_score 两个柱状系列。
- 涉及 Skill：`XCharts/AppendSeries.cs`。

### 6.3 C1-T3（键盘 3）— 每个学生课桌上的科目成绩图

- 模拟指令：`Show each student's subject scores on their desk.`
- Skill 序列（XCharts 2D）：

```csharp
DATA_TRANSFORM("education/student_scores.json", "student_id", "subject", "score", ["name"]);
// 对 S001–S012 各重复：
CREATE("StudentScores_S001", "education/student_scores_S001.json", "2d_bar", "subject", "score");
ADAPT_POS("StudentScores_S001", "DeskAndChair_S001", 0.1f, 0.1f);
ORIENT_TO("StudentScores_S001", "User");
// ... S002~S012 同理（不同课桌高度偏移 0.1f/0.2f/0.3f）
```

- 效果：`DATA_TRANSFORM` 将宽表（每行一个学生）拆成 12 个长表 `student_scores_S001.json` ~ `S012.json`（字段变为 subject / score）；随后为每个学生在对应课桌上方创建科目成绩柱状图，并朝向用户。
- 涉及 Skill：`DataTransform.cs`、`XCharts/Create.cs`、`AdaptPos.cs`、`OrientTo.cs`。

### 6.4 C1-T4（键盘 4）— 批量改色

- 模拟指令：`Recolor science and English scores across all charts.`
- Skill 序列（XCharts 2D，对 S001–S012 全部执行）：

```csharp
CHANGE_DATA_COLOR("StudentScores_S001", "score", "science", "#8AC471FF");  // 绿色
CHANGE_DATA_COLOR("StudentScores_S001", "score", "english", "#F7C15BFF");  // 橙色
// ... S002~S012 同理
```

- 效果：把每个学生图表中 science 数据点改为绿色、english 数据点改为橙色。
- ⚠️ **注意**：代码实际设置的是 **science→绿、english→橙**（新模拟指令 `Recolor science and English scores across all charts.` 不指定具体颜色，与代码一致）。
- 涉及 Skill：`XCharts/ChangeDataColor.cs`。

### 6.5 C2-T1（键盘 5）— 全城用电 3D 柱状景观（总览）

- 模拟指令：`Show city-wide electricity usage in 3D.`
- Skill 序列（DxR 3D，一张图代替原来的 18 张折线图）：

```csharp
DATA_TRANSFORM("city", "building", type: "merge");
CREATE("CityElectricityLandscape", "city/city_all.json", "3d_bar", "time", "electricity", "electricity", "quantitative", "building");
SCALE("CityElectricityLandscape", 3f, 3f, 3f);
ADAPT_POS("CityElectricityLandscape", "User", 1.5f, 0.2f);
ORIENT_TO("CityElectricityLandscape", "User");
```

- 数据：先由 `DATA_TRANSFORM("city", "building", type: "merge")` 把 18 个 `building_XXX.json` 运行时合并为 `city/city_all.json`（108 行，字段 `building / time / electricity / water / gas / footfall`），再创建图表。
- 效果：**x=time、z=building、y=electricity** 的 3D 柱状网格，柱高表示用电量、**颜色（quantitative ramp）表示用电高低**，一眼看出"哪个时段、哪栋楼用电最高"。
- 涉及 Skill：`DxR/Create.cs`（z 通道 + 量化颜色）、`Scale.cs`、`AdaptPos.cs`、`OrientTo.cs`。
- 细节调整：3D 图用 `ADAPT_POS(..., "User", ...)` 放置时会**自动将整张图的中心对准目标点**（不再以数据原点/左下角为锚）；DxR 渐变图例的刻度文字已向外偏移，避免与 LegendMark 重叠（见 `DxR/Resources/Legend/Legend.cs`）。

### 6.6 C2-T2（键盘 6）— 电-水-气 3D 散点图（关联分析）

- 模拟指令：`Explore correlations between utilities.`
- Skill 序列（DxR 模式）：

```csharp
DATA_TRANSFORM("city", "building", type: "merge");
CREATE("UtilityCorrelation3D", "city/city_all.json", "3d_scatter", "electricity", "water", "electricity", "quantitative", "gas");
SCALE("UtilityCorrelation3D", 3f, 3f, 3f);
LAYOUT(["CityElectricityLandscape", "UtilityCorrelation3D"], 1.50f, 0.2f, "arc");
ORIENT_TO("CityElectricityLandscape", "User");
ORIENT_TO("UtilityCorrelation3D", "User");
```

- 数据：复用 T5 合并生成的 `city/city_all.json`（若单独执行 T6 会先执行 merge 类型的 DATA_TRANSFORM）。
- 效果：**x=electricity、y=water、z=gas** 的 3D 散点，每个点代表"某建筑某时段的三种能耗组合"，颜色按用电量（quantitative ramp）渐变色，用于探索电/水/气之间是否同涨同跌；同时把 T5 的 `CityElectricityLandscape` 一起放入 **arc 布局**，两张图并排、分别朝向用户，便于总览图与关联图对照查看。
- 涉及 Skill：`DxR/Create.cs`（scatter + z + 量化颜色 + 固定点大小）、`Scale.cs`、`Layout.cs`、`OrientTo.cs`。
- 细节调整：散点通过 `size` 通道固定放大（`{"value": 30}`，约 3cm/点，再乘 SCALE 3x），让点在 VR 中更醒目；`LAYOUT` 会把两张 3D 图的高度统一归一化到 1m，并将包围盒中心对准布局槽位（与 ADAPT_POS 的居中逻辑一致）。

### 6.7 C2-T3（键盘 7）— 单栋建筑 3D 柱状图

- 模拟指令：`Show a building's electricity, water, and gas in 3D.`
- Skill 序列（DxR 3D）：

```csharp
CREATE("ElectricityChart_building_001", "city/building_001.json", "3d_bar", "time", "electricity");
CREATE("WaterChart_building_001",   "city/building_001.json", "3d_bar", "time", "water");
CREATE("GasChart_building_001",     "city/building_001.json", "3d_bar", "time", "gas");
LAYOUT(["ElectricityChart_building_001", "WaterChart_building_001", "GasChart_building_001"], 1.20f, 0.40f, "arc");
POSITION("ElectricityChart_building_001", -2.0f, 2.0f, 0.01f);
POSITION("WaterChart_building_001",       -1.0f, 2.0f, 0.01f);
POSITION("GasChart_building_001",          0.0f, 2.0f, 0.01f);
ORIENT_TO("ElectricityChart_building_001", "User");
ORIENT_TO("WaterChart_building_001", "User");
ORIENT_TO("GasChart_building_001", "User");
```

- 效果：为 building_001 创建电/水/气三张 3D 柱状图，弧形布局并设置绝对位置，全部朝向用户。
- 涉及 Skill：`DxR/Create.cs`、`Layout.cs`、`Position.cs`、`OrientTo.cs`。

### 6.8 C2-T4（键盘 8）— 双建筑用电 2D 多折线对比

- 模拟指令：`Compare electricity usage between two buildings.`
- Skill 序列（XCharts 2D）：

```csharp
CREATE("ElectricityCompareChart", "city/building_001.json", "2d_line", "time", "electricity", "building_001");
APPEND_SERIES("ElectricityCompareChart", "city/building_005.json", "time", "electricity", "line", "building_005");
CHANGE_SERIE_COLOR("ElectricityCompareChart", "building_001", "#4A90E2FF");
CHANGE_SERIE_COLOR("ElectricityCompareChart", "building_005", "#E95E4FFF");
ADAPT_POS("ElectricityCompareChart", "User", 1.2f, 0.2f);
ORIENT_TO("ElectricityCompareChart", "User");
```

- 效果：对比 building_001 与 building_005 的用电曲线，**每个建筑一条线**（`serie_name` 命名 + `CHANGE_SERIE_COLOR` 区分颜色），放在用户面前。
- 说明：`CREATE` 与 `APPEND_SERIES` 新增了可选的 `serie_name` 参数（第 6 个），用于图例命名；不传则保持原有行为（系列名 = y 字段名）。多系列（传入 `serie_name`）时自动显示 **Legend** 图例。
- 涉及 Skill：`XCharts/Create.cs`、`XCharts/AppendSeries.cs`、`XCharts/ChangeSerieColor.cs`、`AdaptPos.cs`、`OrientTo.cs`。

---

## 7. Skill 系统（ActionExecutor 执行机制）

### 7.1 可用 Skill API

（完整定义见 `Resources/prompts/TestPromptAPI.txt` / `SkillControllerSystemPrompt.txt`）

**上下文级**：
- `ORIENT_TO(view_id, object_id)` — 让图表朝向目标（`"User"` 表示用户/相机）
- `ADAPT_POS(view_id, object_id, distance, height_offset)` — 将图表放到物体上方/前方指定偏移处
- `EMBED(view_id, object_id)` — 把 2D 图表平面映射到物体表面

**视图级**：
- `CREATE(view_id, data, chart_type, x_field, y_field, color_field = "", color_type = "", z_field = "")` — 创建图表（`2d_bar/2d_line/2d_pie/3d_bar/3d_scatter` 等）；3D 图表可用 `z_field` 编码第三个数据维度，用 `color_field + color_type("quantitative"/"nominal")` 编码颜色
- `DELETE(view_id)` / `POSITION(view_id, x, y, z)` / `ROTATE(...)` / `SCALE(...)`
- `LAYOUT(List<view_id>, distance, height_offset, "arc"|"grid")` — 批量布局

> **SCALE 语义**：`SCALE(view_id, x, y, z)` 的参数是图表的**绝对目标 localScale**（0 = 该轴保持不变），例如当前 localScale 为 (3,3,3)、要放大 50% 时应输出 `SCALE(..., 4.5f, 4.5f, 4.5f)`。prompt 中要求 LLM 从对话历史追踪每个图表的当前 localScale（3D 初始为 1 + 之前 SCALE 调用），并**输出计算后的具体数值**（factor × 当前值），禁止输出裸倍率或符号表达式。2D 图表"贴合黑板/墙面"应使用 `EMBED`（自动适配表面大小）。

**元素级**：
- `UPDATE` / `DELETE_ELEMENT` / `APPEND_SINGLE` / `APPEND_SERIES(chart_id, data, x_field, y_field, serieType, serie_name = "")`（`serie_name` 用于自定义系列/图例名）
- `HIGHLIGHT` / `CHANGE_SERIE_COLOR` / `CHANGE_DATA_COLOR`

**系统级**：
- `MESSAGE(content)` — 仅显示文本反馈
- `DATA_TRANSFORM(inputPath, idField, labelName = "item", valueName = "value", excludeFields = null, type = "split", includeFields = "")` — 通过 `type` 选择模式：
  - `type = "split"`（默认）：按 `idField` 把单个宽表拆成多个长表 JSON；
  - `type = "merge"`：把 `DxRData/{inputPath}` 文件夹下所有 JSON 合并为 `{文件夹名}_all.json`（如 city → city/city_all.json），每行补 `idField`（= 源文件名），`includeFields` 可只保留指定字段（逗号分隔，留空保留全部）

### 7.2 执行流程

1. `ExecuteSkillSequence(skillOutput)` 用正则 `(\w+)\s*\((.*?)\);` 逐条匹配 `函数名(参数);`。
2. **CREATE 自动路由**：解析第 3 个参数 chart_type，含 `2d` → 切换到 XCharts，含 `3d` → 切换到 DxR；同时把 `2d_`/`3d_` 前缀去掉后传回参数。
3. 函数名下划线转大驼峰：`ORIENT_TO` → `OrientTo`、`CREATE` → `Create`。
4. 查找脚本：`StreamingAssets/Skills/{ClassName}.cs` → `StreamingAssets/Skills/{skillsFolder}/{ClassName}.cs`。
5. 用 Roslyn 编译执行拼接后的代码：`{Skill源码}\n{ClassName}.Execute({args});`。

### 7.3 2D 与 3D 的差异

- **XCharts（2D）**：`Create` 创建 Canvas + LineChart/BarChart/PieChart 等组件，挂到 `VisObject` 下，tag 为 `Visualization_2D`。
- **DxR（3D）**：`Create` 把 spec JSON 写到 `StreamingAssets/DxRSpecs/`，再实例化 `Assets/DxR/Prefabs/DxRVis.prefab`，tag 为 `Visualization_3D`，并计算合并 BoxCollider。
- DxR 原生支持 **x/y/z 三通道**：`3d_bar` 的 z（nominal）会自动映射为柱子 depth，`3d_scatter` 的 z（quantitative）作为第三坐标轴；color 通道不写 scale 时 DxR 会自动推断配色（nominal→tableau10 分类色，quantitative→ramp 渐变色）。
- `ADAPT_POS(..., "User", ...)` 对 3D 图表会把**包围盒中心**对准目标点（见 `StreamingAssets/Skills/AdaptPos.cs`）。
- 注意 DxR 的 line 类型实际用 `tick` mark 近似（DxR 无原生 line mark）。

**2D 轴标签与图例细节**：
- XCharts 的 x 轴标签**按类别数量自适应字号**（见 `XCharts/Create.cs`）：类别多（如 T1/T2 的 14 名学生）→ 缩小到 10 并倾斜 45°；类别少（如 T3 每学生 3-4 个科目、T8 的 6 个时段）→ 放大到 24，保证 VR 中清晰可读。
- 多系列 2D 图表（传入 `serie_name`）会自动显示 `Legend`。

**3D 朝向细节**：
- `ORIENT_TO` 计算朝向时使用图表的**渲染包围盒中心**而非 transform 原点（DxR 图表原点在数据坐标 0,0,0，即左下角），避免"用左下角对准用户"导致的倾斜（见 `StreamingAssets/Skills/OrientTo.cs`）。
- 因此 T5/T6 的放置顺序保持 **先 `ADAPT_POS`（把图表中心放到目标点）再 `ORIENT_TO`（从图表中心朝向用户）**，这样朝向是几何精确的；若反过来在创建位置（场景原点，远离相机）先朝向，再移动到用户面前，反而会产生更大的视差倾斜。

---

## 8. 数据文件

| 文件 | 内容 |
|---|---|
| `StreamingAssets/DxRData/education/student_scores.json` | 14 名学生：`student_id`、`name`、`math_score`、`science_score`、`english_score` |
| `StreamingAssets/DxRData/education/student_scores_S001~S012.json` | DATA_TRANSFORM 生成的长表（`subject`、`score`），每学生一个文件 |
| `StreamingAssets/DxRData/city/building_001~018.json` | 18 栋建筑，每栋 6 条时间记录：`time`、`electricity`、`water`、`gas`、`footfall`；建筑按类型区分：001–004 住宅（傍晚峰值）、005–010 办公（白天峰值，005 为大型办公楼、010 为 16:00 峰值）、011–015 工业（全天高位，015 为夜班型 04:00 峰值）、016–018 商业（晚间峰值、人流最大），各楼量级不同 |
| `StreamingAssets/Skills/DataTransform.cs` | DATA_TRANSFORM skill：`type="split"` 拆分为多文件（默认）、`type="merge"` 运行时把 18 个 building 文件合并成 `city/city_all.json`（T5/T6 在 CREATE 前调用；不再预生成） |
| `StreamingAssets/DxRData/sales/` | 销售数据（Reproduction 场景用） |

---

## 9. 修改指南（改哪里）

| 想改什么 | 改哪里 |
|---|---|
| 某个任务创建/布局/改色的具体指令 | `Scripts/ActionExecutor.cs` 中对应的 `TestCaseT1()~T8()`（`skillSequence` 字符串） |
| 键盘触发、模拟语音/思考文案 | `Scripts/Evaluation.cs` 的 `VideoRecording()`（键盘 1–8 分支） |
| Skill 的具体实现（如 Create 的图表样式、Layout 算法） | `StreamingAssets/Skills/` 下对应 `.cs`（XCharts/ 与 DxR/ 分离） |
| 数据内容 | `StreamingAssets/DxRData/education/`、`city/` 下的 JSON |
| LLM 提示词 / API 文档 | `Resources/prompts/TestPromptAPI.txt`、`TestPromptGeneral.txt`、`SkillControllerSystemPrompt.txt` |
| 场景切换、传送点 | `Scripts/UserStudyController.cs`；场景对象在 `Scenes/DemoScene01.unity` |
| 新增任务 | ① 在 `ActionExecutor` 加 `TestCaseTX()`（含 `[ContextMenu]`）；② 如需键盘触发，在 `Evaluation.VideoRecording()` 加分支 |
| 自动评测用例 | `Resources/TestCasesComplete.json`（或改 `Evaluation.LoadTestCases()` 切换 `TestCases.json`） |

---

## 10. 注意事项 / 已知问题

1. **T4 配色说明**：代码将 science 数据点设为绿色（#8AC471FF）、english 设为橙色（#F7C15BFF）；新版模拟指令 `Recolor science and English scores across all charts.` 不指定具体颜色，避免了口径不一致。
2. **T5/T6 使用 3D x/y/z 编码**：合并文件 `city_all.json` 不再预生成，而是由 `DATA_TRANSFORM("city", "building", type: "merge")` 在运行时动态生成（T5/T6 序列开头已包含该调用）；若单独执行 T5/T6 请确保先运行 merge。
3. **运行时文件自动清理**：`DATA_TRANSFORM`（split/merge）在 play 期间新增的数据文件（写入前不存在的，如 `city/city_all.json`）会在**停止运行时自动删除**（`Scripts/RuntimeFileRegistry.cs` 登记，`ActionExecutor.OnDisable` 清理，连同 .meta）。仓库中已有的预生成文件（如 `student_scores_S001.json`）即使被覆盖也不会被删除。
4. **语音流程 POST 400 排查**：已确认根因——语音期间 `InteractionTracker` 每帧都往 `_recordedHitDetails` 追加命中点，说话几十秒可积累几千条（实测 6800+ 条、约 360 万字符），全量塞进 user prompt 导致 400（Test Workflow 无语音记录所以正常）。已修复：① 命中点改为**变化去重 + 上限 30 条**（`InteractionTracker.RecordHitPoint`）；② 命中点坐标改为四舍五入的匿名 `{x,y,z}`，消除 Vector3 的 normalized/magnitude 膨胀（`RoundToObj`）；③ `RelationDetection` 场景图两两关系截断到 80 条；④ `SkillController` 失败日志保存到 `Assets/Logs/SkillController/API_Error_*.txt` 并记录消息总字符数。
5. **T3 的 DATA_TRANSFORM 路径有空格**：`"education / student_scores.json"`（斜杠两侧有空格）会导致文件找不到并静默返回；但仓库已预生成 `student_scores_S001~S012.json`，所以后续 CREATE 仍能成功。若删除生成文件后重新执行 T3 需先修复该路径。
6. **T3 只用到 S001–S012**：数据源有 14 名学生，S013/S014 未参与。
7. **任务依赖**：T2←T1、T4←T3；T5–T8 建议按顺序执行（总览→关联→钻取→对比）以获得完整分析体验，但每个任务自身均可独立创建图表。
8. **构建列表**：`ProjectSettings/EditorBuildSettings.asset` 目前只包含旧的 `SampleScene.unity`；实际开发/评测应打开 `Scenes/DemoScene01.unity`。
9. `ActionExecutor.OnDisable()` 会取消所有 Roslyn 任务并触发 GC，场景停止/切换时可能有短暂卡顿属正常。
10. **细节微调点**：DxR 图例 tick 文字偏移量在 `DxR/Resources/Legend/Legend.cs`（`pos.y`/首尾 `pos.x`）；散点大小在 `DxR/Create.cs` 的 scatter 分支 `size.value`；x 轴标签字号/倾斜在 `XCharts/Create.cs`（>8 类别 → 10px+45°，否则 24px）；3D 朝向参考点在 `StreamingAssets/Skills/OrientTo.cs`（包围盒中心）。
11. **VR 卡顿优化**：`ExecuteSkillSequence` 保持**每个函数单独编译、单独执行**（单个函数出错只影响它自己，后续函数继续执行），同时用 `_compiledSkillCache` 按 Skill 文件缓存编译结果——同一个 Skill 文件只完整编译一次，后续调用通过 Roslyn `ContinueWith` 只追加编译一行调用代码，显著减少重复编译开销；每步之间 `await Task.Yield()` 让出一帧。剩余瓶颈：DxR 单张图一次创建上百个 mark 仍在同一帧完成，若仍感明显冻结，可进一步做 prefab/着色器预热或把 mark 创建分批到多帧。

---

## 附录：NuGetForUnity 包管理器安装（原有说明）

### 推荐方法：安装NuGetForUnity包管理器（github）

1. 安装NuGetForUnity包管理器（github）：
   - 在Unity中打开 `Window > Package Manager`
   - 点击左上方 `+` 按钮并选择`Add package from git URL...`
   - 输入网址 `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity` 并点击添加按钮
   - 重启unity
2. 在Unity中打开 `Window > NuGet > Manage NuGet Packages`
3. 搜索并安装：
   - Microsoft.CodeAnalysis.CSharp (5.0.0)
   - Microsoft.CodeAnalysis (5.0.0)
   - Microsoft.CodeAnalysis.CSharp.Scripting
   - Microsoft.CodeAnalysis.Scripting;
4. 重启Unity
5. Console可能会报错类似：
   - Assembly 'Assets/Packages/Microsoft.CodeAnalysis.VisualBasic.Workspaces.5.0.0/lib/netstandard2.0/Microsoft.CodeAnalysis.VisualBasic.Workspaces.dll' will not be loaded due to errors:
Reference has errors 'Microsoft.CodeAnalysis.Workspaces'.
   - *解决方法*：直接删掉所有 `*Workspaces*.dll` 文件，或者选中每个 DLL，在 Inspector 里：取消所有平台（勾选 “Any Platform” 去掉，所有 Platform 都不选），让 Unity 不再把它们当插件加载。
6. 无法安装：检查网络；关闭防火墙；手动安装。
