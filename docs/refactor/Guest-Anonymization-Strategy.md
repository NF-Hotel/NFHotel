# D-05 — Anonymisation Strategy for Guest Data

> Work to make **D-05 (Q-41)** concrete.
> Booking data and personal data have separate deletion and retention rules. This document describes the handling of personal data when a guest cannot be deleted directly because of existing bookings.

---

## Purpose

In the current model a `Guest` cannot be deleted if the guest has existing bookings. Bookings refer to the guest via `GuestId` and the relation is protected with `DeleteBehavior.Restrict`.

D-05 at the same time requires that deletion of booking data and personal data be treated as two separate tracks.

The purpose of the strategy is therefore to define how a guest's personal data can be removed or anonymised without breaking existing booking relations.

The retention periods are not yet fixed and are therefore still stated as **X**, cf. D-05.


---

## Current situation

`GuestService.DeleteAsync()` already supports deleting a `Guest`.

If the guest has no bookings, the guest is removed via `IGuestRepository.Remove()`.

If the guest has one or more bookings, the deletion is rejected with `ErrorCodes.Guest.HasBookings`. This is because existing bookings still refer to the guest via `GuestId`.

The relation is also protected in the database with `DeleteBehavior.Restrict`, so a guest with linked bookings cannot be deleted and leave invalid booking relations behind.

**Consequence:** A guest with existing bookings currently cannot have their personal data deleted through the existing delete function.

`Guest` contains the following guest details:

| Field | Current requirement |
|---|---|
| `FirstName` | Mandatory |
| `LastName` | Mandatory |
| `Email` | Mandatory |
| `PhoneNumber` | Mandatory |
| `Country` | Mandatory |
| `PassportNumber` | Optional |

The mandatory fields are enforced by `GuestRules`. The existing `UpdateDetails()` can therefore not be used simply to empty the fields as an anonymisation mechanism.


---

## Proposed anonymisation strategy

If a `Guest` has no existing bookings, the guest can still be physically deleted through the existing `DeleteAsync()`.

If a `Guest` has existing bookings, it is proposed to keep the `Guest` row and the relation to existing bookings, while the guest's identifying details are anonymised.


`GuestId` is kept as a technical reference, so existing relations between `Guest` and `Booking` are not broken.

This solution makes it possible to keep the booking relations without depending on the original identifying guest details.

The specific values and fields to be used for anonymisation must of course be clarified and approved before the strategy is implemented.


---

## Fields on anonymisation

The following handling of guest details is proposed as a starting point:

| Field | Proposed handling | Reason |
|---|---|---|
| `GuestId` | Kept | The booking relations must still be able to refer to the guest |
| `FirstName` | Anonymised | Can identify the guest |
| `LastName` | Anonymised | Can identify the guest |
| `Email` | Anonymised | Can identify and contact the guest |
| `PhoneNumber` | Anonymised | Can identify and contact the guest |
| `Country` | To be clarified | It must be assessed whether the information is still needed after anonymisation |
| `PassportNumber` | Removed | The original passport number should not be kept as part of an anonymised guest |

The specific replacement values have not yet been decided. They must, among other things, take into account the existing requirements in `GuestRules`, where several of the fields must not be empty.

---

## Technical proposal
The existing `UpdateDetails()` is intended for ordinary editing of a guest and follows the validation in `GuestRules`.

It is not known whether the existing `UpdateDetails()` is suited for anonymisation, since it is intended for ordinary editing of a guest and follows the validation in `GuestRules`.

It must therefore be clarified how anonymisation should be handled technically.

The following parts of the system can be expected to be affected:

| Area | Expected change |
|---|---|
| `Guest` | Must be able to perform a separate anonymisation operation |
| `GuestRules` | Ordinary creation and editing must still enforce the existing rules |
| `GuestService` | Must be able to choose between physical deletion and anonymisation depending on existing bookings |
| `IGuestRepository` | The existing relation to bookings must still be checkable |
| Tests | Tests of both physical deletion and anonymisation must be added |

The specific implementation is fixed only when the anonymisation strategy has been approved.

---

## Open questions

The following points should preferably be clarified with the team before the anonymisation strategy can be implemented:

- Should the proposed strategy of keeping the `Guest` row and anonymising the guest details be approved?
- Which specific replacement values should be used for `FirstName`, `LastName`, `Email` and `PhoneNumber`?
- Should `Country` be kept after anonymisation, or should the field also be anonymised?
- Should `PassportNumber` be set to `null` on anonymisation?
- How should the system mark that a `Guest` has been anonymised?
- Should anonymisation happen only when a guest asks for their personal data to be deleted, or should it also happen automatically after a certain period?
- How long should booking data and the guest's personal data be kept? The retention periods are still **X** in D-05 and must be fixed.
