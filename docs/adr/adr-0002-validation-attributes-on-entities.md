# ADR-0002 Validation attributes on the entities

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0002 |
| CrossReference | [BC-001], [ADR-0001], [DM-004], [MIL-009] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Accepted after RC-042 Go | [4cab5fc] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The field rules of `Room` and `Guest` (required, maximum length, a positive
floor and capacity, a defined room size, an email with an at sign and a dot)
are written as code: the static methods `Room.Validate` and `Guest.Validate`,
with private helpers, make up about 170 lines inside the two entities, and the
application services call them before the entity is built. The user wants the
entities kept clean by using attributes instead.

Options considered:

1. **Keep the static `Validate` methods.** No change; the rules are code, found in the entity but not declarative.
2. **Attributes on the entity properties** (`System.ComponentModel.DataAnnotations`: `Required`, `StringLength`, `Range`, `RegularExpression`, `EnumDataType`), each with the error code as `ErrorMessage`, checked by one helper in `Domain` with `Validator.TryValidateObject`. The rules sit beside the property they guard. `System.ComponentModel.Annotations` is part of the .NET base class library, so `Domain` still references no package.
3. **Attributes on the request types in `Application`** and fewer guards in the entity. The entity could then be built invalid, which breaks the rule that no invalid entity can exist (BR-84).
4. **A validation library** (for example FluentValidation). It adds a package to `Domain` or moves the rules out of the entity.

## Decision

We follow option 2. Each field rule is an attribute on the property, with an
error code from `Errors` as its `ErrorMessage`. The factories `Create` and
`UpdateDetails` assign the values, run the helper, and throw `DomainException`
carrying every error code, not only the first (BR-60, BR-96). The services build
the entity inside the call and turn a `DomainException` into a failed `Result`
with all codes, so the services no longer call a separate `Validate`. The
`Validate` and `IsValid…` methods are deleted from the entities. The codes, the
rules and the order of the errors stay the same; a test asserts all three.

The email rule (an at sign and a dot, as in BR-52 to BR-56) is a
`RegularExpression` attribute and is not the framework's `EmailAddress`
attribute, which is stricter and would change behaviour.

## Consequences

**Positive:**

- The entity classes lose the validation code; each rule is one line beside its property.
- The rules are declarative and are read by EF Core and by tools, so the maximum lengths are stated once.
- One path to an entity (`Create`) for both the services and the tests, and every error is returned.
- Maintainability and testability improve (declarative rules, one path to build an entity); there is no change to performance or security.

**Negative:**

- `Domain` now uses `System.ComponentModel.DataAnnotations`; it is part of the framework class library and no package, but it is a framework namespace in the domain, accepted here by this decision.
- The order of the errors depends on the order of the properties, which the `Validator` takes from reflection; a test fixes it.
- Attributes cannot express a rule that needs several properties (none exists today).

## Affected Artifacts

- [ADR-0001] — the structure decision stays; this ADR only adds how rules are expressed
- [DM-004] — unchanged: the model has no rules
- [MIL-009] — the milestone that carries the change

---

[BC-001]: ../business-case.md
[ADR-0001]: ./adr-0001-clean-architecture-structure.md
[DM-004]: ../domain-model.md
[MIL-009]: ../milestones/mil-009-validation-attributes.md
[4cab5fc]: https://github.com/NF-Hotel/NFHotel/commit/4cab5fc64b8358e18f9fec630d27d8bc267e6787
