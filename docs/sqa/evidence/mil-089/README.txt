The public address is in none of these files, and neither is any personal data: the emails of the load test are of
example.com, its addresses are of the documentation range 2001:db8::/32, and every mail went to the mail catcher of
the throwaway stack.

The load test with the booking flow (MIL-089 task 4), 2026-11-20, on a throwaway stack of the branch
mil-089-booking-flow-attacked (main at 3cfe3ab, MIL-085 to MIL-088, with the load test of this task):
- load-verification-off.txt: ops/scripts/load-test.sh with the verification of a booking by email off, as production
  ran until 2026-11-22. 20 virtual guests for two minutes, each visit from an address of its own: 3,445
  requests of the guests, 0 failed, 95 percent within 15.3 ms; every booking answered 200. The burst from one address
  got exactly the 60 searches and the 5 bookings that the limits allow, 65 answers, and 23 refusals with 429 and the
  code of the limits, and no answer of the 500 kind.
- load-verification-on.txt: the same run on the same stack after ops/scripts/e2e-stack.sh verification on: every
  booking request answered 202, 3,445 requests of the guests, 0 failed, 95 percent within 19.9 ms; the burst again 65
  answers and 23 refusals, and no answer of the 500 kind.

The tests of the abuse cases without their control (MIL-089 task 2, criterion 2):
- without-their-control.txt: five tests, each run with its control taken out of the source and then with it in place.
  On 2026-11-20, in a worktree of main at 3cfe3ab: case 8 (a field that the request type does not have), case 7
  (writing the address of the caller), case 2 (trying emails against a reference) and case 6 (the race for the last
  room, a test that is new in MIL-089). On 2026-11-21, in the worktree of the branch: case 11 (many slow requests,
  the limit of the circuits that is new in MIL-089). Each test failed without its control and passed with it.
