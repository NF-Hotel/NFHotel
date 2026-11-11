# Price Suggestions

**English**

The back office shows price suggestions for the holidays and the weeks of the next two months. The market analysis service makes them from the history of the same days in earlier years; a Manager reads a suggestion, approves it with one click and can undo it. No price changes until a Manager approves it.

**For:** the Manager of the hotel (permission `Prices.Manage`). **Work:** MIL-055 (the approval, the undo and the settings), MIL-071 (the suggestions come from the service), user story US-001.12, use case UC-007.

**Contents**
1. [What the suggestions are](#what-the-suggestions-are)
2. [The rule in plain words](#the-rule-in-plain-words)
3. [The three pages](#the-three-pages)
4. [Approve a suggestion](#approve-a-suggestion)
5. [Revert a price change](#revert-a-price-change)
6. [What a booking pays](#what-a-booking-pays)
7. [Audit trail](#audit-trail)
8. [Who can do it](#who-can-do-it)
9. [The AI text](#the-ai-text)
10. [Known limits](#known-limits)

## What the suggestions are

A room size (single, double, suite) has a base rate: the price a night before any change. A **price suggestion**, made by the market analysis service, says: "from this first day to this last day, change the price of this room size by this percentage", and shows the old and the new nightly price and the figures it rests on. A **price change** is a suggestion that a Manager has approved. A price change is never deleted, only reverted, so the list shows what was in force.

## The rule in plain words

The rule is fixed and lives in the market analysis service, not in the back office: the same bookings and settings always give the same suggestions. An AI never sets a day, a percentage or a price.

1. **The horizon** is the next two months, from the day of the analysis.
2. **The history.** For the same days in each earlier year the service works out the occupancy: the room nights taken divided by the room nights the hotel has, over all earlier years together. It needs at least two earlier years; with fewer it makes no suggestion for those days and the report says so.
3. **Holidays.** A holiday in the next two months is a period of its own. The service looks at the same holiday in the earlier years (by its name, so a holiday that moves is still found).
4. **Weeks.** The other days are taken a week at a time. Days that belong to a holiday are not counted in a week.
5. **Busy:** the occupancy of the same days in the earlier years is at or above the high threshold (default 80 %). The suggestion is a surcharge (default 20 %) for every room size. The reason is `holiday` for a holiday and `high_history` for a week.
6. **Quiet:** the occupancy is at or below the low threshold (default 40 %). The suggestion is a discount (default 15 %) for the room sizes that qualify, which are single rooms by default. The reason is `low_history`; for a holiday with a low history the reason is also `low_history`.
7. **In between** gets no suggestion.

The website then applies two limits that the service never sees, because the service does not know the room rates:

- **Cap:** a change is never larger than the largest change of the settings (default 30 %). A suggestion that would be larger is cut to the cap and says so.
- **Minimum price:** a discounted price never goes below the minimum nightly price. A suggestion that would is cut to it and says so.

The new price is worked out from the base rate of the room size, so a room size without a rate gets no suggestion. The occupancy is that of the whole hotel; it is not split by room size.

## The three pages

All three are in the back office menu, for a Manager only.

### Room rates (`/admin/pricing/rates`)

The nightly base rate of each room size.

| Column | Meaning |
| --- | --- |
| Room size | Single, double or suite |
| Rate a night | The base rate now, or "not set" |
| Set by, Set at (UTC) | Who changed the rate last, and when |
| New rate | Type a number (a dot for decimals) and press Save |

### Pricing settings (`/admin/pricing/settings`)

The values of the rule. They are sent to the market analysis service when an analysis is run, so they apply to the next analysis; the cap and the minimum price are applied again when the page is shown. Percentages use a dot for decimals, for example 12.5.

| Field | Meaning | Default |
| --- | --- | --- |
| Surcharge in a high season (%) | Added to the base rate for busy days | 20 |
| Discount in a low season (%) | Taken from the base rate for quiet days | 15 |
| High season from occupancy (%) | At or above this the same days in earlier years count as busy | 80 |
| Low season up to occupancy (%) | At or below this the same days in earlier years count as quiet | 40 |
| Largest change of a price (%) | Cap of any change | 30 |
| Minimum nightly price | A price never goes below this | 0 |
| Room sizes that get a low season discount | Checked sizes get the discount | Single |

The low threshold must be below the high threshold, and the percentages must be from 0 to 100 (the largest change above 0). Saved settings apply to the next suggestions. They do not change a price change that is already approved.

### Price suggestions (`/admin/pricing/suggestions`)

| Part | Meaning |
| --- | --- |
| Open suggestions | One row per room size and period: Room size, Season (high for a surcharge, low for a discount), Reason, First day, Last day, Change (the percentage), Price a night (old to new), Basis |
| Reason | The name of the holiday, or "Busy on the same days in earlier years", or "Quiet on the same days in earlier years" |
| Basis | The average occupancy and the threshold it crossed, the occupancy of each earlier year, the explanation of the suggestion and a note when the cap or the minimum price cut it |
| Approve | The two date fields default to the first and last day of the suggestion; change them if you want a shorter period, then press Approve |
| Further ideas | Ideas without a price from the service, such as a package; they cannot be approved and change no price |
| Explanation | The AI text of the market analysis, labelled; it is not used for any price |
| Price changes | Every approved change, newest first: room size, change, first and last day, State (In force, Coming, Finished, Reverted or Withdrawn), who approved and when, who reverted and when, and a Revert button |
| Prices today | For each room size the base rate, the change in force today and the price today |

A suggestion is not listed for days that a price change of the same room size already covers.

**When there is nothing to approve.** The page says why. Without a completed report from the service (it may be stopped, or the last report is from before it made suggestions) it says that nothing can be approved and asks you to run a new analysis. With a report but no suggestion it says that no holiday or week in the next two months needs a change. The website never makes a suggestion of its own.

## Approve a suggestion

1. Open Price suggestions and read the row, especially the Reason and the Basis columns.
2. Change the first or last day if you do not want the whole run.
3. Press **Approve**. The page shows the new price change. The percentage is calculated again by the server, not taken from the browser.

A repeated click, or a second approval of the same suggestion, creates no second price change. A last day before the first day, or days that overlap a price change in force for the same room size, are refused with a message and nothing is created. Two approved changes for one room size never overlap.

## Revert a price change

Press **Revert** on a row of the Price changes table.

- A change **in force** (it has begun) is **ended**: its last day becomes the day before you revert, so from the revert day the room size is back at the base rate.
- A change that has **not begun** is **withdrawn**: it counts for no day at all.

Either way the row stays in the list with the state `reverted`, the name of the Manager and the time. A change that is already reverted, or whose period is over, cannot be reverted again.

## What a booking pays

A price change applies to **new bookings only**. A new booking gets the price of its room size on the **arrival date**: the base rate with the price change in force on that day. A booking made before keeps its price. If the room size has no base rate, the booking is made without a price, as before.

## Audit trail

Every approval, revert, change of a room rate and change of the settings is written to the audit trail with the staff member, the time and the values. Read it on the **Audit** page (`/admin/staff/audit`, menu entry Audit); the **Detail** column holds the room size, the period and the percentage, and for a revert whether the change `ended` on a date or was `withdrawn`. The audit actions are `PriceChangeApproved`, `PriceChangeReverted`, `PricingSettingsChanged` and `RoomRateChanged`.

## Who can do it

Only a **Manager**, through the permission `Prices.Manage`. A Receptionist and Housekeeping get no menu entry and the API answers 403; every call needs a sign-in. The changes use the Manager write limit: more than 30 changes in a minute are refused with "try again later".

## The AI text

The market analysis service asks an AI to explain each suggestion in a sentence or two, and to give up to three ideas without a price. The AI only explains: it is given the figures of the suggestion and may not use any other number, a currency, a cause or a promise. An answer that breaks this is asked for again once, and if it breaks it again the text is built from the figures instead.

Every text on the page is labelled: **Written by an AI**, or **Built from the figures**. A text never sets a day, a percentage or a price, and a test fixes that a text with a price in it changes no figure of the row.

## Known limits

- The occupancy is that of the whole hotel, not of each room size.
- A holiday or a week with fewer than two earlier years of history gets no suggestion; the service lists it, but the page shows only what can be approved.
- A new analysis is needed to get new suggestions or to apply changed settings; the page shows the latest completed report.
- Approvals are serialised by a lock inside one API instance. Run one API instance; with more, two approvals at the same moment could both pass the overlap check.
- A booking made in the hotel holds only its date and its price. In the market analysis such a row reads as "unknown" for guests, meal and the like, so it does not count as zero guests.
- That a price change does not touch bookings already made is the proposal in UC-007, for S01 to confirm.

Related: [Home](Home) and the use case UC-007 in `docs/use-cases/uc-007/uc.md`.
