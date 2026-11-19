# RC-421 Review of ADR-0050 Where the rules of the booking input live, second version

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-421 |
| CrossReference | [ADR-0050], [QC-ADR-001], [MIL-086], [ADR-0001], [ADR-0002], [MIL-008], [RC-420] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: review of the second version of ADR-0050 (task 1 of MIL-086), written after the first tests of task 3 met the structure tests; Go; mode autonomous, not independent | [ae4f4c0] |

---

## Artifact Under Review

- Instance reviewed: [ADR-0050], its second Version History row, on the branch `mil-086-validation-rules` (from
  `main` at 0b1350b), before the code of tasks 4 to 6.
- Checklist used: [QC-ADR-001].
- Scope: delta re-review, every criterion over what the second version changed: one force of the Context, options D
  and F, the Decision, the names in decisions 2 to 4, one negative consequence and the Affected Artifacts. The rest
  of the record is as [RC-420] reviewed it and is not reviewed again.
- Language and domain: n/a (a technical type, IT Professional English)
- Language reviewer: none
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An
  AI wrote both versions of the ADR, [RC-420] and this record.

**Why there is a second version.** The first run of the new tests of task 3, on a skeleton that put the rules where
the first version said (`NFHotel.Domain.Rules`: `GuestFieldRules`, `StayRules`, `Countries`, `IsoCountry`), failed
in the architecture tests of the Application test project: `Domain_EveryType_IsInOneOfTheSkillFolders` and
`Domain_WhenInspected_HasNoRulesNamespaceOrRulesClass` named the four types. The transcript is
`docs/sqa/evidence/mil-086/structure-tests-v1.txt`. The second version moves the rules; nothing of what they check
changes.

**What the second version rests on**, read in the code at 0b1350b, in the skill and in the earlier records:

| Change | Evidence |
| --- | --- |
| The force and option F | [ADR-0001] keeps the layer folders of the skill and says that the static rule classes of the first version of the project (`Domain/Rules/BookingRules`) were moved into the entities, the value objects and `Errors` by [MIL-008]; criteria 1 and 5 of [MIL-008] ask for no `Rules` folder, no type whose name ends in `Rules` and a test that fails when one comes back; `StructureTests` holds that test and the list of the folders of the skill |
| The formats as attributes in `Domain/Attributes` | The skill lists `Attributes` among the folders of `Domain` ("the domain's own marker attributes"); [ADR-0002] puts the rules of a guest field on the entity as data annotations, run by `EntityValidation.ThrowIfInvalid`; `Guest` carries them today |
| The joined rules as members of `Booking` | `Booking` already holds the check of the arrival (`GuardPeriodIsNotInThePast`) and, since [MIL-008], the overlap rule of the former `BookingRules` (`ConflictsWith`); `Booking.Create` and `Booking.Reschedule` take `today` from the caller |
| The normal form and the countries as value objects | `ValueObjects` holds the types of the domain that carry a value and no identity, such as `DateRange`; neither type has an identity or is stored on its own |
| The skeleton in the new places | `docs/sqa/evidence/mil-086/before-tests.txt`: the solution builds with no warning, no architecture or test-rule test fails, and the 252 failures are rules that the code does not have yet |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | Unchanged: ADR-0050 in `adr-0050-where-the-rules-of-the-booking-input-live.md`; the registry gives 0051 next |
| 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | One force added: the folders of the skill and the refusal of `Rules` in the domain, with [ADR-0001] and [MIL-008]; six options now |
| 3 | At least two options were considered, or the absence of alternatives is justified | Pass | Option F, the place of the first version, is added with the reason it lost; D says where each rule lives |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | Two sentences that state option D as reworded, with the places of each kind of rule and the table |
| 5 | Consequences list both Positive and Negative outcomes | Pass | One negative point added: `Booking` holds limits that staff do not have, and the alias that the web application needs; the positive points hold as before |
| 6 | Quality-attribute impacts (performance, security, portability, etc.) are named where the decision affects them | Pass | Maintainability is the attribute that the move serves (one place per kind of rule, the layout of the skill); security, usability and performance are unchanged by it |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | [ADR-0001] and [MIL-008] added, both with "stands" and what the change means for them, both in the CrossReference with this record; the [ADR-0002] bullet names the new places |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | Row 2 is new and `Accepted` after this review, its commit `pending` until the commit; row 1 is `Deprecated` and otherwise unchanged |
| 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | The ADR is not superseded; its own earlier row is deprecated, and option F records what it said |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | No new abbreviation |
| 11 | Status is one of `Proposed`, `Accepted`, `Rejected`, `Deprecated` or `Superseded by ADR-NNNN`, and every Version History row uses only these | Pass | `Deprecated` and `Accepted` |

## Findings

- **F1, the finding of this review: [RC-420] missed a conflict with the structure of the domain.** The first version
  put two static rule classes in `Domain/Rules`, which [ADR-0001] and [MIL-008] removed and a test refuses. [RC-420]
  read the code that each decision changes but not the records and tests that bound the domain as a whole, and
  [QC-ADR-001] has no criterion that asks for it. The tests of task 3 caught it before any code, which is what the
  order of the milestone (decision record, then tests, then code) is for. This review read [ADR-0001], [MIL-008],
  `StructureTests` and the skill against every place the second version names.
- **F2, note: a validation attribute in a folder of marker attributes.** The skill describes `Attributes` as "the
  domain's own marker attributes", and these five carry a rule. The AI places them there as stand-in for S01:
  [ADR-0002] makes the rules of a field attributes of the entity, `Attributes` is the one folder of the skill for
  attribute types, and `Entities` or `ValueObjects` would name them as what they are not. The structure test lists
  the folder, as the change of `StructureTests.DomainFolders` in task 3 shows.
- **F3, note: `Booking` grows.** It gets four constants and five static members. Each one guards the dates or the
  party of a booking, as its existing check of the arrival does; those of the public site say `Public` in their
  names, so a reader sees that staff are not bound by them. Accepted as stand-in for S01.
- **F4, note: a name the web application cannot use directly.** The namespace `NFHotel.Web.Features.Booking` hides
  the type `Booking`; the view models and the tests of the web application name it through an alias. The ADR records
  this as a negative consequence; renaming the namespace is outside [MIL-086].

## Overall Verdict

Go. Every applicable criterion of [QC-ADR-001] passes for the second version, which keeps every decision of the first
and moves the rules to places that [ADR-0001], [MIL-008] and the structure tests allow. The AI accepted the second
version of ADR-0050 and this record as stand-in for S01 in autonomous mode, at S01's word in chat on 2026-11-19; row 2
of ADR-0050 is `Accepted` and row 1 `Deprecated`. The reviewer is not independent: an AI wrote the ADR, [RC-420] and
this record. S01 may challenge any decision of it, F2 and F3 above all.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the second version of ADR-0050, or return them | S01; done on 2026-11-19 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-086-validation-rules` |

---

[ADR-0050]: ../../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[MIL-086]: ../../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[ADR-0001]: ../../adr/adr-0001-clean-architecture-structure.md
[ADR-0002]: ../../adr/adr-0002-validation-attributes-on-entities.md
[MIL-008]: ../../milestones/mil-008-domain-rules-in-entities.md
[RC-420]: ./rc-420-adr-0050.md
[ae4f4c0]: https://github.com/NF-Hotel/NFHotel/commit/ae4f4c0c03a8dda8d8578f90131fc44598e4814c
