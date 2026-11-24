# 轮次目录

命名：`<YYYYMMDD>-<slug>/`。每轮至少包含：

| 文件 | 作者 | 内容 |
| --- | --- | --- |
| `plan.md` | 主会话 | 功能、owner、文件所有权、并行度、验收、依赖 |
| `requirements.md` | 需求专家 | 每个功能一份（多份加后缀） |
| `review.md` | 审查员 | 需求审查结论（PASS/CHANGES） |
| `execution.md` | 执行者 | 改动、命令、测试结果、commit、SHA |
| `final-review.md` | 审查员 | 实现验收与回归结论 |
| `blockers.md` | 任意角色 | 阻塞、证据、下一步（可选） |

临时产物放 `tmp/`（已 gitignore），不提交。
