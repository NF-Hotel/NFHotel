# Price Suggestions

**English**

The back office can suggest a change of the room prices for the busy and the quiet months, from the market analysis. A Manager reads the suggestion, approves it with one click and can undo it. No price changes until a Manager approves it.

**For:** the Manager of the hotel (permission `Prices.Manage`). **Work:** MIL-051, user story US-001.12, use case UC-007.

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

A room size (single, double, suite) has a base rate: the price a night before any change. A **price suggestion** says: "from this first day to this last day, change the price of this room size by this percentage", and shows the old and the new nightly price and the figures it rests on. A **price change** is a suggestion that a Manager has approved. A price change is never deleted, only reverted, so the list shows what was in force.

## The rule in plain words

The rule is fixed and has no AI in it: the same figures always give the same suggestions.

1. **Season of a month.** The rule reads the occupancy of each calendar month from the latest completed market analysis report (the average over the years of the report).
   - **High season:** the occupancy is at or above the high threshold (default 80 %).
   - **Low season:** the occupancy is at or below the low threshold (default 40 %).
   - **Normal:** anything in between, and a month without a figure. Normal months get no suggestion.
2. **High season:** a surcharge of 20 % for every room size.
3. **Low season:** a discount of 15 % for the room sizes that qualify, which are single rooms by default. Double rooms and suites get no suggestion in low season unless you check them in the settings.
4. **Cap:** a change is never larger than 30 % (the largest change). A suggestion that would be larger is cut to the cap and says so in the Basis column.
5. **Minimum price:** a new price never goes below the minimum nightly price. A suggestion that would is cut to it and says so.
6. **Horizon:** the suggestions cover the next twelve months, starting with the month after today. A run of neighbouring months with the same season becomes one suggestion, from the first to the last day of the run.
7. **Why twelve calendar months:** the season of, say, July is read from July. If the report has no occupancy for one of the twelve calendar months, the rule cannot tell the seasons apart, so it makes no suggestions and the page says there is no basis. Run a market analysis over a period that covers all twelve calendar months first (at least twelve months).

Suggestions also need a base rate for the room size. A room size without a rate gets no suggestion.

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

The values of the rule. Percentages use a dot for decimals, for example 12.5.

| Field | Meaning | Default |
| --- | --- | --- |
| Surcharge in a high season (%) | Added to the base rate in a high season | 20 |
| Discount in a low season (%) | Taken from the base rate in a low season | 15 |
| High season from occupancy (%) | At or above this a month is high season | 80 |
| Low season up to occupancy (%) | At or below this a month is low season | 40 |
| Largest change of a price (%) | Cap of any change | 30 |
| Minimum nightly price | A price never goes below this | 0 |
| Room sizes that get a low season discount | Checked sizes get the discount | Single |

The low threshold must be below the high threshold, and the percentages must be from 0 to 100 (the largest change above 0). Saved settings apply to the next suggestions. They do not change a price change that is already approved.

### Price suggestions (`/admin/pricing/suggestions`)

| Part | Meaning |
| --- | --- |
| Open suggestions | One row per room size and season run: Room size, Season, First day, Last day, Change (the percentage), Price a night (old to new), Basis (the occupancy of the month and the threshold it crossed, and a note when the cap or the minimum price cut it) |
| Approve | The two date fields default to the first and last day of the run; change them if you want a shorter period, then press Approve |
| Explanation | The AI text of the market analysis, labelled; it is not used for any price |
| Price changes | Every approved change, newest first: room size, change, first and last day, State (approved or reverted), who approved and when, who reverted and when, and a Revert button |
| Prices today | For each room size the base rate, the change in force today and the price today |

A suggestion is not listed for days that a price change of the same room size already covers.

## Approve a suggestion

1. Open Price suggestions and read the row, especially the Basis column.
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

When the market analysis report has an AI insight, the Price suggestions page shows it beside the list, labelled as an explanation. It only explains. It never sets a season, a percentage or a price, and a test fixes that a changed AI text gives the same suggestions.

## Known limits

- In low season there is no suggestion for double rooms and suites unless you check them in the settings; a later story may add rules for them.
- Approvals are serialised by a lock inside one API instance. Run one API instance; with more, two approvals at the same moment could both pass the overlap check.
- A booking made in the hotel holds only its date and its price. In the market analysis such a row reads as "unknown" for guests, meal and the like, so it does not count as zero guests.
- The suggestions need a completed market analysis report that covers all twelve calendar months, and a base rate for each room size.
- That a price change does not touch bookings already made is the proposal in UC-007, for S01 to confirm.

Related: [Home](Home) and the use case UC-007 in `docs/use-cases/uc-007/uc.md`.
