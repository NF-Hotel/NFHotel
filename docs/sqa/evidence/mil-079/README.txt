The public address is in the variable HOTEL and in none of the files; the email of the test booking is in none of
them either.

Before the fix, on the live site and on the code of main, 2026-11-12:
- before-FLZ-000001.png and before-FLZ-999999.png: headless Chrome on the host, own throwaway profile, window
  1280x1100, no address bar; taken 10:47:20Z.
- before-code.txt and before-receipt.txt: taken 10:46:51Z. before-count.txt: taken 10:48:21Z.
- before-tests.txt: the new API tests on the code of main; 10 of 17 fail.

After the fix, on the branch before the deploy: after-tests.txt (the command of before-tests.txt; 17 of 17 pass) and
after-tests-all.txt (every suite that runs without a database or a browser).

After the deploy of CD run 43256465429 (ended 16:21:58Z), on the live site (MIL-079 task 5):
- after-receipt.txt and after-count.txt: the commands of before-receipt.txt and before-count.txt again, with the
  fields of the form that the page shows instead; taken 16:22:13Z.
- after-FLZ-000001.png and after-FLZ-999999.png: the lookup page without an email; headless Chromium of the
  Playwright image in Docker, window 1280x1100, no address bar; taken 16:22:45Z.
- after-test-booking-receipt-step.png, after-test-booking-other-email.png and after-test-booking-own-email.png: the
  test booking FLZ-158424 on the receipt step of the booking page, then looked up with another email and with its
  own; same browser; taken by 16:28:10Z.
- after-live-check.txt: what the browser script printed in both runs.
