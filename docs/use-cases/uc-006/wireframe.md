# Wireframes of UC-006

Kept with the use case. They show the screens of [UC-006]; the use case itself names none.

The staff page (Manager only):

```text
+-------------------------------------------------------------+
| Staff                                      [ New account ]  |
| User name   Role          State                  Changed    |
| manager     Manager       Active                 2026-10-20 |
| rita        Receptionist  Active                 2026-10-20 |
|                           [ Reset password ] [ Disable ]    |
| hans        Housekeeping  Disabled                          |
|                           [ Reset password ] [ Enable ]     |
| lena        Receptionist  Must change password              |
+-------------------------------------------------------------+
```

The form "New account" and "Reset password" (a panel under the row):

```text
| User name *  [____________]   Role * [ Receptionist v ]     |
| Temporary password * [______________]  (12+ characters)     |
| [ Create ]  [ Cancel ]                                       |
```

The audit page (Manager only), newest first, with Previous and Next:

```text
| Time (UTC)           By       Action          To      Result|
| 2026-10-20 12:01:10  manager  PasswordChanged manager Success|
| 2026-10-20 11:58:02  (none)   SignInFailed    manager InvalidCredentials |
```

The side menu shows "Staff" and "Audit" to a Manager only.

[UC-006]: ./uc.md
