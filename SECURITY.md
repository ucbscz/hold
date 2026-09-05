# Security Policy

UCB Hold handles user identity, administrative actions and equipment loan records. Security issues should be reported privately and handled with enough context to reproduce and remediate them safely.

## Supported Versions

| Target | Support |
| --- | --- |
| Latest release | Supported |
| `main` branch | Supported |
| Older branches | Best effort |

## Reporting a Vulnerability

Use GitHub Security Advisories when available:

<https://github.com/ucbscz/hold/security/advisories>

If advisories are not available, contact a maintainer listed in the README through a private channel.

Include:

| Field | Details |
| --- | --- |
| Summary | Short description of the issue and affected area. |
| Reproduction | Steps, inputs, account type and environment needed to reproduce. |
| Impact | What data, permissions or workflow could be affected. |
| Evidence | Logs, screenshots or proof-of-concept details, without exposing secrets. |
| Suggested fix | Optional remediation notes if known. |

## Disclosure Rules

| Do | Do not |
| --- | --- |
| Report privately first. | Publish exploit details before maintainers can respond. |
| Share only the access needed to reproduce. | Access or modify unrelated data. |
| Request permission before automated scans. | Run aggressive scans against shared infrastructure. |
| Remove secrets from screenshots and logs. | Include tokens, passwords or private keys in reports. |

## Response Targets

| Step | Target |
| --- | --- |
| Acknowledge report | Within 7 business days |
| Initial triage | After reproducibility and impact are understood |
| Remediation | Prioritized by severity and operational risk |
| Public advisory | After a fix or mitigation is available |

## Secret Management

Never commit credentials, production connection strings, JWT secrets, private keys, database dumps or backups. Use environment variables, `code/server.env` for local Docker execution, or `dotnet user-secrets` for local backend development.
