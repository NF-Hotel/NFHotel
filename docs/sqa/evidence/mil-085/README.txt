The public address is in none of these files, and neither is any personal data.

Before the code, on the branch with the decision record only (MIL-085 task 2), 2026-11-19:
- before-tests.txt: the build of the solution with the new and changed tests on the code of main 04c0a878. It fails
  with 36 compiler errors, each a name that the tests describe and the code does not have yet (the reference, the
  open status, the limits of the guest sign-in, the caller address, the guest routes and their requests); the test
  projects that use NFHotel.TestSupport are not compiled while it fails.

The code review RC-418, 2026-11-19:
- review-tests-before.txt: the new and changed tests of the review on the code before its fixes. Nine fail where a
  fix is due: four of the store of the limits (F4), three IPv6 cases of the address key (F5), the search with spaces
  around (F8) and the sign-in page with the old footer (F9); the others pass and keep what already held (F6, F7, F11).
- review-tests-after.txt: the same tests after the fixes; every one passes.
- after-tests.txt: the checks after the last fix: the Release build, dotnet format, the tests of every project on
  this machine, the Infrastructure tests that need no PostgreSQL, the run of every test with coverage in the SDK image
  on the Docker engine of WSL (its 113 failures are the tests on PostgreSQL, which could not reach their container
  there and run in CI), the linters of the two scripts, the wiki coverage test, and the failure paths of
  check-guest-login.sh.

The gate (MIL-085 task 6), 2026-11-19:
- live-check.txt: the check of criterion 8 on the live site after the deploy of 7fd5bea (NF Hotel CD run
  44514175991, which deployed the merge of pull request 837 together with pull request 836). The planning session
  ran check-guest-login.sh with --cancel in WSL on a booking that it made for the test on /book at S01's word in chat.
  21 of 21 checks pass, and the booking is cancelled, so it holds no room. Its reference,
  25082319-9db9-435f-866f-9f7674f44256, is of version 4 and of the variant of RFC 9562. A second test booking, made
  later for a picture and cancelled by the same check, is not evidence.
