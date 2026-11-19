# RBAC — Access Control Matrix

> The access control matrix states which roles may use which functions and data in NFHotel.
> Since MIL-018 it is the implemented table: the code holds the same table, and a test compares the two cell by cell.

---

## Purpose

The purpose of the matrix is to define the access rights of the different roles in NFHotel, and to be the one place where a reader can see them.

The matrix makes clear which functions and data the different roles need access to, and supports the principle of **least privilege**: a user only has the rights that are necessary for the user's tasks.

Access control is enforced not only in the user interface, but also in the API and the application layer.

---

## Roles

| Role | Description |
|---|---|
| **Guest** | The hotel's guest. A guest signs in with the booking reference and the email of the booking; there is no guest account and no password. The guest token names one booking, and every guest right is limited to that booking ([ADR-0047], MIL-018, [ADR-0049]). |
| **Receptionist** | Employee who handles guests and bookings. |
| **Manager** | Employee with extended administrative rights. |
| **Housekeeping** | Employee who handles cleaning and housekeeping tasks. |

---

## Access control matrix

One row per permission of the code (`Permissions` in `NFHotel.Application`). The first ten rows are the functions of the
original matrix; the rows after them are the permissions that the back office added since ([ADR-0005], [ADR-0013],
[ADR-0014], [ADR-0017]).

| Function / data | Permission | Guest | Receptionist | Manager | Housekeeping |
|---|---|:---:|:---:|:---:|:---:|
| Create booking | `Bookings.Create` | o | o | o | x |
| Change own booking | `Bookings.EditOwn` | o | o | o | x |
| Change other guests' bookings | `Bookings.EditOthers` | x | o | o | x |
| See own guest details | `Guests.ViewOwn` | o | o | o | x |
| See other guests' details | `Guests.ViewOthers` | x | o | o | x |
| See own passport data | `Guests.Passport.ViewOwn` | x | x | x | x |
| See other guests' passport data | `Guests.Passport.ViewGuest` | x | o | o | x |
| Change prices/discounts | `Prices.Manage` | x | x | o | x |
| See housekeeping information | `Housekeeping.View` | x | o | o | o |
| Change housekeeping status | `Housekeeping.UpdateStatus` | x | x | o | o |
| See bookings in the back office | `Bookings.View` | x | o | o | x |
| Change and delete guests in the back office | `Guests.Manage` | x | o | o | x |
| See rooms and their status | `Rooms.View` | x | o | o | o |
| Create, change and delete rooms | `Rooms.Manage` | x | x | o | x |
| See the revenue | `Sales.View` | x | x | o | x |
| See the stored benchmark runs | `Benchmark.View` | x | x | o | x |
| Start a benchmark run | `Benchmark.Run` | x | x | o | x |
| Delete a stored benchmark run | `Benchmark.Delete` | x | x | o | x |
| Set the default AI models | `AiDefaults.Manage` | x | x | o | x |
| Add and delete benchmark models | `Benchmark.ModelsManage` | x | x | o | x |
| Read the market analysis report | `MarketAnalysis.View` | x | x | o | x |
| Start a market analysis run | `MarketAnalysis.Start` | x | x | o | x |
| Manage staff accounts and read the audit trail | `Staff.Manage` | x | x | o | x |
| Change own staff password | `Staff.ChangeOwnPassword` | x | o | o | o |

**Symbols:**

- `o` = allowed
- `x` = refused

**Notes:**

- *Own* for a guest means the booking in the guest token. A guest reads the receipt of that booking, cancels it and
  moves it to other dates in the same room through the guest routes `api/guest/booking`, which take no key, so no other
  booking can be named; the receipt holds nothing of the guest. A guest token on any other route of the API gets 403
  ([ADR-0047] decision 5, [ADR-0049] decision 3).
- *See own passport data* is refused for every role. A guest never sees a passport number, not even the own one: the
  guest's answers carry no passport number ([ADR-0047] decision 4). Staff are not guests and have no own passport data
  in the system; they read a guest's passport number through the next row.
- *Create booking* stays public: a visitor without any token books a stay and lists the free rooms. The guest who has
  just booked receives the guest token with the booking ([ADR-0047] decisions 2 and 5).

---

## Open questions

The question of the own passport data is answered by [ADR-0047]: a guest does not see it. The following access rules
are still to be clarified with the team/PO:

- Which booking details does Housekeeping specifically need?
- Should Housekeeping have a limited view of booking details instead of access to the whole booking?
- Are any functions or data missing from the access control matrix?

---

## Implementation note

The matrix is implemented by `RolePermissions` in `NFHotel.Application`, the one table the API and the web application
read. `AccessControlMatrixTests` reads this file and fails when a permission is missing here, when a cell is neither
`o` nor `x`, or when a cell differs from the table in the code (MIL-018 criterion 6). A change of a right is therefore a
change of both, in the same pull request.

Access to own resources requires more than a check of the user's role. For a guest the API takes the booking
from the guest token, and the use cases check again that it is the booking of the token (`GuestOwnership`, [ADR-0047]
decision 5, [ADR-0049] decision 3).

---

[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[ADR-0013]: ../adr/adr-0013-benchmark-results.md
[ADR-0014]: ../adr/adr-0014-default-ai-model.md
[ADR-0017]: ../adr/adr-0017-staff-passwords-and-audit.md
[ADR-0047]: ../adr/adr-0047-guest-login.md
[ADR-0049]: ../adr/adr-0049-booking-reference.md
