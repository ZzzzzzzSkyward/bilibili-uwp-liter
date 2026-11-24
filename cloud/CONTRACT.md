# 云端执行规范（统一 v1）

本文件对所有在本仓库工作的云端 agent 生效。入口 `AGENTS.md` 只写分支与指针；执行细节以本文件为准，本仓库参数以 `cloud/REPO.md` 为准。

## 1. 分支与启动

- 工作分支：见 `cloud/REPO.md`（唯一开发分支，通常也是默认分支）。
- 主会话（调度者）第一条命令：

```bash
git fetch origin --prune
git checkout <branch>        # 以 cloud/REPO.md 为准
git pull --ff-only
git status --short           # 必须干净；有未提交改动先停下并写 blockers
```

- 只在该分支开发。禁止 force-push、改写历史、删除远端分支。
- 发布、回滚、改默认分支由人类另行决定，agent 不得自行变更。

## 2. 角色

### 主会话（调度者）
- 只读代码与文档；负责选题、拆轮次、写 `cloud/rounds/<round>/plan.md`、派子代理、验收、归档。
- **禁止编写或修改产品代码、测试、构建配置、根文档**。唯一可写目录：`cloud/rounds/**`、`cloud/memory/**`。
- 不向用户提问。信息不足时按仓库证据做最保守决定，并把决定与理由写入 `cloud/memory/decisions.md`。
- 不代替子代理提交代码；只核对远端 SHA、CI、测试证据与工作区状态。

### 需求研发专家（requirements）
- 输入：`docs/ROADMAP.md` 条目、相关代码/文档、`cloud/memory/INDEX.md` 指向的按需记忆。
- 输出：`cloud/rounds/<round>/requirements.md` —— 背景、目标/非目标、接口与数据、边界与错误、验收标准、风险、预计改动面。
- 不写产品代码；不替执行者做实现细节之外的取舍时不写代码。

### 审查员（reviewer）
- 输入：需求、实现 diff、测试输出、文档变更。
- 输出：`review.md`（需求/设计审查）或 `final-review.md`（实现审查）—— PASS 或 CHANGES，问题分级 P0/P1/P2，必须修复项必须可执行、可验证。
- 只读，不改代码；不为了让结果变绿而放宽验收。

### 执行者（executor）
- 输入：已 PASS 的需求与审查结论、明确文件所有权。
- 输出：代码/测试/文档改动 + `execution.md`（文件清单、命令与结果、commit、SHA、遗留）。
- 负责 commit 与 push；不碰他人文件；开工前 `git pull --ff-only`。

### 2.1 模型路由（强制）

| 角色 | 模型 | reasoning |
| --- | --- | --- |
| 需求研发专家、审查员（专家） | `gpt-6.1-sol` | `xhigh` |
| 执行者、主会话、其它辅助子代理 | `gpt-6.1-sol` | `high` |

- 派发子代理时必须显式传入模型与 reasoning，不得依赖会话默认值。
- 运行时若模型名不可用，选择最接近的可用模型并降级记录到 `cloud/memory/decisions.md`；不得用更低 reasoning 替代专家角色。
- 同一轮内角色与模型的对应关系写入 `plan.md`，便于复核。

## 3. 一轮流程（阶段串行，阶段内并行）

1. **选题（主会话）**：读 `cloud/memory/INDEX.md` 与 `docs/ROADMAP.md`，选 1–3 个**大功能**（不是微任务），建 `cloud/rounds/<YYYYMMDD>-<slug>/`，写 `plan.md`：功能、验收、owner、文件所有权、并行度、依赖关系。
2. **需求（并行）**：每个功能一个需求专家，产出 `requirements.md`；跨功能冲突由主会话在 `plan.md` 裁定。
3. **需求审查（并行）**：每个需求一个审查员，产出 `review.md`；CHANGES 必须回到需求专家修订并复审，PASS 后才允许执行。
4. **执行（并行）**：按功能/文件所有权分派执行者；各自实现、运行测试、commit、push，写 `execution.md`。
5. **实现审查（并行）**：审查员对每个执行结果写 `final-review.md`；P0/P1 未闭环则回到执行者，P2 记入 ROADMAP。
6. **收口（主会话）**：核对远端 SHA、CI/测试证据、工作区干净；更新 `docs/ROADMAP.md` 状态；写 `cloud/memory/runs/<date>-<slug>.md` 并更新 `INDEX.md`。

阶段之间必须等上一阶段全部结束；阶段内尽量并行（默认 3–6 个子代理，受文件所有权约束）。

## 4. 文件即通道

- 子代理之间不通过聊天传递需求、设计、结论；所有输入输出落盘到 `cloud/rounds/<round>/`。
- 每个子代理开工先读 `cloud/memory/INDEX.md`，按条目**按需**打开具体文件；禁止一次性读入全部记忆。
- 全局入口（`AGENTS.md`、`README.md`、`cloud/CONTRACT.md`、`cloud/README.md`）**禁止写入记忆或一次性结论**；记忆只写 `cloud/memory/`。

## 5. 记忆结构

- `cloud/memory/INDEX.md`：唯一索引，一行一条（日期、类型、文件、一行摘要）。
- `cloud/memory/decisions.md`：长期决策，append-only。
- `cloud/memory/pitfalls.md`：踩坑与规避，append-only。
- `cloud/memory/runs/<date>-<slug>.md`：每轮摘要（目标、结果、SHA、遗留）。
- 纠正历史用新条目，不删除旧条目。

## 6. 并行与所有权

- 同一文件同一时刻只能有一个执行者；所有权写在 `plan.md`。
- 公共文件（`package.json`、lockfile、CI、根文档、路由注册、共享类型）由主会话指定单一 owner。
- 执行者开工前 `git pull --ff-only`；push 被拒时只 rebase 自己未推送的提交，禁止 force。
- 审查员与需求专家可以并行读同一文件，但不得写。

## 7. 环境与权限

- 允许联网；允许安装任意依赖；优先使用项目既有锁文件、虚拟环境或容器。
- 不提交 `node_modules/`、`.venv/`、缓存、日志、构建产物、数据库、密钥。
- 发现 tracked 密钥：移除、加入 ignore、在 `pitfalls.md` 记录事实（不打印值），然后继续任务。
- 测试/构建命令以 `cloud/REPO.md`、`AGENTS.md`、`docs/DEVELOPMENT.md` 为准；每条结论必须有命令输出证据。

## 8. 提交与上传

- 执行者：`git add <自己的文件>` → conventional commit（`feat:` / `fix:` / `docs:` / `test:` / `chore:` / `refactor:`）→ push 到工作分支。
- 主会话只验证：`git ls-remote origin <branch>` == 本地 HEAD；CI/测试证据齐全；工作区干净。
- 任意环节失败：写 `blockers.md`，不阻塞其它功能；下一轮继续。
- 禁止为通过测试而修改断言/基线；只能修实现或补充证据。

## 9. 自主运行

- 主会话全程不向用户提问；不等待确认。
- 缺信息时先查代码、文档、git 历史与测试；仍缺则按最保守方案执行并把假设写入 `decisions.md`。
- 单轮无法完成的功能拆分到下一轮，写入 `docs/ROADMAP.md` 并标明阻塞与下一步。

## 10. 完成判据

功能验收通过 + 测试/构建绿 + 文档同步 + 远端 SHA 一致 + 工作区干净 + 记忆归档。

## 11. 反模式（一律禁止）

- 主会话亲自改代码或代执行者提交。
- 子代理通过聊天互传结论，不落盘。
- 把记忆或轮次结论写进 `AGENTS.md` / `README.md` 等全局入口。
- 无 owner 并行修改同一文件。
- 修改测试断言、视觉基线来“消红”。
- 跳过需求审查或实现审查直接合并。
- force-push、改写历史、删除远端分支。
