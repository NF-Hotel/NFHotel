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
\echo '=== TEST 1: overlap paa samme rum — FORVENTET: afvist ==='
-- Room 1 is booked CURRENT_DATE..+3 (booking 1). This one overlaps in the middle.
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE + 1, CURRENT_DATE + 2, 0, 1, 3);

\echo ''
\echo '=== TEST 2: overlap paa nyt rum uden konflikt — FORVENTET: accepteret ==='
-- Room 9 (204) has no bookings.
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE + 1, CURRENT_DATE + 2, 0, 9, 3);

\echo ''
\echo '=== TEST 3: ankomst samme dag som en anden gaests afrejse — FORVENTET: accepteret ==='
-- Booking 1 paa rum 1 slutter CURRENT_DATE + 3. Halvaabent interval [) betyder
-- at en ny booking maa starte praecis der (BR-39).
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE + 3, CURRENT_DATE + 5, 0, 1, 4);

\echo ''
\echo '=== TEST 4: genudlejning efter TIDLIG UDTJEKNING — FORVENTET: accepteret ==='
-- This is A-01, the whole point of effective_end_date.
-- Booking 6 on room 10 is booked until CURRENT_DATE + 4, but the guest checked out yesterday.
-- Without effective_end_date this one would be rejected, even though the room is empty.
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE, CURRENT_DATE + 3, 0, 10, 5);

\echo ''
\echo '=== TEST 5: overlap med en ANNULLERET booking — FORVENTET: accepteret ==='
-- Booking 7 on room 3 is Cancelled and therefore does not block.
INSERT INTO booking (start_date, end_date, status, room_id, guest_id)
VALUES (CURRENT_DATE + 1, CURRENT_DATE + 2, 0, 3, 6);

\echo ''
\echo '=== OPRYDNING: fjerner de testrækker der blev accepteret ==='
DELETE FROM booking WHERE guest_id IN (3,4,5,6) AND check_in_time IS NULL AND booking_id > 12;

\echo ''
\echo '=== RESULTAT ==='
\echo 'Test 1 skal have givet:  ERROR ... ex_booking_room_period'
\echo 'Test 2 til 5 skal alle have givet: INSERT 0 1'
\echo ''
\echo 'Fejler test 1 IKKE, er constrainten ikke aktiv — tjek at migrationen er koert.'
\echo 'Fejler test 4, er effective_end_date forkert — det er A-01 der er braekket.'
