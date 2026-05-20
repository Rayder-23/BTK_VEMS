---
name: github-commit
description: >-
  Safe Git commits with brief prefixed messages (Add, Update, Fix, Feature,
  Remove), secret scanning, and .gitignore hygiene. Use whenever the user asks
  to commit, push changes, save work to git, create a commit, stage files, or
  says "commit and push to github" (the primary activation phrase — always use
  this skill for that). Also triggers on casual phrasing ("commit this", "git it
  up", "push to github"). Use before any git operation that might touch merges
  or conflicts. Do NOT use for pull requests, branch strategy, or code review
  without a commit request.
---

# GitHub Commit

Commit only when the user explicitly asks. Never commit proactively.

## Activation

**Primary phrase:** `commit and push to github`

When the user says this (or a close variant — e.g. "commit and push to GitHub", "commit & push to github"), treat it as full activation:

1. Run the complete workflow in this skill (secret scan → stage → commit with prefixed subject + body).
2. **Then push** to the remote after a successful commit (`git push`, or `git push -u origin <branch>` when upstream is not set).

If the user asks to **commit only** (no "push" in the request), commit locally and do not push unless they ask separately.

## Commit message format

**Subject line:** One line, clean and brief. Start with exactly one of these prefixes (including the colon):

| Prefix | Use when |
|--------|----------|
| `Add:` | New files, features, endpoints, or capabilities |
| `Update:` | Changes to existing behavior, refactors, config tweaks |
| `Fix:` | Bug fixes, error corrections, regressions |
| `Feature:` | User-facing capability that spans multiple areas |
| `Feature(Name):` | Same as Feature, but scoped (e.g. `Feature(Fees):`, `Feature(Auth):`) |
| `Remove:` | Deletions, deprecations, dead code removal |

**Pattern:** `Prefix: short imperative summary in plain language`

**Body (default):** After a blank line, add a short body that explains *why* the change was made — bullet points or paragraphs, same style as a normal descriptive commit. Focus on intent and impact, not a file list.

**Example with body:**
```
Fix: PaidAmount not updating after partial FeePayments insert

FeePayments rows were inserted without syncing Challans.PaidAmount.
Update PaidAmount on each insert so Balance (computed) stays correct.
```

**Subject-only examples:**
- `Add: Student fee allocation check during challan generation`
- `Update: Scalar OpenAPI route mapping in Program.cs`
- `Feature(Billing): Late fee applied when past DueDate and LateFeeDays`
- `Remove: Unused Swagger UI package reference`

Pick the prefix that best matches the **primary intent** of the diff. Do not combine prefixes on the subject line.

## Pre-commit workflow

Run these in parallel before staging:

```bash
git status
git diff
git diff --cached
git log -3 --oneline
```

Use recent commit style as a tiebreaker when ambiguous.

### 1. Secret and credential scan (mandatory)

Before `git add`, inspect **staged and unstaged** changes for sensitive data. Never commit:

| Blocked | Notes |
|---------|--------|
| `.env`, `.env.local`, `.env.*` | **Exception:** `*.example`, `*.example.env`, files clearly named as templates with **no real secrets** |
| Connection strings | `ConnectionStrings` in `appsettings*.json`, `web.config`, code, or comments with real hosts/passwords |
| API keys, tokens, passwords | Including Azure, AWS, JWT secrets, SMTP creds, `User Secrets` IDs with values |
| `appsettings.Development.json` | Local overrides — use `appsettings.Development.example.json` for templates |
| Certificates, `.pfx`, `.pem` with private keys | |
| `credentials.json`, `secrets.json`, `*.secrets.*` | |

Scan commands (adapt to shell):

```bash
git diff --name-only
git diff
```

Flag patterns: `Password=`, `pwd=`, `User ID=`, `Server=`, `AccountKey=`, `api_key`, `apikey`, `secret`, `Bearer `, `sk-`, `ghp_`, `AKIA`, base64 blobs in config.

**If secrets are found:**
1. **Stop** — do not commit those files.
2. Tell the user what was found and where.
3. Move secrets to environment variables, User Secrets, or local-only config.
4. Add or update `.gitignore` (see below).
5. If a file was already committed historically, warn the user; do not rewrite history unless they explicitly ask.

For extended patterns, read [references/secrets-patterns.md](references/secrets-patterns.md).

### 2. .gitignore hygiene

If a sensitive or generated path is not ignored, append to `.gitignore` **before** committing other work:

```
# Secrets and local config
.env
.env.*
!.env.example
appsettings.Development.json
**/secrets.json
**/credentials.json
```

Use project-appropriate entries (e.g. `bin/`, `obj/`, `.vs/`, `pubtest/` for .NET). Prefer existing .gitignore sections and conventions.

### 3. Stage only intended files

- Do not stage `bin/`, `obj/`, `.vs/`, `node_modules/`, or other build artifacts unless the user explicitly wants them.
- Do not stage files that failed the secret scan.
- Warn if the user asked to commit a file that looks like a secret.

## Commit execution

**Git safety (non-negotiable):**
- Never change `git config`
- Never use `--no-verify`, `--no-gpg-sign`, or skip hooks unless the user explicitly requests it
- Never `push --force` to `main`/`master` without explicit request; warn if they do
- Avoid `git commit --amend` unless: user asked, HEAD was created in this session, and commit was not pushed

**Sequence:**
1. Complete secret scan and .gitignore updates
2. `git add` only approved paths
3. Commit with the formatted message

**PowerShell (preferred on Windows)** — use a here-string when the message has a body:
```powershell
git commit -m @"
Fix: PaidAmount not updating after partial FeePayments insert

Sync Challans.PaidAmount on each FeePayments insert so the computed Balance stays correct.
"@
```

Single-line commits are fine when the change is trivial and needs no explanation.

4. `git status` after commit to confirm success

**After commit — push (when activated):** If the user used the activation phrase or explicitly asked to push:

```powershell
git push
# or, if no upstream yet:
git push -u origin HEAD
```

Report push result (branch, remote, commits pushed). Never force-push to `main`/`master` without explicit approval.

## Merges and conflicts — always ask first

**Never** run these without explicit user approval in the current message:
- `git merge`, `git pull` (when it merges), `git rebase`, `git cherry-pick`
- Resolving conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`)
- `git checkout --theirs` / `--ours` on conflicted files
- `git reset --hard`, `git push --force`

If a commit attempt surfaces merge conflicts or a dirty merge state:
1. Stop
2. Explain what conflicted (files/branches)
3. Present options (pull first, abort, manual resolution)
4. Wait for the user to choose

## When commit fails

- Hook rejected commit → fix the issue, create a **new** commit (do not amend unless amend rules apply)
- Nothing to commit → say so; do not create an empty commit

## Checklist (mental)

```
- [ ] User explicitly asked to commit
- [ ] No secrets, .env (except safe examples), or connection strings in the commit
- [ ] .gitignore updated if needed
- [ ] Subject uses one allowed prefix; body explains why when the change is non-trivial
- [ ] No merge/conflict operations without user approval
- [ ] git status clean after commit (or explain remaining state)
- [ ] Pushed to GitHub when activation phrase or explicit push was requested
```
