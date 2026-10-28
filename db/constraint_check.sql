-- =====================================================================
-- Verification of the exclusion constraint (A-01, B-03)
-- =====================================================================
-- Run AFTER seed_dev.sql:
--   docker exec -i nfhotel-postgres psql -U nfhotel -d nfhotel < db/constraint_check.sql
--
-- This is the most important single mechanism in the whole design: it makes
-- double booking physically impossible, even if two users press at the same time.
-- The old WPF system had no overlap check on creation at all.
--
-- Four tests. The first three MUST fail, the fourth MUST succeed.
-- =====================================================================

\set ON_ERROR_STOP off
\echo ''
\echo '=== TEST 1: overlap on the same room — EXPECTED: rejected ==='
-- Room 1 is booked CURRENT_DATE..+3 (booking 1). This one overlaps in the middle.
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE + 1, CURRENT_DATE + 2, 0, 1, 3);

\echo ''
\echo '=== TEST 2: overlap on a new room without conflict — EXPECTED: accepted ==='
-- Room 9 (204) has no bookings.
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE + 1, CURRENT_DATE + 2, 0, 9, 3);

\echo ''
\echo '=== TEST 3: arrival on the same day as another guest's departure — EXPECTED: accepted ==='
-- Booking 1 on room 1 ends CURRENT_DATE + 3. A half-open interval [) means
-- that a new booking may start exactly there (BR-39).
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE + 3, CURRENT_DATE + 5, 0, 1, 4);

\echo ''
\echo '=== TEST 4: re-letting after EARLY CHECK-OUT — EXPECTED: accepted ==='
-- This is A-01, the whole point of effective_end_date.
-- Booking 6 on room 10 is booked until CURRENT_DATE + 4, but the guest checked out yesterday.
-- Without effective_end_date this one would be rejected, even though the room is empty.
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE, CURRENT_DATE + 3, 0, 10, 5);

\echo ''
\echo '=== TEST 5: overlap with a CANCELLED booking — EXPECTED: accepted ==='
-- Booking 7 on room 3 is Cancelled and therefore does not block.
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE + 1, CURRENT_DATE + 2, 0, 3, 6);

\echo ''
\echo '=== CLEAN-UP: removes the test rows that were accepted ==='
DELETE FROM booking WHERE guest_id IN (3,4,5,6) AND check_in_time IS NULL AND booking_id > 12;

\echo ''
\echo '=== RESULTAT ==='
\echo 'Test 1 must have given:  ERROR ... ex_booking_room_period'
\echo 'Tests 2 to 5 must all have given: INSERT 0 1'
\echo ''
\echo 'Fejler test 1 IKKE, er constrainten ikke aktiv — tjek at migrationen er koert.'
\echo 'Fejler test 4, er effective_end_date forkert — det er A-01 der er braekket.'
