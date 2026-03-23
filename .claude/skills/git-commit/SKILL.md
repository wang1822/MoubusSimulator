---
name: git-commit
description: |
  智能 Git 提交技能，适用于 C# WPF 项目（GS215 EMS Modbus模拟器）。
  当用户说"提交代码"、"git commit"、"帮我提交"、"提交一下"、"commit"、"暂存并提交"、
  "写提交信息"、"push之前"等时，必须使用此技能。
  功能：自动分析 git diff、智能筛选暂存文件、生成中文 Conventional Commits 格式提交信息、
  提交前执行 dotnet build 检查，确保不提交破坏性改动。
---

# 智能 Git 提交技能

## 执行流程（每次提交严格按此顺序）

### 第一步：获取当前改动全貌

同时并行执行以下命令：

```bash
git status                          # 查看所有已跟踪/未跟踪文件状态
git diff                            # 已跟踪但未暂存的改动
git diff --cached                   # 已暂存的改动
git log --oneline -5                # 最近5条提交，用于对齐提交风格
```

### 第二步：智能筛选要暂存的文件

**应该暂存的文件：**
- `.cs` / `.xaml` / `.csproj` / `.sln` 等源代码文件
- `*.md` 说明文档（如本次同步修改了设计文档）
- `nlog.config`、`appsettings.json` 等配置文件
- `*.json` 快照/配置文件（在 `Resources/` 下）

**不应该暂存的文件：**
- `bin/`、`obj/` 编译输出目录
- `*.user` Visual Studio 个人配置
- `logs/` 运行时日志文件
- `.vs/` IDE 缓存目录
- 任何包含密钥、密码的文件

如果用户未明确指定文件，根据上述规则智能判断，然后告知用户将暂存哪些文件，等待确认或直接执行（视用户授权程度）。

### 第三步：执行 dotnet build 检查

在暂存文件后、提交前，必须先执行构建检查：

```bash
# 在项目根目录查找 .sln 或 .csproj
dotnet build --no-restore 2>&1 | tail -20
```

- ✅ **构建成功**（`Build succeeded`）→ 继续提交
- ❌ **构建失败** → 停止提交，输出错误信息，提示用户先修复

> 如果项目还未初始化（无 .sln/.csproj），跳过此步骤并说明原因。

### 第四步：生成中文 Conventional Commits 提交信息

**格式：**
```
<类型>(<范围>): <中文简述>

<可选：详细说明，说明改动的原因和影响>
```

**类型对照表：**

| 类型 | 适用场景 | 示例 |
|---|---|---|
| `feat` | 新增功能/字段/设备面板 | `feat(PCS): 添加故障字1多选注入功能` |
| `fix` | 修复bug | `fix(RegisterBank): 修复float32字序写入错误` |
| `refactor` | 重构，不改变行为 | `refactor(ViewModel): 提取FlushToRegisters基类方法` |
| `docs` | 仅文档改动 | `docs: 更新寄存器地址映射说明` |
| `style` | 格式/样式，不影响逻辑 | `style(XAML): 统一FieldRow控件间距` |
| `test` | 测试代码 | `test: 添加RegisterBank并发读写单元测试` |
| `chore` | 构建/依赖/配置 | `chore: 升级NModbus4至2.1.0` |
| `perf` | 性能优化 | `perf(RegisterBank): 减少lock竞争范围` |

**范围（可选）常用值：**
`PCS` / `BMS` / `MPPT` / `STS` / `AC` / `Genset` / `DiDo` / `Gas` / `Meter`
/ `RegisterBank` / `ModbusService` / `ViewModel` / `MainWindow` / `Config` / `Log`

**生成规则：**
- 从 `git diff` 内容中提取实际改动，不凭空捏造
- 一次提交只做一件事，如果改动跨越多个不相关模块，建议拆分为多次提交
- 简述不超过 50 个字符
- 如改动涉及寄存器地址、比例系数、字序等关键参数，在详细说明中注明

**示例：**
```
feat(BMS): 添加告警信息1和告警信息2多选注入

新增 BmsAlarmBits.cs 中 Alarm1/Alarm2 的 [Flags] 枚举定义。
BmsViewModel 中增加对应 ObservableCollection<AlarmItem>，
属性变更时自动 OR 合并写入 RegisterBank 地址 23680+offset。
```

### 第五步：执行提交

```bash
# 暂存文件
git add <筛选后的文件列表>

# 提交（使用 HEREDOC 避免特殊字符问题）
git commit -m "$(cat <<'EOF'
<类型>(<范围>): <中文简述>

<详细说明（如有）>

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"

# 提交后验证
git status
git log --oneline -3
```

## 注意事项

- **禁止** `git add .` 或 `git add -A`，防止误提交 bin/obj/logs
- **禁止** `--no-verify` 跳过 hooks
- 如果用户说"不用build检查，直接提交"，可跳过第三步，但需告知风险
- 如果工作区有未解决的合并冲突（`<<<<<<`），停止并提醒用户先解决
- **不主动 push**，除非用户明确说"提交并推送"

## 常见场景处理

**场景1：只改了一个设备的 ViewModel**
→ 类型 `feat` 或 `fix`，范围填设备名，简述说明添加/修复了什么

**场景2：同时改了多个文件但都属于同一功能**
→ 合并为一次提交，范围可省略或用最核心的模块名

**场景3：新增了设备面板（.cs + .xaml + Model）**
→ `feat(<设备名>): 新增<设备名>完整设备面板`，详细说明列出新增文件清单

**场景4：改了设计文档 .md**
→ 单独 `docs:` 提交，或与代码一起提交并在详细说明中注明文档也已同步更新
