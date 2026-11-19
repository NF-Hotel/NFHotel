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
