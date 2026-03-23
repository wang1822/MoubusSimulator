# Git 推送规范

> 本项目 `main` 分支受保护，**任何人（包括管理员）不得直接推送到 main**，所有改动必须通过分支 + PR 流程合并。

---

## 基本流程

```
1. 从 main 创建功能分支
2. 在分支上开发、提交
3. 推送分支到远端
4. 在 GitHub 上提 PR
5. 等待审批通过后合并到 main
6. 删除已合并的分支
```

---

## 分支命名规范

| 类型 | 格式 | 示例 |
|---|---|---|
| 新功能 | `feat/功能描述` | `feat/pcs-viewmodel` |
| 修复Bug | `fix/问题描述` | `fix/float32-byteorder` |
| 重构 | `refactor/描述` | `refactor/registerbank-lock` |
| 文档 | `docs/描述` | `docs/register-mapping` |
| 测试 | `test/描述` | `test/registerbank-concurrent` |
| 配置/依赖 | `chore/描述` | `chore/upgrade-nmodbus4` |

---

## 每次推送步骤

```bash
# 第一步：确保本地 main 是最新的
git checkout main
git pull origin main

# 第二步：创建并切换到功能分支
git checkout -b feat/你的功能名

# 第三步：开发代码，完成后提交
git add 文件名              # 精确添加，不要 git add .
git commit -m "feat(模块): 中文描述"

# 第四步：推送分支到远端
git push origin feat/你的功能名

# 第五步：去 GitHub 提 PR
# https://github.com/wang1822/MoubusSimulator/pulls
# 点击 "New pull request"，选择你的分支合并到 main
```

---

## 提交信息格式（Conventional Commits）

```
<类型>(<范围>): <中文简述>

<可选详细说明>
```

**示例：**
```
feat(PCS): 添加故障字1多选注入功能
fix(RegisterBank): 修复float32 AB CD字序写入错误
docs: 更新寄存器地址映射表
```

**类型说明：**

| 类型 | 说明 |
|---|---|
| `feat` | 新功能 |
| `fix` | 修复Bug |
| `refactor` | 重构（不改变行为） |
| `docs` | 仅文档改动 |
| `test` | 测试代码 |
| `chore` | 构建/依赖/配置 |
| `style` | 格式调整，不影响逻辑 |

---

## 禁止行为

```bash
git push origin main          # ❌ 禁止直接推送 main
git push origin main --force  # ❌ 禁止强制推送
git add .                     # ❌ 禁止全量暂存（防止提交 bin/obj/logs）
git commit --no-verify        # ❌ 禁止跳过检查
```

---

## PR 合并要求

- 至少 **1 位审批人** 通过
- 本地 `dotnet build` 必须成功（无编译错误）
- 分支与 main 保持同步（无冲突）
- 合并后删除该功能分支

---

## 常见问题

**Q：我的分支和 main 有冲突怎么办？**
```bash
git checkout feat/你的分支
git pull origin main          # 拉取最新 main
# 手动解决冲突文件
git add 冲突文件
git commit -m "fix: 解决与main的合并冲突"
git push origin feat/你的分支
```

**Q：PR 提交后发现还需要改动怎么办？**
```bash
# 直接在同一分支继续提交推送，PR 会自动更新
git commit -m "fix: 根据审查意见修改xxx"
git push origin feat/你的分支
```

**Q：分支合并后怎么清理？**
```bash
# 删除本地分支
git branch -d feat/你的分支
# 删除远端分支
git push origin --delete feat/你的分支
```
