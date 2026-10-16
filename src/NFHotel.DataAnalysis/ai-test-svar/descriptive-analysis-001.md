We need answer user's request. User says: "You work as a data analyst and in marketing to optimize hotel operations. We have extract descriptive analysis:
<descriptive stats table>"

They likely expect analysis/interpretation/insights and maybe recommendations. Need produce helpful response in English? User wrote English. Need maybe point out issues: is_canceled mean 0.2776 cancellation rate 27.8%. Lead time median 57 days, mean 93 high skew max 737. Arrival week number mean 27, min1 max53 maybe includes 53 for leap? Need interpret seasonality. day of month not seasonality. stays weekend nights median 1, week nights median 3, adults median 2, children low, babies very low, repeated guest only 4.4%, previous cancellations mean low but max 26, booking_changes mean low max17, agent count? agent is categorical encoded? Mean 173, std, min 0, median 240, max 535; but agent id not meaningful numeric, should not use descriptive stats as continuous. required car parking median 0, mean 0.138, total special requests median 1, mean 0.62. Need maybe mention missing values? none in count. Data is 40,060 reservations. Need likely provide business interpretation, potential modeling targets, marketing optimization. Need be careful with descriptive stats: categorical variables encoded numeric (agent) shouldn't be averaged. is_repeated_guest etc binary. Need maybe propose actions: cancellation risk segments by lead time, booking changes, previous cancellations, agent? Special requests? Weekend/week stays. Repeated guests low. Need maybe discuss target distribution: 27.8% canceled, 72.2% completed. Need calculate percentages from means: no-cancellation 72.24%. repeated guest 4.44%, parking 13.81%, special requests 61.98% have at least one? Since mean 0.62 but max 5, 75% =1, median 1, 25%=0, so at least 50% have >=1? Actually mean 0.62, median 1. Need not overstate. children 12.87% of rows? mean 0.128 but if binary? children can be 0,1,2; mean is average count, not percentage. Need note. adults mean 1.87 but min 0 max 55, likely data anomalies: adults min 0 and children min 0? Usually adults at least 1, but min 0 indicates data quality issue. Also stays_in_week_nights max 50 and weekend max 19, long stays maybe group bookings. babies mean low. Need mention possible outliers / data validation: adults=0, children=10, babies=2, stays 50, agent numeric encoding, previous_cancellations max 26, booking_changes max17. Need maybe compute simple insights: cancellation rate 27.8%; if target reduce by 5% absolute saves 5% of reservations ~2003 completed? Actually 5% of 40060 = 2003 reservations, not revenue. Need maybe propose KPIs: cancellation rate, no-show? not present. Need maybe use descriptive to identify segments: long lead time maybe higher/lower cancellations? Need not assume. But can suggest analyzing relationship. Need maybe mention agent as categorical: count distinct agents, use frequency not mean. Need maybe create a concise analyst report.

Need produce final answer with sections: snapshot, key interpretations, caveats/data quality, next steps, marketing/ops recommendations. Since user didn't ask specific question but implies we have descriptive analysis. We can frame as "Here's how to read it and what to do". Need maybe include table of variables and insights. Need be detailed.

Need ensure no hidden chain-of-thought in final. We can provide direct.

Potential final:
- Dataset: 40,060 reservation records, no missing values.
- Target: is_canceled: mean 0.2776 => 27.76% canceled; 72.24% stayed. Baseline.
- Lead time: median 57 days, mean 93, max 737, right-skewed. Most bookings 10-155 days. Use log transformation or bins; investigate very long lead times for group/corporate.
- Arrival week number: distribution 16-38 50%, suggests seasonal peaks/troughs; identify high-risk/high-value weeks.
- Day of month: not a strong seasonal signal but may interact with holidays.
- Stays: weekend nights median 1, mean 1.19; week nights median 3, mean 3.13. Typical 3-5 night stay. Long stays (50 week, 19 weekend) potential group/extended stay.
- Guests: adults median 2, mean 1.87; children low; babies rare. Most bookings couples/single adults. Data quality: adults min 0 and high 55 suspicious; children/babies counts need validation.
- Repeat guest: only 4.44% repeated, low loyalty; opportunity CRM/loyalty.
- Previous cancellations: median 0, mean 0.10, max 26; small high-risk chronic cancellers.
- Booking changes: median 0, mean .29, 75% <=1, max17; changes may signal risk; monitor.
- agent: numeric coding, don't treat as continuous; use as categorical; identify top agents by volume, cancellation rate, revenue.
- parking: 13.8% average? Actually mean 0.138, median 0; some require parking.
- special requests: mean .62, median1; many have at least one; useful for personalization and operational preparation.

Need maybe note: descriptive stats don't show causality or relationship to cancellation. Need next analyses:
1. Distribution of is_canceled by bins: lead_time, arrival week, adults, booking_changes, previous_cancellations, agent, parking, special requests.
2. Correlation / target encoding.
3. Segmentation: high risk, high value, corporate/group, leisure families.
4. Model: logistic regression/XGBoost to predict cancellation; feature engineering: total_nights, is_long_stay, guest_count, has_children, has_baby, special_request count, lead_time buckets, week number, agent performance, interaction.
5. A/B test policies: free cancellation deadlines, deposits, reminder, upsell, dynamic pricing.

Marketing recommendations:
- Reduce cancellations: target high-risk segments with retention offers, flexible dates, deposit, reminders 48h, alternative room.
- Acquire new guests: repeated low -> loyalty program, direct booking discount, post-stay email, repeat guest offer.
- Optimize agent/channel: analyze agents with high cancellation/volume; training/incentives; shift to direct channels.
- Personalize: special requests -> service touches; parking -> package; families -> child amenities.
- Seasonality: week number -> dynamic pricing/promos in low-demand weeks; weekend/week nights -> packages.
- Data governance: fix adults=0, long stays, agent id, children/babies definitions.

Need maybe include "immediate action plan" with 30/60/90.

Need maybe mention "is_canceled" target; if goal maximize occupancy/profit, need revenue per booking not present. Need ask for more? But we can answer.

Need maybe provide a small "executive summary".

Need ensure not to claim that special requests 62% have at least one. Since mean .62, median 1, 75th 1. At least 25% have 1? Actually 25%=0, 50%=1 means 50% <=1, 75% <=1. So maybe 50-75% have 1? Can't know. But mean 0.62 suggests many zeros and ones. Could say "a substantial share". Avoid exact.

Need maybe mention agent: min 0 and max 535, median 240. But if agent is categorical ID, descriptive stats meaningless; count of unique agents? We don't have nunique. Could say need nunique and top categories.

Need maybe note missing values: counts equal 40060 for all, no missing. But not necessarily no NaN? descriptive count excludes NaN; if all counts equal, no missing. Good.

Need maybe calculate cancellation rate: 0.277634*100 = 27.7634%. No cancellation 72.2366%.

Need maybe compute percentage of repeated guest: 4.4383%. previous cancellations nonzero? Mean .1017, but max; can't know percent nonzero but if median 0, 75% 0. Could estimate at least 25% have 1? Actually 75%=0 means 75% have 0 or less? Since nonnegative, 75th percentile 0 means at least 75% are 0. So >25% have previous_cancellations >0. Similarly booking_changes 75%=0? Yes 75% 0, mean .288. Special requests 25%=0, 50%=1, so at least 50% have 0? Actually 25%=0 means 25% <=0 => 25% are 0; 50%=1 means 50% <=1. So 50% have 0 or 1; can't exact. Parking 75%=0, so at least 75% no parking. Children 75%=0, at least 75% no children. Babies 75%=0. Repeated guest 75%=0, 25%=1? Actually 75%=0 means 75% are 0; 25% are 1? Since binary, yes if 75th percentile 0 and max1, at least 75% 0. Mean .044. Good.

Need maybe mention "binary variables mean = percentage".

Could include a "risk flags" table:
- High-risk signals: previous_cancellations>0, booking_changes>1, long lead time? maybe.
- Low-risk/loyalty: is_repeated_guest? but low.

Need maybe mention "min adults=0" and "adults max 55" indicates data errors or group bookings. Need validate.

Need maybe provide sample SQL/Python? Could be useful. But user likely wants interpretation. Could include next analysis code? Maybe not necessary. But desired oververbosity 9, so detailed. Could include "What this table tells us", "What it does not tell us", "Recommended next analyses", "Marketing/operations actions", "KPIs".

Need maybe final answer in a professional tone.

Let's craft final:
Start: "This is a reservation-level dataset of 40,060 bookings with no missing values. The target is is_canceled. Overall cancellation baseline 27.8%." Then sections.

Maybe include table:
Variable | What it means | Business takeaway
Rows:
- is_canceled: 27.8% canceled, 72.2 completed; target.
- lead_time: median 57, mean 93, max 737; right skewed; most guests book 1-5 months ahead; long lead may be group/corporate.
- arrival_date_week_number: median 28; half arrivals weeks 16-38; seasonal; build week-level occupancy/cancellation dashboards.
- arrival_date_day_of_month: less actionable; check holidays.
- stays: typical 3-5 nights; long stays outliers; segment.
- guests: mostly 1-2 adults; families rare; data quality.
- is_repeated_guest: 4.4% low; loyalty opportunity.
- previous_cancellations: mostly 0, but tail high; high-risk list.
- booking_changes: mostly 0, tail; changes as risk/engagement signal.
- agent: categorical; need unique count and performance.
- parking: low but meaningful; package.
- special requests: common; service/ops.

Then "Caveats" bullet: agent numeric not continuous; means of counts not percentages; outliers; need revenue, room type, channel, meal plan, deposit, no-show, profit. Need target modeling.

Then "Next steps" detailed:
1. Validate data: adults>=1, children/babies logic, stays >0, agent mapping, date ranges, leap week 53.
2. Build cancellation risk score: features, encoding, evaluate.
3. Segment guests: new/returning, lead time, party size, special requests, parking, agent, season.
4. Marketing campaigns: pre-arrival, reminders, retention, loyalty, direct booking, family packages, group sales.
5. Operations: housekeeping, staffing based on special requests, parking, weekend vs week.
6. KPIs: cancellation rate, no-show rate, occupancy, RevPAR, repeat guest rate, booking changes, agent performance.

Need maybe include "potential experiments":
- deposit for high-risk long lead bookings
- flexible date alternative
- post-stay loyalty offer for non-repeats
- targeted agent training/incentives
- dynamic pricing in low-demand weeks.

Need maybe include "If I had to prioritize three actions":
- analyze cancellation by agent, lead time, previous cancellations/booking changes
- build high-risk cancellation model and pre-cancellation retention
- launch direct/loyalty program to increase repeat 4.4% to higher.

Need maybe mention "marketing optimization" specific: use special requests to personalize email; use arrival week to plan promotions; use parking to bundle; use children to family packages.

Need maybe include a mini "segmentation matrix" high value? No revenue so can't high value. Could say proxy by nights/parking/special requests.

Need maybe mention "is_canceled mean is baseline; if we reduce by 2pp, ~801 fewer canceled reservations (40060*0.02 = 801.2); if 5pp, 2,003." Nice.

Need ensure final is not too long but thorough.

Let's draft.
__LM_STUDIO_INTERNAL_LSEP_SYNTHETIC_REASONING_END_f4e9a8d2c6b14d0c9e5f3a7b8c1d2e6a__

## Executive read

You have **40,060 reservation records**, with **no missing values** visible in the summary. The target variable is `is_canceled`, and its mean is:

```text
is_canceled mean = 0.277634
```

So the baseline cancellation rate is:

```text
27.76% canceled
72.24% not canceled
```

That is a meaningful cancellation baseline. If you can reduce cancellations by even **2 percentage points**, that would affect approximately:

```text
40,060 × 0.02 = ~801 reservations
```

A **5 percentage point** improvement would affect approximately:

```text
40,060 × 0.05 = ~2,003 reservations
```

So the first priority should be understanding which guests, booking patterns, agents, dates, or booking behaviors are most associated with cancellations.

---

# 1. Key variable interpretation

## 1. `is_canceled`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 0.2776 | 27.76% of reservations were canceled |
| Max | 1.0 | Binary outcome: canceled or not |
| Min | 0.0 | Binary outcome |

### Business implication

This is your target variable. Your main modeling and marketing goal should be to predict which reservations are more likely to be canceled and then take action before cancellation occurs.

Possible actions:

- Retention offers
- Deposit requirements
- Alternative dates or room offers
- Pre-arrival confirmation reminders
- Customer support outreach for high-risk bookings
- Dynamic pricing for low-risk, high-intent bookings

---

## 2. `lead_time`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 92.68 days | Average booking was made about 3 months before arrival |
| Median | 57 days | Typical booking was made about 2 months before arrival |
| 25% | 10 days | 25% of bookings were made within the last 10 days |
| 75% | 155 days | 75% of bookings were made within 155 days |
| Max | 737 days | Some bookings were made about 2 years in advance |

### Business implication

Most guests book moderately in advance, but the distribution is right-skewed because of very long lead times.

A typical guest books around **57 days before arrival**, but some guests book very far in advance, possibly for:

- Corporate bookings
- Group bookings
- Event stays
- Wedding or conference reservations
- Extended stays
- Early seasonal bookings

### Recommended analysis

Segment `lead_time` into buckets, for example:

```text
0–14 days
15–30 days
31–60 days
61–90 days
91–180 days
181–365 days
365+ days
```

Then analyze cancellation rate by lead-time bucket.

Long-lead bookings may be valuable but may also be more cancellation-prone, especially if the stay is far in the future. You may want to monitor those with:

- Flexible cancellation
- No deposit
- Multiple booking changes
- High booking value
- Corporate or group profile

---

## 3. `arrival_date_week_number`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 27.14 | Average arrival week is around late summer, depending on year |
| Median | 28 | Typical arrival is in week 28 |
| 25% | 16 | Lower quartile around spring |
| 75% | 38 | Upper quartile around late summer/early fall |
| Max | 53 | Some arrivals are in week 53 |

### Business implication

This variable captures seasonality. Half of the reservations arrive between weeks **16 and 38**, which suggests a strong seasonal demand pattern.

This is important for:

- Pricing
- Promotions
- Staffing
- Housekeeping planning
- Channel mix
- Cancellation risk
- Occupancy forecasting

### Recommended analysis

Analyze:

```text
Cancellation rate by arrival week
Occupancy by arrival week
Average nights by arrival week
Average guests by arrival week
Special requests by arrival week
```

Then identify:

- Peak weeks
- Low-demand weeks
- High-cancellation weeks
- Seasonal guest segments

You can then design seasonal campaigns:

- Shoulder-season discount
- Last-minute deal
- Midweek promotion
- Extended-stay package
- Corporate off-peak offer

---

## 4. `arrival_date_day_of_month`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 15.82 | Arrivals are spread across the month |
| Median | 16 | Typical arrival is mid-month |
| Range | 1–31 | Full monthly coverage |

### Business implication

This variable is less useful than week number for seasonality, but it may still be useful for:

- Monthly cash flow
- Billing cycles
- Pay-day promotions
- Corporate travel patterns
- School holidays
- Religious or local holidays

### Recommended analysis

Check whether specific days of the month have higher demand or cancellation rates.

Examples:

```text
Days 1–5
Days 6–10
Days 11–15
Days 16–20
Days 21–25
Days 26–end
```

Also combine this with:

- Arrival week number
- Year
- Holiday flags
- Day of week

---

## 5. `stays_in_weekend_nights`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 1.19 | Average weekend nights per stay |
| Median | 1 | Most stays include 1 weekend night |
| 25% | 0 | 25% of stays have no weekend nights |
| 75% | 2 | 75% of stays have 2 or fewer weekend nights |
| Max | 19 | Some very long stays include many weekend nights |

### Business implication

Most bookings include one weekend night. This suggests many leisure guests book weekend or near-weekend stays.

Long stays with many weekend nights may indicate:

- Extended stays
- Group stays
- Corporate housing
- Event stays
- Monthly reservations

### Recommended analysis

Segment by total nights:

```text
1 night
2 nights
3 nights
4 nights
5–7 nights
8–14 nights
15+ nights
```

Then analyze:

- Cancellation rate
- Booking changes
- Lead time
- Guest count
- Special requests
- Parking demand
- Agent source

Long stays may be high-value but may also require special approval or sales follow-up.

---

## 6. `stays_in_week_nights`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 3.13 | Average week nights per stay |
| Median | 3 | Typical stay includes 3 week nights |
| 25% | 1 | 25% of stays have 1 or fewer week nights |
| 75% | 5 | 75% of stays have 5 or fewer week nights |
| Max | 50 | Some very long business/group stays |

### Business implication

The hotel has a meaningful mix of leisure and business/extended stays.

A typical stay may be around:

```text
3 week nights + 1 weekend night = 4 nights
```

But some stays are much longer.

### Marketing angle

You can build packages around:

- 2-night weekend break
- 3-night weekend package
- 4-night leisure stay
- 7-night extended stay
- Corporate monthly stay
- Midweek business package

### Operational angle

Long week-night stays may require:

- Housekeeping every 2 or 3 days
- Linen replacement schedule
- Laundry service
- Extended-stay pricing
- Corporate rate approval
- Parking allocation
- Special request fulfillment

---

## 7. `adults`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 1.87 | Average adults per reservation |
| Median | 2 | Typical reservation has 2 adults |
| 25% | 2 | Most bookings have 2 adults |
| 75% | 2 | Most bookings have 2 adults |
| Min | 0 | Potentially invalid or special booking |
| Max | 55 | Large group or data quality issue |

### Business implication

The majority of reservations are for couples or two adults. This is a strong signal for:

- Couple packages
- Honeymoon packages
- Business traveler rooms
- Two-person pricing
- Double room inventory
- Romantic or business stay offers

However, the minimum value is **0 adults**, which is suspicious for a normal hotel reservation.

### Data quality check

You should investigate:

```text
adults = 0
adults > 10
adults + children + babies = 0
adults + children + babies > 20
```

Possible causes:

- Data entry error
- Group booking recorded differently
- Corporate booking with guest count missing
- Special event booking
- Test booking
- Cancellation before guest count was entered

---

## 8. `children`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 0.1287 | Average children per reservation is low |
| Median | 0 | Most reservations have no children |
| 25% | 0 | Most have no children |
| 75% | 0 | Most have no children |
| Max | 10 | Some large family/group bookings |

### Business implication

Family travel exists but is not the dominant segment.

You should identify reservations with:

```text
children > 0
```

and analyze whether they have:

- Higher cancellation rate
- More special requests
- More parking needs
- Longer lead time
- More booking changes
- Different room type preference

### Marketing angle

Create family packages:

- Family room
- Breakfast for kids
- Child-friendly amenities
- Babysitting service
- Board game or entertainment
- Parking discount
- Late checkout
- Kids’ welcome gift

---

## 9. `babies`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 0.0139 | Babies are very rare |
| Median | 0 | Most reservations have no babies |
| Max | 2 | Some bookings include up to 2 babies |

### Business implication

This is a small but valuable service segment.

If you have families with babies, you can improve guest experience by offering:

- Crib setup
- Baby bed
- Bottle warmer
- Baby bath
- Quiet room location
- Early check-in
- Extra towels or laundry
- Parent-friendly amenities

### Recommended analysis

Check:

```text
babies > 0
```

Then see if these bookings have:

- More special requests
- More parking
- Higher cancellation
- Longer stay
- Higher guest satisfaction

Even if small, this segment may be high-value if it improves reviews and repeat visits.

---

## 10. `is_repeated_guest`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 0.0444 | Only 4.44% are repeat guests |
| Median | 0 | Most guests are not repeat guests |
| 75% | 0 | Most guests are not repeat guests |

### Business implication

This is one of the most important insights.

Your hotel currently depends heavily on new or one-time guests. Only **4.44%** of reservations are from repeat guests.

That means there is a major opportunity to improve:

- Loyalty program
- Direct booking incentive
- Post-stay email marketing
- Personalized offers
- Guest relationship management
- Review response and service recovery

### Marketing recommendations

Create a repeat-guest strategy:

1. **Post-stay email**
   - Thank you message
   - Offer discount for next stay
   - Ask for review
   - Invite to loyalty program

2. **Loyalty program**
   - Points for stay
   - Free night after certain number of stays
   - Member-only rates
   - Birthday offer
   - Upgrade eligibility

3. **Direct booking advantage**
   - Best rate guarantee
   - Late checkout
   - Free parking
   - Welcome drink
   - Room upgrade if available

4. **Targeted retention**
   - Offer to guests who stayed 6 months ago
   - Offer to guests who had a special request
   - Offer to high-spending or long-stay guests
   - Offer to guests from good-performing agents or channels

### KPI to improve

Increase repeat-guest rate from:

```text
4.44%
```

to a target such as:

```text
8%
10%
15%
```

Even a small increase can improve occupancy and reduce reliance on third-party commission.

---

## 11. `previous_cancellations`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 0.1017 | Low average, but tail is high |
| Median | 0 | Most guests have no previous cancellations |
| 75% | 0 | Most guests have no previous cancellations |
| Max | 26 | Some guests have canceled many times |

### Business implication

Most guests have not canceled before, but there is a small group of high-risk guests.

A guest with:

```text
previous_cancellations > 0
```

may be higher-risk, especially if they also have:

```text
booking_changes > 0
lead_time > 90 days
is_repeated_guest = 0
agent = high-cancellation agent
special_requests = many
```

### Recommended analysis

Analyze cancellation risk by previous cancellation count:

```text
0
1
2
3–5
6–10
10+
```

Then calculate:

```text
Share of reservations
Cancellation rate
Average lead time
Average booking changes
Average revenue or nights
```

### Operational action

Create a high-risk cancellation list.

Possible actions:

- Require deposit
- Send confirmation reminder
- Offer flexible dates instead of cancellation
- Assign to priority sales team
- Flag for follow-up before check-in
- Exclude from free-cancellation promotion

---

## 12. `booking_changes`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 0.288 | Most bookings have no changes |
| Median | 0 | Most bookings are unchanged |
| 75% | 0 | Most bookings have no changes |
| Max | 17 | Some bookings were changed many times |

### Business implication

Booking changes are a strong behavioral signal.

A guest who changes dates, room, guests, or request may be:

- Planning to cancel
- Comparing alternatives
- Unsatisfied with the booking
- Facing budget or schedule uncertainty
- Booking for a group with changing requirements
- Using the hotel as a placeholder

### Recommended analysis

Analyze:

```text
booking_changes = 0
booking_changes = 1
booking_changes = 2
booking_changes >= 3
```

Calculate cancellation rate for each group.

You should expect the risk to increase with more changes.

### Marketing/ops action

When a booking change occurs, trigger:

- Confirmation email
- Customer care message
- Upsell or room upgrade
- Flexible date alternative
- Loyalty offer
- Deposit reminder
- Pre-arrival question: “Is there anything we can adjust to make your stay easier?”

This can reduce cancellations and improve guest confidence.

---

## 13. `agent`

| Statistic | Value | Interpretation |
|---|---:|---|
| Mean | 172.99 | Not meaningful if agent is categorical |
| Median | 240 | Not meaningful if agent is categorical |
| Max | 535 | Likely an encoded agent ID |

### Important caveat

`agent` is likely a **categorical identifier**, not a continuous numeric variable.

The mean, standard deviation, min, and max of `agent` are not very meaningful for business analysis.

Instead, you should treat `agent` as a category and analyze:

```text
Number of unique agents
Reservations per agent
Cancellation rate by agent
Average lead time by agent
Average nights by agent
Average guest count by agent
Average special requests by agent
Average booking changes by agent
Revenue by agent, if available
```

### Business implication

Some agents may be high-volume, low-cancellation, high-value. Others may be high-cancellation or low-quality leads.

### Recommended analysis

Create an agent performance table:

| Metric | Why it matters |
|---|---|
| Total reservations | Volume |
| Cancellation count | Risk |
| Cancellation rate | Quality |
| Average lead time | Planning horizon |
| Average nights | Stay value |
| Average booking changes | Guest uncertainty |
| Average special requests | Service complexity |
| Average parking demand | Operational need |
| Revenue per reservation | Profitability, if available |
| Repeat guest share | Loyalty |

Then segment agents into:

1. **High value, low cancellation**
   - Protect and grow
   - Offer better terms or exclusive rates

2. **High volume, high cancellation**
   - Investigate
   - Require deposit
   - Improve confirmation flow
   - Retrain or renegotiate

3. **Low volume, high cancellation**
   - Monitor
