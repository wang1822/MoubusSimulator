# GS215 EMS 设备故障模拟器

Modbus Slave 模拟器，供 GS215 EMS 通过 Modbus TCP/RTU 轮询，无需真实硬件即可联调测试。

- **设计文档**：`设备故障模拟器_设计文档.md`（字段定义、寄存器地址、比例系数，编码前必读）
- **Git 规范**：`GIT_WORKFLOW.md`（分支命名、PR 流程、禁止行为）
- **项目源码**：`SimulatorApp/`

## 技术栈

| 层 | 技术 |
|---|---|
| UI | WPF .NET 8 |
| MVVM | CommunityToolkit.Mvvm 8.x（Source Generator） |
| Modbus | NModbus4 2.1.0 |
| 序列化 | System.Text.Json |
| 日志 | NLog + NLog.Extensions.Logging |
| 测试 | xUnit（SimulatorApp.Tests/） |

## 可用技能（Skills）

| 技能 | 触发场景 |
|---|---|
| `/modbus-simulator` | 编写设备代码、ViewModel、寄存器映射、故障注入 |
| `/git-commit` | 提交代码（含 dotnet build 检查，生成中文 Conventional Commits） |
| `/test-runner` | 运行单元测试、分析失败原因 |
| `/review-pr` | PR 代码审查（四维度：逻辑/线程安全/MVVM规范/异常处理） |

## 核心编码约定

- **float32 字序**：统一 AB CD（Big-Endian 字），通过 `FloatRegisterHelper.ToRegisters()` 读写
- **RegisterBank**：所有寄存器读写必须在 `lock(_lock)` 内，线程安全
- **ViewModel 字段**：`[ObservableProperty]` 声明，属性变更通过 `partial void OnXxxChanged` 触发 `FlushToRegisters()`
- **bitmask 告警字**：`AlarmItem.IsChecked` OR 合并，不能用 `+=`
- **输入校验**：超量程自动截断到边界值，日志追加 WARN，不抛异常

## 设备寄存器地址速查

| 设备 | 起始地址 |
|---|---|
| PCS 储能变流器 | 7296 |
| BMS 电池管理系统 | 23680 |
| MPPT 光伏 | 40064 |
| STS（仪表） | 1408 |
| STS（控制IO卡） | 1920 |
| 空调 | 52352 |
| 除湿机 | 53248 |
| 柴发 | 53504 |
| 气体检测 | 53760 |
| 外部电表 | 384 |
| 储能电表 | 48256 |
| DI/DO 动环控制器 | 60544 |

## Git 工作流

```
main 分支受保护，禁止直接推送。所有改动必须通过分支 + PR 合并。

分支命名：feat/xxx | fix/xxx | refactor/xxx | docs/xxx | test/xxx
提交格式：feat(PCS): 中文描述（Conventional Commits）
PR 合并要求：至少 1 位审批人 + dotnet build 成功
```

## 文件输出目录

| 类型 | 路径 |
|---|---|
| PR 审查报告 | `.reviews/pr-<编号>-<日期>.md` |
| 测试报告 | `.test-reports/test-<日期>-<时间>.md` |
| 配置快照 | `Resources/*.snapshot.json`（已 gitignore） |
| 运行日志 | `logs/app-yyyy-MM-dd.log`（已 gitignore） |
