---
name: review-pr
description: |
  GitHub PR 代码审查技能，适用于 C# WPF Modbus 模拟器项目。
  当用户说"审查PR"、"review PR"、"review一下"、"帮我看看这个PR"、"代码审查"、
  "check PR"、"review #123"、"看看这次改动有没有问题"时，必须使用此技能。
  自动拉取 PR diff，从四个维度审查（代码逻辑、线程安全、WPF/MVVM规范、异常处理），
  在对话中输出审查摘要，同时生成完整 Markdown 审查报告保存到项目目录。
---

# PR 代码审查技能

## 执行流程

### 第一步：获取 PR 信息

**如果用户提供了 PR 编号（如 `#42`）：**
```bash
gh pr view 42 --json number,title,author,body,baseRefName,headRefName,changedFiles,additions,deletions
gh pr diff 42
```

**如果用户没提供编号，查看当前分支关联的 PR：**
```bash
gh pr view --json number,title,author,body,baseRefName,headRefName,changedFiles,additions,deletions
gh pr diff
```

**如果没有远程 PR（纯本地分支对比）：**
```bash
git log main..HEAD --oneline          # 本次分支包含的提交
git diff main...HEAD                  # 与主分支的完整差异
git diff main...HEAD --stat           # 改动文件统计
```

---

### 第二步：审查改动（四个维度）

逐文件阅读 diff，对每个维度单独评估。

---

#### 维度一：代码逻辑与正确性

重点检查 Modbus 模拟器的领域特定逻辑：

**寄存器映射正确性：**
- `BaseAddress` 是否与设计文档（`设备故障模拟器_设计文档.md` 第9节）一致
- `ToRegisters()` 中各字段的寄存器偏移量是否按协议顺序排列，有无跳位或重叠
- 比例系数（scale factor）是否正确：写入时 `value / scale`，读出时 `value * scale`

**float32 字序：**
- 必须使用 AB CD 字序（Big-Endian 字，Little-Endian 字节）
- 标准写法：`bank.WriteFloat32(addr, (float)(value / scale))`
- 反例（错误）：直接用 `BitConverter.GetBytes` 但未按 AB CD 拆分

**bitmask 告警字合并：**
- 多选 CheckBox 的 bitmask OR 合并是否完整
- 是否有 bit 被遗漏或重复

**ComboBox 枚举值：**
- 下拉绑定的 int 值是否与协议中定义的枚举编码一致（0=待机/1=运行 等）

---

#### 维度二：线程安全

此项目有两个并发线程：UI主线程 + Modbus监听线程（Task）。

**RegisterBank 访问：**
- 所有 `_regs[]` 读写是否都在 `lock (_lock)` 内
- 不允许在 lock 外直接访问 `_regs[]`

**UI 更新（从 Modbus 线程回调到 UI）：**
- Modbus 线程中触发的 UI 更新必须通过 `Dispatcher.BeginInvoke` 或 `Application.Current.Dispatcher.InvokeAsync`
- 日志 `ObservableCollection` 的 Add 操作若在非UI线程调用，必须派发到 UI 线程

**Task 异常观察：**
- `async Task` 方法是否被 `await` 或 `.ContinueWith` 观察异常
- 未被观察的 Task 异常会触发 `TaskScheduler.UnobservedTaskException`

---

#### 维度三：WPF / MVVM 规范

**CommunityToolkit.Mvvm 用法：**
- 字段是否用 `[ObservableProperty]` 声明（私有、下划线前缀、camelCase）
- 命令是否用 `[RelayCommand]` 声明，不手动 new RelayCommand
- 属性变更回调用 `partial void OnXxxChanged(T value)`，不在 setter 里写逻辑

**XAML 绑定：**
- `TextBox` 数值绑定是否有 `UpdateSourceTrigger=PropertyChanged`
- 双向绑定是否声明 `Mode=TwoWay`（TextBox 默认 TwoWay，ComboBox 需显式写）
- `ItemsSource` 和 `SelectedValue` / `SelectedItem` 是否配对正确

**代码后台（Code-Behind）：**
- View 的 `.xaml.cs` 中不应包含业务逻辑，只允许 UI 事件转发到 ViewModel 命令
- 不应在 Code-Behind 直接访问 ViewModel 属性赋值

**资源和样式：**
- 硬编码颜色/尺寸是否应改为引用 `Resources/Colors.xaml` / `Resources/Styles.xaml`

---

#### 维度四：异常处理完整性

对照设计文档 6.5 节的异常分类，检查以下场景是否有处理：

| 场景 | 期望处理方式 |
|---|---|
| TCP 端口占用 | catch `SocketException`，弹窗提示，不崩溃 |
| 串口不存在/占用 | catch `IOException` / `UnauthorizedAccessException` |
| TextBox 输入非数字 | 属性 setter 校验，拒绝写入，字段边框变红 |
| 数值超量程 | 截断到边界值，日志追加 WARN |
| JSON 快照格式错误 | catch `JsonException`，弹窗提示 |
| Modbus 通信异常 | try-catch 包裹请求处理，记录 ERROR，不崩溃 |
| 文件写入无权限 | catch `UnauthorizedAccessException`，弹窗提示 |

**全局兜底：**
- `App.xaml.cs` 中是否注册了 `DispatcherUnhandledException`
- 是否注册了 `TaskScheduler.UnobservedTaskException`

---

### 第三步：生成审查结果

#### 3.1 对话内输出审查摘要

在对话中直接输出以下结构：

```
## PR #<编号> 审查摘要：<PR标题>

**改动规模：** +<additions> / -<deletions>，<changedFiles> 个文件

### 🔴 必须修改（Blocking）
- [文件名:行号] 问题描述

### 🟡 建议改进（Non-blocking）
- [文件名:行号] 建议描述

### 🟢 值得肯定
- 做得好的地方

### 总体评价
一段综合评价，结论：通过 / 需要修改后通过 / 不通过
```

#### 3.2 生成完整 Markdown 报告文件

报告保存路径：`E:/MoubusSimulator/.reviews/pr-<编号>-<日期>.md`

报告包含：
- PR 基本信息（编号/标题/作者/分支/改动规模）
- 四个审查维度的详细发现（每条问题附文件名+行号+问题描述+修改建议）
- 审查通过/不通过结论
- 审查时间戳

**文件命名示例：** `.reviews/pr-42-2026-03-23.md`
**若为本地分支审查：** `.reviews/local-<分支名>-<日期>.md`

---

### 第四步：可选后续操作

审查完成后，询问用户是否需要：

1. **在 GitHub 上提交审查评论**（需用户确认）：
```bash
# 提交整体审查意见
gh pr review 42 --comment --body "审查意见内容"
# 或请求修改
gh pr review 42 --request-changes --body "审查意见内容"
# 或批准
gh pr review 42 --approve --body "LGTM"
```

2. **对具体行添加评论**（需用户确认具体内容）：
```bash
gh api repos/{owner}/{repo}/pulls/42/comments \
  --method POST \
  --field body="具体问题描述" \
  --field commit_id="<sha>" \
  --field path="文件路径" \
  --field line=<行号>
```

> ⚠️ 提交 GitHub 评论前必须告知用户并获得明确确认，不自动发布。

---

## 严重程度定义

| 级别 | 标记 | 说明 |
|---|---|---|
| 必须修改 | 🔴 Blocking | 寄存器字序错误、lock 缺失、未捕获崩溃异常、数据写错地址 |
| 建议改进 | 🟡 Non-blocking | MVVM 规范偏差、缺少日志记录、命名不规范、可优化的写法 |
| 小问题 | 🔵 Minor | 注释缺失、格式问题、魔法数字未提取为常量 |
| 肯定 | 🟢 Positive | 做得好的实现，值得记录 |

## 报告模板

```markdown
# 代码审查报告

**PR / 分支：** #<编号> <标题>
**作者：** <author>
**基础分支：** <base> ← <head>
**改动规模：** +<additions> / -<deletions>，<changedFiles> 个文件
**审查时间：** <datetime>

---

## 一、代码逻辑与正确性

### 🔴 必须修改
### 🟡 建议改进
### 🟢 值得肯定

## 二、线程安全

...（同上结构）

## 三、WPF / MVVM 规范

...

## 四、异常处理完整性

...

---

## 总体评价

<综合评价段落>

**审查结论：** ✅ 通过 / ⚠️ 需修改后通过 / ❌ 不通过
```
