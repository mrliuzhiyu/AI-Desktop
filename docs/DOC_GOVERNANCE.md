---
status: authoritative
type: spec
verified: 2026-11-21
---
# 文档规范

参考 TEXTVIEW 的文档治理机制：新文档声明 status、type、verified，并在 docs/README.md 登记。
active/authoritative 可作为当前参考；draft 为未实施计划；historical/superseded 仅供溯源。
代码影响文档时同时更新说明；验证日期填写实际核实日期，不跟随整理后的 Git 日期。
运行 `python scripts/check_docs.py` 检查状态头和索引。
