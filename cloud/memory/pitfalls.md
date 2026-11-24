# 踩坑与规避（append-only）

每条：日期 / 现象 / 根因 / 规避 / 相关命令或文件。

- 2026-11-24：云端代理可能把结论写进 `AGENTS.md`，导致入口膨胀。规避：结论只写 `cloud/memory/`，入口仅保留指针。

- 2026-11-25（Asia/Shanghai；轮次 20261124）：Bash 探针不能用 dash 的 `sh` 调用；LF 只解决换行，不解决 BASH_SOURCE/pipefail。显式 Bash，并核对真实 SDK/ref-pack。证据：maintenance-foundation/verification/evidence/baseline.md。
- 2026-11-25：不要只搜 Services 命名空间就断言兼容类型不存在。ApiHelper.cs 顶部真实定义 global CookieService，ApiHelper.Cookie 也要求该派生包装类型；换成基类曾产生 CS0266。看完整类型/字段/构造器，真实编译优先于审查假设。证据：verification/ownership-conflict.md 与 amendment-review.md。
- 2026-11-25：GitHub Actions 的 YAML/Bash 解析成功不代表表达式上下文有效。job-level env 不支持 runner.temp；真实 dispatch HTTP422 才暴露此错误。查官方 Context Availability；运行时路径在受支持的步骤中经 GITHUB_ENV 设置，不能据 workflow active 或空运行历史宣称 CI 通过/服务故障。证据：verification-runner/hosted-validation-execution.md、workflow-env-review.md。
- 2026-11-25：套件预算和取消也覆盖 git/bash/SDK 元数据子进程。逐次独立的五秒 capture 会在全局到期后继续启动后缀；初始报告应先落地，所有自有进程组共享剩余预算并被清理。新增慢元数据与信号负控，不能只测已启动的测试阶段。证据：verification-runner/final-review.md P1-01 与 review-fix-execution.md。
- 2026-11-25：CER 扩展名不等于 DER。当前跟踪 CER 是 PEM、有效期到 2024，且指纹不等于两个项目选择的 Windows-store signer；不能据此宣称当前签名证书失效。仅读取并记录，实际签名存储/私钥/发行证书匹配由 Windows 发布准备核验；不改受保护证书/身份。证据：package-layout-guard/release-signing-evidence.md。
