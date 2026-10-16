# Domain Model

> **Legacy note.** This is not the framework Domain Model. The project Domain Model is [docs/domain-model.md](domain-model.md), checked against QC-DM-001 (RC-036). This document is kept as the history of the analysis.

## Description

This is a domain model for a hotel management system. It includes the main entities involved in the system, such as guests, bookings, rooms, cleaning tasks, maintenance tasks, and staff members. The relationships between these entities are also defined to illustrate how they interact with each other.

## Model

```plantuml
@startuml
skinparam classAttributeIconSize 0
title Hotel Management System - Domain Model

' --- Entities ---
object Guest {
  firstname
  lastname
  email
  phone_number
  nationality
  passport_number
  ic  
}

object Booking {
  startAt
  endAt
  checkIn
  checkOut
  linkTo3PartyBooking
}

object Room {
  room_number
  size
}

object CleaningTask {
  cleanedAt
}

object MaintenanceTask {
  note
  updateNote
  startAt
  finishAt
}

object Staff {
  firstname
  lastname
  initials
  role
}

' --- Relationships ---
Guest "1" -- "*" Booking : places >
Booking "0..*" -- "1..*" Room : occupies >
Room "1" -- "0..*" CleaningTask : tracks >
Room "1" -- "0..*" MaintenanceTask : tracks >
Staff "0..1" -- "0..*" CleaningTask: assigns to >
Staff "0..1" -- "0..*" MaintenanceTask: assigns to >
@enduml
```

## Remarks

As we are using auditlogging, we do not need to store the history of the tasks. The `updateNote` attribute in the `MaintenanceTask` entity is used to store any updates or changes made to the maintenance task.
