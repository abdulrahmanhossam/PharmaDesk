# Contributing to PharmaDesk

Thank you for contributing to **PharmaDesk**! This document outlines our collaborative standards, code quality expectations, and contribution process.

---

## 📜 Core Principles & Hard Constraints

1. **Offline-First Non-Negotiable**: PharmaDesk must run 100% locally with zero external network connectivity. Never introduce external CDN links (`https://cdn...`) for scripts, fonts, or stylesheets. All assets must reside in `wwwroot/lib/`.
2. **ACID Financial Invariants**: Always maintain transactional integrity. Any operation touching inventory balances, sales lines, or purchase batches must execute within an explicit database transaction (`IDbContextTransaction`).
3. **Strict Role Separation**: Every administrative endpoint must be protected on the server with `[Authorize(Roles = "Admin")]`. Never rely on client-side menu hiding alone.
4. **Never Commit Secrets**: Never commit passwords, API keys, or plaintext connection strings.
5. **No Direct Commits to `main`**: All code enters `main` exclusively through pull requests.

---

## 🌿 Branching Strategy

Branch names must strictly adhere to:
```
<type>/<short-kebab-description>
```
- `feat/daily-sales-report` (new user-facing capability)
- `fix/lockout-utc-timestamp` (bug fix)
- `docs/add-api-specs` (documentation updates)
- `refactor/extract-stock-service` (refactoring without behavioral change)
- `test/fefo-allocation-tests` (tests)
- `chore/cleanup-dead-models` (tooling or dependency tasks)

---

## ✍️ Conventional Commits

All commit messages must follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:
```text
<type>(<scope>): <subject in imperative present tense>

[optional body explaining context, why, and what]

[optional footer: Closes #12]
```

### Examples:
- `feat(reports): implement daily sales reconciliation report`
- `fix(auth): resolve hardcoded cashier id in sale checkout`
- `docs(data-model): add corrected Mermaid ERD and data dictionary`
- `refactor(inventory): extract batch deduction logic into StockService`

---

## 🛠 Pull Request Checklist

Before submitting a Pull Request, verify the following:

- [ ] **Clean Build**: `dotnet build` succeeds with 0 errors and 0 new warnings.
- [ ] **Business Rules Uphold**: FEFO deduction, non-negative inventory, and unexpired batch rules are strictly enforced.
- [ ] **Authorization Gates**: Any administrative controller or action has `[Authorize(Roles = "Admin")]`.
- [ ] **Audit Trail**: State-mutating events are logged to `ActivityLog`.
- [ ] **No Secrets**: `appsettings.json` contains no production credentials.
- [ ] **Templates Used**: PR description fills out [.github/pull_request_template.md](.github/pull_request_template.md).
- [ ] **Issue Linked**: PR links to a tracking issue (`Closes #123`).

---

## 👥 Splitting Work Between Developers

Work is organized strictly across separate files, controllers, services, and views following the build order in [docs/08-development-workflow.md](docs/08-development-workflow.md). Please coordinate on GitHub Issues before commencing work on unassigned tasks.
