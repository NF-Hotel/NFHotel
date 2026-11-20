The public address is in none of these files, and neither is any personal data: the emails are of example.com and
the addresses of the documentation ranges or of the containers of the stack.

Before the code, on the branch with the decision record and the tests (MIL-087 tasks 2 and 3), 2026-11-19:
- before-tests.txt: the tests of the limits, the address of a caller, the security events and the mark of a public
  booking on a skeleton with the names and no behaviour. The solution builds with no warning, and every failure is a
  rule that the code did not have yet.

After the code (MIL-087 tasks 4 to 6), 2026-11-19:
- stack-check.txt: ops/tests/check-e2e-stack.sh against the throwaway stack with the numbers of production: the 61st
  search and the 6th booking request from one address refused with 429, the code and the whole seconds of
  Retry-After, a search of another address still answered, failed guest sign-ins with forged address headers counted
  under the address that nginx saw, and the security events in the log with the address and a masked email and no
  email of the check in any log. Criterion 8 of MIL-087, before the live site.

After the code review (RC-428), 2026-11-19:
- before-review-fixes.txt: the tests of the points of the independent read of the code, written before their
  fixes: 13 fail, each on its point (Api 2 of 698, Infrastructure 5 of 279, and 1 and 5 in two runs of the tests
  written after the first); the tests of the rounding of Retry-After and of the email count pass at once, because
  the code they test was right.
- after-tests.txt: the whole run after the fixes: the build with no warning, every test project but the browser
  tests green (3,538 tests), the whitespace format check, the linters of the operations code, the checks of the
  monitoring configuration with the rule tests of the two new alerts, and the coverage of the wiki pages.

The gate (MIL-087 task 8), 2026-11-20:
- live-check.txt: the check of criterion 8 on the live site after the deploy of 6f7bb80 (NF Hotel CD run
  44624470820). In Chromium, in the container image of Playwright, one address searched for free rooms on /book: the
  first 60 searches showed the room step within 13.1 seconds, the 61st was refused with the one text of the limits,
  and a search after the minute was answered again. The counter of the security events had no refusal by a limit
  before the run and one refusal of the limit "search" after it. Nothing was booked.
