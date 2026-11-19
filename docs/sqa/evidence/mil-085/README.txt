The public address is in none of these files, and neither is any personal data.

Before the code, on the branch with the decision record only (MIL-085 task 2), 2026-11-19:
- before-tests.txt: the build of the solution with the new and changed tests on the code of main 04c0a878. It fails
  with 36 compiler errors, each a name that the tests describe and the code does not have yet (the reference, the
  open status, the limits of the guest sign-in, the caller address, the guest routes and their requests); the test
  projects that use NFHotel.TestSupport are not compiled while it fails.
