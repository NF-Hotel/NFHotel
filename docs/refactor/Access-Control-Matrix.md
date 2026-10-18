# RBAC — Access Control Matrix

> The access control matrix describes which roles may access which functions and data in NFHotel.
> The matrix is the basis for the later implementation of authentication and authorisation.

---

## Purpose

The purpose of the matrix is to define the access rights of the different roles in NFHotel before they are implemented in the system.

The matrix must make clear which functions and data the different roles need access to, and at the same time support the principle of **least privilege** — a user should only have the rights that are necessary for the user's tasks.

Access control must be enforced not only in the user interface, but also in the application layer.

---

## Roles

| Role | Description |
|---|---|
| **Guest** | The hotel's guest. Login and authorisation for Guest are implemented in a later phase. |
| **Receptionist** | Employee who handles guests and bookings. |
| **Manager** | Employee with extended administrative rights. |
| **Housekeeping** | Employee who handles cleaning and housekeeping tasks. |

---


## Access control matrix

| Function / data | Guest | Receptionist | Manager | Housekeeping |
|---|:---:|:---:|:---:|:---:|
| Create booking | o | o | o | x |
| Change own booking | o | o | o | x |
| Change other guests' bookings | x | o | o | x |
| See own guest details | o | o | o | x |
| See other guests' details | x | o | o | x |
| See own passport data | ? | o | o | x |
| See other guests' passport data | x | o | o | x |
| Change prices/discounts | x | x | o | x |
| See housekeeping information | x | o | o | o |
| Change housekeeping status | x | x | o | o |

**Symbols:**

- `o` = access
- `x` = no access
- `?` = to be clarified

---

## Open questions

The following access rules must be clarified with the team/PO before they are implemented:

- Should a Guest be able to see their own passport data after upload?
- Which booking details does Housekeeping specifically need?
- Should Housekeeping have a limited view of booking details instead of access to the whole booking?
- Are any functions or data missing from the access control matrix?

---

## Implementation note

The matrix describes the desired access control and is not itself an implementation.

When the access rules have been reviewed and approved, this can be used as the basis for implementing roles and authorisation in NFHotel.

Access to own resources requires more than a check of the user's role. If a Guest may, for example, see their own booking, the system must also check that the specific booking belongs to the authenticated user.
