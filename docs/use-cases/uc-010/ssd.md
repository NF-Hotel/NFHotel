# SSD-001 View and Change Own Booking

## Metadata
| Key | Value |
| --- | --- |
| ID | SSD-001 |
| CrossReference | [UC-010], [DM-004], [OC-001], [UC-011], [SSD-002] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | signIn takes the booking reference in place of the booking number, and the refused sign-in counts per reference and per address (MIL-085, ADR-0049)<br>Go in RC-417; accepted by the AI as stand-in for S01 in autonomous mode | [e551458] |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | Extension 1a now points to the booking on the website of UC-011, whose verifyBooking signs the guest in (MIL-088, ADR-0052, ADR-0053)<br>Go in RC-435; accepted by the AI as stand-in for S01 in autonomous mode | [739a25b] |

---

## Source Use Case

[UC-010] View and Change Own Booking: the main success scenario (steps 1 to 8), and the refused sign-in of
extensions 2a and 2b in a second diagram. The other extensions are out of scope of the diagrams: in 1a the booking on
the website of [UC-011] signs the guest in instead of signIn, by its verifyBooking in [SSD-002] or, while the
verification by email is switched off, by the offer after its bookAtOnce; 3a to 3c and *a to *c are refusals of a
message below, listed as exceptions in [OC-001]; 7a changes nothing that the guest sees.

## Diagram

Main success scenario:

```plantuml
@startuml
hide footbox
actor "Guest" as Guest
participant ":System" as System

Guest -> System : signIn(bookingReference, email)
activate System
System --> Guest : own booking, changes it allows now
deactivate System

opt the guest wants other dates
  Guest -> System : changeDates(startDate, endDate)
  activate System
  System --> Guest : booking with the new dates
  deactivate System
end

opt the guest cancels
  Guest -> System : cancelBooking()
  activate System
  System --> Guest : cancelled booking
  deactivate System
end

Guest -> System : signOut()
@enduml
```

Refused sign-in (extensions 2a and 2b):

```plantuml
@startuml
hide footbox
actor "Guest" as Guest
participant ":System" as System

loop five failed attempts for one reference, or twenty from one address, within 15 minutes
  Guest -> System : signIn(bookingReference, email)
  activate System
  System --> Guest : refusal: the reference and the email do not match a booking
  deactivate System
end

Guest -> System : signIn(bookingReference, email)
activate System
System --> Guest : refusal: too many attempts, try again later
deactivate System
@enduml
```

## System Operations Table

| Step | Message (verb phrase) | Parameters | Return | Use case step |
| --- | --- | --- | --- | --- |
| 1 | signIn | booking reference, email | the own booking with the changes it allows now | 1, 2 |
| 2 | changeDates | startDate (the arrival date), endDate (the departure date) | the booking with the new dates | 3, 4 |
| 3 | cancelBooking | none | the cancelled booking | 5, 6 |
| 4 | signOut | none | none | 7, 8 |

Steps 1 and 2 of the use case are one message: the system shows the booking as the answer to the sign-in.
changeDates and cancelBooking name no booking: they act on the booking of the guest's sign-in.

## Lifecycle Notes

- The `:System` is the NFHotel platform as the guest reaches it: the website and the hotel API behind it.
- signIn starts the guest's sign-in for one booking; it ends with signOut or two hours after signIn, whichever comes
  first. changeDates and cancelBooking work only within it. A refused signIn starts nothing. verifyBooking of
  [SSD-002] starts the same sign-in without signIn (extension 1a).
- A staff sign-in in the same browser is a separate sign-in: signIn and signOut neither start nor end it.

---

[UC-010]: ./uc.md
[DM-004]: ../../domain-model.md
[OC-001]: ./oc.md
[UC-011]: ../uc-011/uc.md
[SSD-002]: ../uc-011/ssd.md
[e551458]: https://github.com/NF-Hotel/NFHotel/commit/e551458c167261cad75f9ffd2902fb6163a78b81
[739a25b]: https://github.com/NF-Hotel/NFHotel/commit/739a25b00890c77fe75ba0bb02fcd64a00548089
