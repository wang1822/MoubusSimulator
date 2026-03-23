---
name: test-runner
description: |
  智能测试运行技能，适用于 C# WPF Modbus 模拟器项目（xUnit 测试框架）。
  当用户说"跑测试"、"run test"、"执行测试"、"测试一下"、"单元测试"、"测试有没有过"、
  "帮我测试 RegisterBank"、"测试映射是否正确"、"check 一下测试"时，必须使用此技能。
  功能：自动发现测试项目、执行 dotnet test、解析结果、失败时分析根因并给出修复建议，
  对话展示摘要并生成完整报告保存到 .test-reports/ 目录。
---

# 智能测试运行技能

## 测试项目约定

- **测试项目路径**：`E:/MoubusSimulator/SimulatorApp.Tests/`
- **测试框架**：xUnit + xunit.runner.visualstudio
- **被测项目**：`SimulatorApp/SimulatorApp.csproj`
- **四大核心测试模块**：

| 测试类 | 测试文件 | 覆盖内容 |
|---|---|---|
| `RegisterBankTests` | `Tests/RegisterBankTests.cs` | 并发读写、float32 字序、地址边界 |
| `RegisterMapServiceTests` | `Tests/RegisterMapServiceTests.cs` | 各设备字段比例系数、偏移量映射 |
| `ViewModelValidationTests` | `Tests/ViewModelValidationTests.cs` | 超量程截断、非法输入拒绝、bitmask合并 |
| `ModbusSlaveServiceTests` | `Tests/ModbusSlaveServiceTests.cs` | TCP/RTU 启停、端口占用、异常场景 |

---

## 执行流程

### 第一步：检查测试项目是否存在

```bash
ls E:/MoubusSimulator/SimulatorApp.Tests/ 2>/dev/null || echo "NOT_FOUND"
```

**若测试项目不存在** → 跳转到「初始化测试项目」章节，引导用户创建。
**若存在** → 继续第二步。

---

### 第二步：确定运行范围

根据用户的描述判断运行哪些测试：

| 用户说的 | 运行范围 |
|---|---|
| "跑所有测试" / 无特定说明 | 全部测试 |
| "测试 RegisterBank" | `--filter "FullyQualifiedName~RegisterBank"` |
| "测试映射" / "测试寄存器映射" | `--filter "FullyQualifiedName~RegisterMapService"` |
| "测试 ViewModel" / "测试输入校验" | `--filter "FullyQualifiedName~ViewModelValidation"` |
| "测试 Modbus 服务" / "测试TCP" | `--filter "FullyQualifiedName~ModbusSlaveService"` |
| "测试 PCS" | `--filter "FullyQualifiedName~Pcs"` |
| "测试 BMS" | `--filter "FullyQualifiedName~Bms"` |

---

### 第三步：执行测试

```bash
cd E:/MoubusSimulator

# 先构建（确保代码是最新的）
dotnet build SimulatorApp.Tests/SimulatorApp.Tests.csproj --no-restore -v quiet 2>&1

# 运行测试（输出详细结果，含失败的堆栈信息）
dotnet test SimulatorApp.Tests/SimulatorApp.Tests.csproj \
  --no-build \
  --logger "console;verbosity=detailed" \
  <filter参数（如有）> \
  2>&1
```

解析输出中的关键信息：
- `Passed` / `Failed` / `Skipped` 数量
- 每个失败测试的名称、断言信息、堆栈跟踪

---

### 第四步：失败时自动分析根因

对每个失败的测试，执行以下分析：

1. **读取测试代码**（了解测试意图和断言条件）
2. **读取被测代码**（找出实际行为与期望行为的差距）
3. **结合项目领域知识**给出具体分析：

**常见失败场景与分析方向：**

| 失败场景 | 可能原因 | 检查点 |
|---|---|---|
| float32 读出值与写入值不符 | AB CD 字序实现有误 | `FloatRegisterHelper.ToRegisters()` 的 bytes 拆分顺序 |
| 寄存器地址偏移断言失败 | `ToRegisters()` 中偏移量写错 | 对照设计文档第8节字段顺序重新核对 |
| bitmask 合并值不正确 | AlarmItem.BitMask 定义有误或 OR 逻辑缺失 | `[Flags]` 枚举 bit 位与文档对齐 |
| 并发测试数据竞争 | `RegisterBank` 的 `lock` 范围不够 | 检查是否所有读写都在 lock 内 |
| TCP 启动后立即失败 | 端口已被占用或 TcpListener 未正确 bind | 检查 `ModbusSlaveService.StartTcpAsync` 异常捕获 |
| ViewModel 截断值不符预期 | 边界值判断用了 `>` 而非 `>=` | 校验规则中的边界是否包含端点 |

---

### 第五步：输出结果

#### 5.1 对话内摘要

```
## 测试运行结果

**运行范围：** <全部 / 指定模块>
**时间：** <执行时长>

| 状态 | 数量 |
|---|---|
| ✅ 通过 | <passed> |
| ❌ 失败 | <failed> |
| ⏭ 跳过 | <skipped> |

### 失败的测试（如有）

#### ❌ <测试名称>
**断言信息：** <Expected xxx, Actual xxx>
**根因分析：** <分析结论>
**修复建议：**
- <具体修改建议，含文件名和行号>

### 结论
<全部通过 / X 个测试失败，需要修复>
```

#### 5.2 生成完整报告文件

保存路径：`E:/MoubusSimulator/.test-reports/test-<日期>-<时间>.md`

报告包含：
- 运行时间、范围、环境（.NET 版本）
- 完整的通过/失败/跳过列表
- 每个失败测试的完整堆栈 + 根因分析 + 修复建议
- 总体健康度评估

---

## 初始化测试项目（首次使用）

若 `SimulatorApp.Tests/` 不存在，按以下步骤引导用户创建：

### 创建 xUnit 测试项目

```bash
cd E:/MoubusSimulator
dotnet new xunit -n SimulatorApp.Tests -f net8.0
dotnet add SimulatorApp.Tests/SimulatorApp.Tests.csproj reference SimulatorApp/SimulatorApp.csproj
```

### 推荐添加的 NuGet 包

```bash
cd SimulatorApp.Tests
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package Moq          # 用于 Mock IModbusSlaveService 等接口
dotnet add package FluentAssertions  # 可选，让断言更可读
```

### 推荐目录结构

```
SimulatorApp.Tests/
├── SimulatorApp.Tests.csproj
└── Tests/
    ├── RegisterBankTests.cs
    ├── RegisterMapServiceTests.cs
    ├── ViewModelValidationTests.cs
    └── ModbusSlaveServiceTests.cs
```

### 核心测试用例模板

#### RegisterBankTests.cs（关键用例）

```csharp
public class RegisterBankTests
{
    [Fact]
    public void WriteFloat32_AbCdByteOrder_ReadbackMatches()
    {
        var bank = new RegisterBank();
        bank.WriteFloat32(7296, 750.0f);
        float result = FloatRegisterHelper.FromRegisters(
            bank.Read(7296), bank.Read(7297));
        Assert.Equal(750.0f, result, precision: 3);
    }

    [Fact]
    public void ConcurrentReadWrite_NoCrashNoDataRace()
    {
        var bank = new RegisterBank();
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => {
                bank.Write(i % 65536, (ushort)i);
                _ = bank.Read(i % 65536);
            }));
        Assert.True(Task.WaitAll(tasks.ToArray(), 5000));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65535)]
    public void Write_BoundaryAddress_NoException(int address)
    {
        var bank = new RegisterBank();
        bank.Write(address, 0xFFFF);
        Assert.Equal(0xFFFF, bank.Read(address));
    }
}
```

#### ViewModelValidationTests.cs（关键用例）

```csharp
public class ViewModelValidationTests
{
    [Theory]
    [InlineData(1201, 1200)]  // 超上限截断
    [InlineData(-1,   0)]     // 超下限截断
    public void DcVoltage_OutOfRange_ClampedToBoundary(double input, double expected)
    {
        var vm = new PcsViewModel(...);
        vm.DcVoltage = input;
        Assert.Equal(expected, vm.DcVoltage);
    }

    [Fact]
    public void Fault1Bitmask_MultipleChecked_OrMergedCorrectly()
    {
        var vm = new PcsViewModel(...);
        vm.Fault1Items[0].IsChecked = true;  // bit 0 = 0x0001
        vm.Fault1Items[2].IsChecked = true;  // bit 2 = 0x0004
        Assert.Equal(0x0005, vm.CalculateFault1());
    }
}
```

---

## 覆盖率目标

| 模块 | 目标覆盖率 | 优先级 |
|---|---|---|
| `RegisterBank` | ≥ 90% | 🔴 最高 |
| `FloatRegisterHelper` | 100% | 🔴 最高 |
| `RegisterMapService` | ≥ 80% | 🟡 高 |
| ViewModel 校验逻辑 | ≥ 75% | 🟡 高 |
| `ModbusSlaveService` | ≥ 60% | 🟢 中 |

如需生成覆盖率报告：
```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:".coverage-report" -reporttypes:Html
```
