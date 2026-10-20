# RC-137 Review of the staff accounts and password change code (MIL-030)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-137 |
| CrossReference | [MIL-030], [ADR-0015], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - Domain: `StaffMember` (stamp, must-change flag, change, reset, enable), `StaffAuditEvent`, `StaffAuditAction`, `IStaffAuditRepository`, the two new members of `IStaffRepository`;
  - Application: `PasswordPolicy`, `CommonPasswords`, `StaffAuthService` (sign-in audit, change of the own password), `StaffAccountService` (create, disable, enable, reset, list, audit page), `IStaffAdminService`, `IStaffCallContext`, the permissions `Staff.Manage` and `Staff.ChangeOwnPassword` and their role table, the new error codes and DTOs;
  - Infrastructure: the two columns and `staff_audit_event` with the migration `AddStaffPasswordAndAudit`, `StaffAuditRepository`, the stamp and must-change claims of `JwtStaffTokenIssuer`;
  - Api and client: `StaffController`, the account check in `StaffAuthentication`, `RequiresPermission` (forced change), `HttpStaffCallContext`, `StaffWriteRateLimit`, the `reset-staff-password` command and the `MustChangePassword` setting, `ApiStaffAdminService`, the forwarded address;
  - Web: `ChangePassword`, `StaffList`, `StaffAudit` with their view models, `MustChangePasswordMiddleware`, the claims, policies, menu and texts.
- **Checklist used:** [QC-CS-001].
- **Scope:** full review of the new and changed files.
- **Language and domain:** n/a (technical type).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:**
  - `dotnet build NFHotel.sln` (0 warnings, warnings as errors) and `dotnet format --verify-no-changes`;
  - `dotnet ef` 10.0.13 for the migration, and `scripts/check-comment-language.sh`;
  - all test projects run: Domain 349, Application 368, Api 145, ApiClient 82, Web 232, Infrastructure 151 (against PostgreSQL in a container), External 97, all passed; the end-to-end tests were not run here (they need the stack).

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| CS naming | Names and test names | Pass | Every test is `Method_Condition_Expected`; `TestRulesTests` passes |
| CS layers | Dependencies point inward | Pass | Domain has no new dependency; Application holds the rules and ports only (`IStaffCallContext`, `IStaffAuditRepository`); the web application reaches the accounts only through `IStaffAdminService` over HTTP |
| CS async | Asynchronous code | Pass | Every public asynchronous method ends in `Async` and takes a `CancellationToken`; `AsyncRulesTests` passes |
| CS secrets | No secret in code, logs or answers | Pass | The audit trail, the list and the DTOs hold no password, hash, token or stamp; tests search the audit rows and both API answers for the test passwords and hash prefix |
| CS authorisation | Permission checked where the data is served | Pass | `[RequiresPermission]` on every endpoint; Receptionist and Housekeeping get 403 on all six Manager endpoints (test); the role table is covered by `RolePermissionsTests` |
| CS sessions | A change ends the old sessions | Pass | The API loads the account on every staff call and compares stamp and active flag; tests: the old token gets 401 after a change, a disable and a reset, and a token without a stamp gets 401 |
| CS limits | Rate limits | Pass | Sixth wrong current password gets 429 (test); the 31st Manager write in a minute gets 429 and another Manager is not affected (test) |
| CS tests | Tests cover success, failure and boundary | Pass | Policy boundaries (11/12/256/257), last Manager, own account, unknown account, forced change, paging boundaries, the 5-failure window |
| CS comments | XML comments in English | Pass | Public members documented; the language check passes |
| CS migration | Migration is safe to apply to a database with data | Pass | Two columns with defaults, one new table, and `UPDATE staff_member SET must_change_password = TRUE` so every existing password is replaced once; `Down` drops all three |
| CS format | Formatting | Pass | `dotnet format --verify-no-changes` is clean |

## Findings

- **Decision taken, not in the milestone text:** the migration forces every existing account to choose a new password at its next sign-in. The production Manager password was seen in a chat and no other account has ever rotated its password, so this is the shortest way to retire it. The cost is one forced change per account. Recorded for S01 to challenge.
- **Sign-in failures are audited only for accounts that exist**, so guessing at made-up names cannot grow the table without bound. A blocked name writes nothing either. ADR-0015 says "sign-in failure"; this narrows it and is stated in the code comment.
- **Audit address from a circuit.** The pages Staff and Audit run in a Blazor circuit, which has no request, so a Manager action from them records the address of the web application as the API sees it. The change of the own password and the sign-in are static forms and record the browser address. Not fixed here.
- **Limit of the stamp check.** The check is one read of the account per staff call. A restart of the API clears the in-memory limits (as in ADR-0005).
- **Not done:** the end-to-end tests of the change flow (MIL-030 task 7) and the production steps (task 9) are after this review; see RC-138 and the pull request.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion passes. The conditions are the end-to-end test of the change flow in the Docker stack and the proof in the VM, both listed below.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Add an end-to-end test that signs in as a must-change account and changes the password (Docker container) | S01 | Next change |
| Prove criterion 8 of MIL-030 in the VM after the deploy | S01 | Task 9 |
| Decide whether the circuit pages should pass the browser address too | S01 | Open question |

---

[MIL-030]: ../../milestones/mil-030-staff-accounts-and-password.md
[ADR-0015]: ../../adr/adr-0015-staff-passwords-and-audit.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
