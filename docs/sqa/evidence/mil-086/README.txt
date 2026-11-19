The public address is in none of these files, and neither is any personal data.

Before the code, on the branch with the decision record and the tests (MIL-086 tasks 2 and 3), 2026-11-19:
- structure-tests-v1.txt: the first run of the new tests, on a skeleton that put the rules in a folder
  Domain/Rules as the first version of ADR-0050 said. The architecture tests refused that folder and its names,
  which ADR-0001 and MIL-008 removed, and the rules of the tests refused ten test names and a region. This is the
  evidence of the finding of RC-421, which reviewed the second version of ADR-0050.
- before-tests.txt: the run on a skeleton in the places of the second version (Domain/Attributes, TextNormalForm,
  Countries, the stay members of Booking). The solution builds with no warning, and 252 tests fail, each on a rule
  that the code does not have yet: Domain 143 of 716, Application 21 of 777, Api 63 of 604, ApiClient 0 of 122,
  Web 25 of 863.

After the code (MIL-086 tasks 4 to 6), 2026-11-19:
- before-tests-normal-form.txt: three tests of the normal form of the sign-in email and of the two staff searches,
  written when the code was read against the table and found not to normalise them, and run before that code; the
  same run shows the async rule test refusing the first form of the new event filter of the API.
- after-tests.txt: the whole run after the code: the build with no warning, every test project but the browser tests
  green (the infrastructure tests against PostgreSQL in a container), the whitespace format check, the linters of
  the operations code, and the checks of the nginx test for the body limit and the timeouts.

After the code review (RC-424), 2026-11-19:
- before-review-fixes.txt: the tests of the points of the independent read of the code, written before their fixes:
  20 fail, each on its point (Domain 1 of 717, Application 1 of 782, Api 8 of 613, Web 5 of 881, Infrastructure 1
  of 243); the two tests that run the API on Kestrel pass at once, because the code they test was right.
- mutation-body-limit.txt: the tests of the body limit run with the limit broken, one break at a time, each put back
  afterwards. Without the RequestSizeLimit of the benchmark intake both tests of the intake fail with 413. The 64 KB
  of the server are held twice, by the limit of Kestrel for every request and by the limit that the middleware sets
  per endpoint: with either left the chunked body is refused, and with both removed it is read and the test fails.
- after-review.txt: the whole run after the fixes: the build with no warning, every test project but the browser
  tests green (the infrastructure tests against PostgreSQL in a container), and both format checks.
