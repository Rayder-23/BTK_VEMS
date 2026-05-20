# Secret scan patterns

Use when reviewing diffs before commit. A single match is enough to block the file.

## File names (block unless clearly a template)

- `.env`, `.env.local`, `.env.production`, `.env.development`
- `appsettings.Development.json` (local overrides)
- `secrets.json`, `credentials.json`, `serviceAccountKey.json`
- `*.pfx`, `id_rsa`, `id_ed25519`, `*.pem` (private keys)
- `web.config` with `<connectionStrings>` containing passwords

## Safe template exceptions

Allow only when contents are placeholders, not real credentials:

- `*.example.json`, `appsettings.Development.example.json`
- `.env.example` with values like `your-api-key-here`, `changeme`, `TODO`

## Content patterns (case-insensitive)

```
Password=
Pwd=
User ID=
Server=.*;
AccountKey=
SharedAccessKey=
DefaultEndpointsProtocol=https;AccountName=
mongodb(\+srv)?://[^:]+:[^@]+@
postgres(ql)?://[^:]+:[^@]+@
mysql://[^:]+:[^@]+@
```

## Token prefixes

```
ghp_          # GitHub PAT
github_pat_
gho_
ghu_
ghs_
ghr_
sk-           # OpenAI
AKIA          # AWS access key
ASIA          # AWS temp key
xox[baprs]-   # Slack
```

## ASP.NET / VEMS specifics

- `ConnectionStrings` in committed `appsettings.json` — use placeholders or environment overrides
- `UserSecretsId` in `.csproj` is fine; never commit the secrets store contents
- `launchSettings.json` with real API keys in `environmentVariables`

## If unsure

Ask the user: "This file may contain credentials ([reason]). Commit anyway or exclude?"
