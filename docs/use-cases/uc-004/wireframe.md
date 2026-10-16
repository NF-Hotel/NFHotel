# Wireframes of UC-004

Kept with the use case, as the `clean-architecture` skill asks. They show the screens of [UC-004]; the use case itself names none.

The sign-in page, one column, centred, in the style of the public pages:

```text
+--------------------------------------------------+
| NFHotel & Bar                    [Book your stay]|
+--------------------------------------------------+
|                                                  |
|   Staff sign-in                                  |
|   The back office is for the hotel's staff.      |
|   Sign in with your own account.                 |
|                                                  |
|   +------------------------------------------+   |
|   | (error text, only after a refusal)       |   |
|   |                                          |   |
|   | User name *                              |   |
|   | [______________________________]         |   |
|   |                                          |   |
|   | Password *                               |   |
|   | [______________________________]         |   |
|   |                                          |   |
|   | [ Sign in ]                              |   |
|   +------------------------------------------+   |
|                                                  |
+--------------------------------------------------+
```

The side menu of the back office, after the sign-in, for a Receptionist (a Housekeeping member sees only Rooms):

```text
+----------------+
| NFHotel        |
| Administration |
| Dashboard      |
| Bookings       |
| Rooms          |
| Guests         |
|                |
| rita           |
| Receptionist   |
| [ Sign out ]   |
+----------------+
```


[UC-004]: ./uc.md
