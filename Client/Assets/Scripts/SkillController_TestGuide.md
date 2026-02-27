# SkillController 测试指南

## 概述
本测试框架专为**没有佩戴头盔**的开发环境设计，所有测试用例都不包含 `hit_points` 数据，适合在Unity Editor中进行调试和验证。

## ✨ 新特性：数据文件系统

从现在开始，CREATE函数支持直接引用数据文件路径，而不是生成空的JSON模板：

**旧方式**：
```
CREATE("chart1", "{\"type\":\"chart\",\"data\":{}}");
```

**新方式**：
```
CREATE("chart1", "DataFiles/sales/monthly_sales.json");
```

### 数据文件配置

在SkillController的Inspector中，你可以配置可用的数据文件：

1. **Data Configuration** 区域显示所有可用的数据文件
2. 每个文件包含：
   - `file`: 数据文件路径（相对于Resources文件夹）
   - `description`: 数据描述，用于帮助LLM选择合适的数据

3. 在场景启动时，这些数据信息会自动添加到System Prompt的尾部

### 示例数据文件

系统内置了三个示例数据文件：

1. **DataFiles/sales/monthly_sales.json**
   - 描述：月度销售数据（包含产品类别）
   - 内容：1-6月的电子产品销售额

2. **DataFiles/sales/quarterly_revenue.json**
   - 描述：季度收入数据（按地区）
   - 内容：Q1-Q3三个地区的收入数据

3. **DataFiles/education/student_scores.json**
   - 描述：学生测试成绩和年级
   - 内容：6名学生的数学、科学、英语成绩

## 测试功能

### 🎯 内置测试案例

#### 1. Test Case 1 - 完整场景（自定义输入）
- **触发方式**: 右键点击 SkillController 组件 → `Test Case 1 - 完整场景`
- **使用方法**: 
  1. 在Inspector的 `userinput` 字段输入你的测试指令
  2. 右键触发测试
- **场景**: 使用完整的教室场景（5个对象）
- **示例输入**: 
  - "在讲台前面创建一个柱状图"
  - "把图表移到黑板旁边"

#### 2. Test Case 2 - 创建月度销售图表
- **预设指令**: "在讲台上方创建一个月度销售数据的条形图"
- **目的**: 测试基本的可视化创建功能，使用真实数据文件
- **使用数据**: DataFiles/sales/monthly_sales.json

#### 3. Test Case 3 - 在黑板上嵌入季度收入
- **预设指令**: "在黑板上嵌入一个显示季度收入数据的图表"
- **目的**: 测试 EMBED 功能，将2D可视化嵌入到平面表面
- **使用数据**: DataFiles/sales/quarterly_revenue.json

#### 4. Test Case 4 - 删除可视化
- **预设指令**: "删除所有的图表"
- **目的**: 测试删除功能

#### 5. Test Case 5 - 创建学生成绩图表
- **预设指令**: "创建一个显示学生成绩的图表"
- **目的**: 测试学生数据可视化
- **使用数据**: DataFiles/education/student_scores.json

#### 6. Test Case 6 - 自定义输入（高级）
- **使用方法**:
  1. 在 `userinput` 字段输入测试指令
  2. （可选）在 `testCaseFile` 字段指定不同的测试数据文件
  3. 右键触发测试
- **用途**: 灵活测试不同场景配置

#### 7. Test Case 7 - 调整现有图表
- **预设指令**: "把chart_1向左移动2米，并放大1.5倍"
- **目的**: 测试对已存在可视化的修改操作
- **注意**: 需要先有名为 chart_1 的图表

#### 8. Test Case 8 - 多数据源布局
- **预设指令**: "在教室前方横向创建三个图表：月度销售、季度收入和学生成绩"
- **目的**: 测试同时使用多个数据文件创建多个可视化
- **使用数据**: 所有三个示例数据文件

#### 9. Test Case 9 - 测试数据文件引用
- **预设指令**: "使用月度销售数据创建一个条形图在讲台前面"
- **目的**: 明确测试数据文件引用功能
- **使用数据**: DataFiles/sales/monthly_sales.json

### 🛠️ 调试工具

#### Debug - 打印相机状态
- **功能**: 显示当前主相机的位置、朝向等信息
- **用途**: 理解用户视角，调试位置相关问题
- **输出示例**:
  ```
  ===== 相机状态 =====
  位置: (0.00, 1.60, 2.50)
  前方: (0.00, 0.00, -1.00)
  右侧: (1.00, 0.00, 0.00)
  上方: (0.00, 1.00, 0.00)
  ```

#### Debug - 打印对话历史
- **功能**: 显示当前的对话历史记录
- **用途**: 检查多轮对话状态，调试上下文问题

#### Reset Conversation
- **功能**: 重置对话历史，清空之前的上下文
- **用途**: 在测试新的独立场景前清空状态

## Inspector 配置参数

### userinput (文本框)
- **类型**: 多行文本
- **用途**: 输入自定义的测试指令
- **示例**:
  ```
  在黑板旁边创建一个散点图
  把所有图表向右移动1米
  删除chart_2
  ```

### testCaseFile (字符串)
- **类型**: 文本
- **默认值**: `TestCases/TestCase1`
- **用途**: 指定要使用的测试数据文件
- **格式**: Resources文件夹下的相对路径（无需.txt扩展名）

### lastResponse (只读)
- **类型**: 多行文本（只读）
- **用途**: 显示最近一次LLM的返回结果
- **内容**: 生成的技能序列API调用

## 工作流程

### 典型测试流程

```
1. 启动Unity，打开包含SkillController的场景
   ↓
2. 选择场景中附加了SkillController的GameObject
   ↓
3. 在Inspector中填写userinput字段
   ↓
4. 右键点击组件，选择相应的Test Case
   ↓
5. 等待API调用完成
   ↓
6. 在Console查看日志输出
   ↓
7. 在lastResponse字段查看生成的技能序列
   ↓
8. 在 Assets/Logs/SkillController 文件夹查看完整日志
```

### 无hit_points的数据结构

测试生成的用户提示JSON格式：
```json
{
  "user_status": {
    "position": { "x": 0.0, "y": 1.6, "z": 2.5 },
    "forward": { "x": 0.0, "y": 0.0, "z": -1.0 },
    "right": { "x": 1.0, "y": 0.0, "z": 0.0 }
  },
  "focused_objects": [...],
  "hit_points": [],  // ← 空数组，因为没有佩戴头盔
  "user_request": "用户的指令"
}
```

## 测试数据文件

### TestCase1.txt
- **对象数量**: 5个
- **包含对象**: 
  - Clock (时钟)
  - Blackboard (黑板)
  - Bookcase (书柜)
  - Globe (地球仪)
  - TeacherDesk (讲台)

## 日志系统

### 日志位置
- **路径**: `Client/Assets/Logs/SkillController/`
- **命名格式**: `Skills_yyyyMMdd_HHmmss.txt`
- **示例**: `Skills_20260226_141630.txt`

### 日志内容
每次测试调用会记录：
```
--- User Request ---
{完整的JSON输入}

--- Generated Skill Sequence ---
{生成的API调用序列}
```

## 注意事项

1. **相机要求**: 场景中必须有Main Camera
2. **API配置**: 确保 `ApiConfig` 已正确配置 OpenAI API 密钥
3. **网络连接**: 需要能够访问 OpenAI API
4. **异步操作**: 测试是异步的，请等待完成
5. **错误处理**: 查看Console中的错误信息

## 添加自定义数据文件

### 步骤1：创建数据JSON文件

在 `Client/Assets/Resources/DataFiles/` 目录下创建你的数据文件：

```json
{
  "title": "My Custom Data",
  "chart_type": "bar",
  "data": [
    {"x": "A", "y": 100},
    {"x": "B", "y": 200}
  ]
}
```

### 步骤2：在Inspector中注册

1. 在Unity中选择SkillController对象
2. 在Inspector的 **Data Configuration** 区域
3. 修改 `Available Data Files` 数组大小
4. 添加新条目：
   - **File**: `DataFiles/yourfolder/yourfile.json`
   - **Description**: 描述数据内容，帮助LLM选择

### 步骤3：重置对话

右键点击SkillController组件，选择 **Reset Conversation**，新的数据文件信息会被加载到System Prompt中。

## 常见问题

### Q: 测试没有反应？
A: 检查：
- Console中是否有错误信息
- userinput字段是否为空
- API配置是否正确
- 网络连接是否正常

### Q: 如何查看LLM实际收到的数据文件信息？
A: 使用右键菜单中的 **Debug - 打印对话历史**，查看System Prompt内容。

### Q: CREATE函数还能使用旧的JSON格式吗？
A: 目前LLM被指示优先使用数据文件路径。如需支持内嵌JSON，需要在System Prompt中说明。

### Q: 如何添加自定义测试数据？
A: 
1. 在 `Client/Assets/Resources/TestCases/` 创建新的 .txt 文件
2. 按照 TestCase1.txt 的JSON格式编写
3. 在 testCaseFile 字段填入新文件路径（不含扩展名）

### Q: 如何理解生成的技能序列？
A: 参考 SkillControllerSystemPrompt.txt 中的 API 定义，每行是一个API调用。现在CREATE的第二个参数应该是数据文件路径。

## 扩展建议

如果需要添加更多测试场景，可以：
1. 创建新的测试数据文件
2. 在代码中添加新的 `[ContextMenu]` 方法
3. 调用 `RunTestCase(文件路径, 指令)` 方法

示例：
```csharp
[ContextMenu("Test Case 9 - 我的自定义测试")]
private async void TestCase9()
{
    string testRequest = "我的自定义指令";
    _ = await RunTestCase("TestCases/MyCustomCase", testRequest);
}
```
