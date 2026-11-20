The public address is in none of these files, and neither is any personal data: the emails are of example.com, every
mail went to the mail catcher of the throwaway stack, and no token of a verification link is written down here.

Before the code, on the branch with the decision records and the tests (MIL-088 tasks 1, 2 and 4), 2026-11-19:
- before-tests.txt: the tests of the use case of the request and the verification on a skeleton with the names and
  no behaviour. 43 of the 828 tests of the application fail, each on a rule that the code did not have yet; the
  tests of the domain pass with the entities of the request and of the record of a mail, which were written first.

After the code (MIL-088 tasks 5 to 7) and its code review (RC-436), 2026-11-20:
- before-review-fixes.txt: the tests of the points F1 and F3 of the code review, run without their fixes: 3 of 41
  fail with the catch around the second mail taken out, 1 of 93 on a bound that ran out in the TLS handshake and
  read as tls, and 1 of 17 with the old quit of 30 seconds.
- after-tests.txt: the whole run on the code at 4239529, after which the code changed only in a comment (F16 of
  RC-436, 733a5b2), on which the build and the check of the comment language ran again: the build with no warning,
  every test project but the browser tests green (3,790 tests), the whitespace format check, the linters of the
  operations code, the checks of the monitoring configuration with the rule tests of the three alerts of the mail,
  the 27 tests of the job "Ops script tests" with the workflow policy, the coverage of the wiki pages and the check
  of the comment language.
- stack-check.txt: ops/tests/check-e2e-stack.sh against the throwaway stack with the forwarder and the mail catcher
  (0ce2e0c, before the comments of F14 of RC-436, which change no behaviour): the 54 checks of the stack, among them
  the mail network that is internal and the forwarder that reaches the catcher only with TLS from the first byte;
  the browser tests with the verification off (16) and on (3); and the checks of the logs after each run. Criteria
  3, 4, 6, 7 and 8 of MIL-088, before the live site.
