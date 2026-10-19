-- =====================================================================
-- NFHotel — test data for development
-- =====================================================================
-- Run:
--   docker exec -i nfhotel-postgres psql -U nfhotel -d nfhotel < db/seed_dev.sql
--
-- All dates are relative to CURRENT_DATE, so check-in and check-out
-- can always be tried out, whenever the script is run.
--
-- passport_number is set to NULL. The field is encrypted with AES-GCM in
-- the application layer (B-09), so a value inserted directly in SQL would
-- not be decryptable by the application. Create a guest via /gaester if
-- you want to see the encryption work.
--
-- Enum values:
--   BookingStatus       0=Pending 1=Confirmed 2=CheckedIn 3=CheckedOut 4=Cancelled
--   RoomStatus          0=Available 1=OutOfService 2=Maintenance
--   HousekeepingStatus  0=Clean 1=DailyCleaningDue 2=DepartureCleaningDue
--                       3=CleaningInProgress 4=ServiceRequired 5=ServiceInProgress
--   RoomSize            0=Single 1=Double 2=Suite
-- =====================================================================

BEGIN;

TRUNCATE TABLE booking RESTART IDENTITY CASCADE;
TRUNCATE TABLE room    RESTART IDENTITY CASCADE;
TRUNCATE TABLE guest   RESTART IDENTITY CASCADE;

-- ---------------------------------------------------------------------
-- Rooms: two floors, all three sizes, all three bookability statuses,
-- and the whole cleaning flow represented.
-- ---------------------------------------------------------------------
INSERT INTO room (room_number, floor, size, capacity, status, housekeeping_status) VALUES
  ('101', 1, 0, 1, 0, 0),   -- Single,  available, clean
  ('102', 1, 1, 2, 0, 1),   -- Double,  available, daily cleaning missing
  ('103', 1, 1, 2, 0, 2),   -- Double,  available, departure cleaning missing
  ('104', 1, 0, 1, 0, 3),   -- Single,  available, cleaning in progress
  ('105', 1, 2, 4, 0, 0),   -- Suite,   available, clean
  ('201', 2, 1, 2, 0, 4),   -- Double,  available, service requested
  ('202', 2, 1, 3, 0, 5),   -- Double,  available, service in progress
  ('203', 2, 2, 4, 2, 0),   -- Suite,   maintenance  → cannot be booked
  ('204', 2, 0, 1, 1, 0),   -- Single,  out of service → cannot be booked
  ('205', 2, 1, 2, 0, 0);   -- Double,  available, clean

-- ---------------------------------------------------------------------
-- Guests
-- ---------------------------------------------------------------------
INSERT INTO guest (first_name, last_name, email, phone_number, country, passport_number) VALUES
  ('Anne',    'Sørensen',  'anne.sorensen@example.dk',   '+45 20 11 22 33', 'Danmark',      NULL),
  ('Bjørn',   'Lund',      'bjorn.lund@example.no',      '+47 90 11 22 33', 'Norge',        NULL),
  ('Clara',   'Nielsen',   'clara.nielsen@example.dk',   '+45 30 44 55 66', 'Danmark',      NULL),
  ('Dieter',  'Vogel',     'dieter.vogel@example.de',    '+49 151 2233445', 'Tyskland',     NULL),
  ('Elena',   'Rossi',     'elena.rossi@example.it',     '+39 340 1122334', 'Italien',      NULL),
  ('Frank',   'Jensen',    'frank.jensen@example.dk',    '+45 40 77 88 99', 'Danmark',      NULL),
  ('Greta',   'Andersson', 'greta.andersson@example.se', '+46 70 111 2233', 'Sverige',      NULL),
  ('Henrik',  'Poulsen',   'henrik.poulsen@example.dk',  '+45 50 12 34 56', 'Danmark',      NULL);

-- ---------------------------------------------------------------------
-- Bookings — cover all five statuses and the scenarios that are worth seeing
-- ---------------------------------------------------------------------
INSERT INTO booking
  (start_date, end_date, check_in_time, check_out_time, check_out_date, status, room_id, guest_id)
VALUES
  -- 1. Pending, arrival today → CHECK IN is possible at once (BR-06)
  (CURRENT_DATE, CURRENT_DATE + 3, NULL, NULL, NULL, 0, 1, 1),

  -- 2. Confirmed, arrival today → both confirmed AND ready for check-in
  (CURRENT_DATE, CURRENT_DATE + 2, NULL, NULL, NULL, 1, 2, 2),

  -- 3. Pending, arrival in a week → CHECK IN must be BLOCKED (too early)
  (CURRENT_DATE + 7, CURRENT_DATE + 10, NULL, NULL, NULL, 0, 5, 3),

  -- 4. CheckedIn since yesterday → CHECK OUT is possible, CONFIRM/CANCEL blocked (BR-11)
  (CURRENT_DATE - 1, CURRENT_DATE + 2, now() - interval '1 day', NULL, NULL, 2, 6, 4),

  -- 5. CheckedOut normally — left on the departure day
  (CURRENT_DATE - 5, CURRENT_DATE - 2, now() - interval '5 days', now() - interval '2 days',
   CURRENT_DATE - 2, 3, 7, 5),

  -- 6. EARLY CHECK-OUT (A-01): booked for 4 days from now, but left yesterday.
  --       effective_end_date becomes yesterday → room 10 is free from today.
  (CURRENT_DATE - 3, CURRENT_DATE + 4, now() - interval '3 days', now() - interval '1 day',
   CURRENT_DATE - 1, 3, 10, 6),

  -- 7. Cancelled — the only status that releases the room completely.
  --       Note that it overlaps booking 8 on room 3; that is legal precisely because it is cancelled.
  (CURRENT_DATE + 1, CURRENT_DATE + 5, NULL, NULL, NULL, 4, 3, 7),

  -- 8. Pending on the same room and period as the cancelled one above
  (CURRENT_DATE + 2, CURRENT_DATE + 6, NULL, NULL, NULL, 0, 3, 8),

  -- 9. Confirmed further out in the future
  (CURRENT_DATE + 14, CURRENT_DATE + 18, NULL, NULL, NULL, 1, 1, 1),

  -- 10. Half-open interval: starts exactly the day booking 9 ends.
  --         Must be legal — departure and arrival on the same day is not an overlap (BR-39).
  (CURRENT_DATE + 18, CURRENT_DATE + 21, NULL, NULL, NULL, 0, 1, 2),

  -- 11. Historical booking, last month
  (CURRENT_DATE - 30, CURRENT_DATE - 27, now() - interval '30 days', now() - interval '27 days',
   CURRENT_DATE - 27, 3, 2, 3),

  -- 12. Pending on a suite, arrival tomorrow
  (CURRENT_DATE + 1, CURRENT_DATE + 4, NULL, NULL, NULL, 0, 5, 4);

COMMIT;

-- ---------------------------------------------------------------------
-- Check
-- ---------------------------------------------------------------------
SELECT 'rum' AS tabel, count(*) FROM room
UNION ALL SELECT 'gaester', count(*) FROM guest
UNION ALL SELECT 'bookinger', count(*) FROM booking;

-- Effective end date per booking. Look especially at booking 6:
-- end_date is 4 days in the future, but effective_end_date is yesterday.
SELECT booking_id, room_id, status, start_date, end_date, check_out_date, effective_end_date
FROM booking
ORDER BY booking_id;
