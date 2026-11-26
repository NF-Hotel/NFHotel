// Load test of NF Hotel against the throwaway stack (MIL-036 task 3, MIL-089 task 4), run by
// ops/scripts/load-test.sh, never against production. Twenty virtual guests open the public
// pages through nginx for two minutes, about ten times what the hotel sees at its busiest, and
// search for free rooms and book as the web application does: on the API with its service
// token and the guest's address in X-NFHotel-Client-Address, a new address for each visit, so
// every guest stays within the limits of the booking flow (ADR-0051). The thresholds are the
// objectives of MIL-034. At the same time a burst from one address asks for more than the
// limits allow; it must get only the refusals of the limits (429), never an answer of the 500
// kind (MIL-089 decision 5). The limits are those of production: the stack sets none of them.
import http from "k6/http";
import exec from "k6/execution";
import { check, sleep } from "k6";
import { Counter } from "k6/metrics";

// The addresses of nginx and of the API in the stack come from load-test.sh, the one place
// that names them; without them the test stops before it sends anything.
const target = __ENV.TARGET;
const api = __ENV.API;
if (!target || !api) {
  throw new Error("TARGET and API must be set, as ops/scripts/load-test.sh sets them");
}
// The token of the API and the Manager account of the stack, from a file that load-test.sh
// mounts read-only; never from the environment or a command line.
const secrets = JSON.parse(open("/secrets/load.json"));

// A search for free rooms per visit, and a booking in one visit of four, as most guests look
// and few book. The nights of the bookings never overlap, so no booking finds its rooms taken.
const bookEvery = 4;
const rooms = 6;
// The nights, in days from today: each guest of the test books in 60 nights of its own from
// day 30, and the burst from day 1500, where no guest books. Each visit searches two nights
// from day 40.
const firstNightOfTheGuests = 30;
const nightsPerGuest = 60;
const firstNightOfTheBurst = 1500;
const searchFrom = 40;
const searchNights = 2;
const millisecondsPerDay = 24 * 60 * 60 * 1000;
// The limits of production per address (BusinessRules.md, "Limits of the booking flow"): each
// window is fixed and starts at the first request of the address, so a burst of one address
// that ends within the minute gets exactly these numbers of answers and the rest refused.
const searchLimit = 60; // searches for free rooms a minute
const bookingLimit = 5; // booking requests an hour
const searchesInBurst = 80;
const bookingsInBurst = 8;
const burstAnswers = searchLimit + bookingLimit;
const burstRefusals = searchesInBurst - searchLimit + (bookingsInBurst - bookingLimit);

const burstAnswered = new Counter("burst_answered");
const burstRefused = new Counter("burst_refused");
const burstServerErrors = new Counter("burst_server_errors");
const burstUnexpected = new Counter("burst_unexpected");

export const options = {
  insecureSkipTLSVerify: true, // the stack has a self-signed certificate
  scenarios: {
    guests: {
      executor: "ramping-vus",
      exec: "guests",
      startVUs: 1,
      stages: [
        { duration: "30s", target: 20 },
        { duration: "1m", target: 20 },
        { duration: "30s", target: 0 },
      ],
    },
    burst: {
      executor: "per-vu-iterations",
      exec: "burst",
      vus: 1,
      iterations: 1,
      startTime: "45s",
      maxDuration: "1m",
    },
  },
  thresholds: {
    // MIL-034: 95 % of the requests of the guests within 1 second, fewer than 5 % failed.
    "http_req_duration{scenario:guests}": ["p(95)<1000"],
    "http_req_failed{scenario:guests}": ["rate<0.05"],
    checks: ["rate>0.95"],
    // MIL-089: the burst gets an answer up to each limit and a refusal of the limit over it,
    // never an answer of the 500 kind or another refusal; the stack has the limits of production.
    burst_answered: [`count==${burstAnswers}`],
    burst_refused: [`count==${burstRefusals}`],
    burst_server_errors: ["count==0"],
    burst_unexpected: ["count==0"],
  },
};

function headers(address) {
  const result = {
    Authorization: `Bearer ${secrets.apiToken}`,
    "Content-Type": "application/json",
  };
  if (address) {
    result["X-NFHotel-Client-Address"] = address;
  }
  return result;
}

// A date in the form of the API, a number of days from today.
function day(offset) {
  return new Date(Date.now() + offset * millisecondsPerDay).toISOString().slice(0, 10);
}

// A guest of the test: the names, the phone and the country pass the rules of ADR-0050; the
// email is new for each booking, so the limit per mailbox never refuses one.
function guest(email) {
  return {
    firstName: "Load",
    lastName: "Guest",
    email,
    phoneNumber: "+4512345678",
    country: "Denmark",
    passportNumber: null,
  };
}

function booking(nights, email) {
  return JSON.stringify({
    checkInDate: day(nights),
    checkOutDate: day(nights + 1),
    roomSize: 2,
    guests: 1,
    newGuest: guest(email),
  });
}

// The rooms of the test, made by the Manager account of the stack, and the mode of the public
// booking: at once, or asked for and verified by email (MIL-088).
export function setup() {
  const login = http.post(
    `${api}/api/auth/login`,
    JSON.stringify({ userName: secrets.staffUser, password: secrets.staffPassword }),
    { headers: headers() },
  );
  if (login.status !== 200) {
    exec.test.abort(`the Manager of the stack cannot sign in: ${login.status}`);
  }
  const staff = { ...headers(), "X-Staff-Token": login.json("value.token") };
  // Up to four hex digits that tell this run apart: in the rooms, the emails and the burst
  // address. They come from WebCrypto rather than Math.random, which a scan flags as a weak
  // generator; nothing secret rests on them.
  const [high, low] = crypto.getRandomValues(new Uint8Array(2));
  const run = ((high << 8) | low).toString(16);
  for (let i = 0; i < rooms; i++) {
    const room = http.post(
      `${api}/api/rooms`,
      JSON.stringify({ roomNumber: `L${run}${i}`, floor: 9, size: 2, capacity: 2 }),
      { headers: staff },
    );
    if (room.status !== 201 && room.status !== 200) {
      exec.test.abort(`a room of the test was not made: ${room.status}`);
    }
  }
  const mode = http.get(`${api}/api/bookings/public/mode`, { headers: headers() });
  if (mode.status !== 200) {
    exec.test.abort(`the mode of the public booking cannot be read: ${mode.status}`);
  }
  return { run, verification: mode.json("value.verificationRequired") === true };
}

// The search for free rooms of a visit, as the booking page asks the API for it.
function search(address, params) {
  const nights = `checkInDate=${day(searchFrom)}&checkOutDate=${day(searchFrom + searchNights)}`;
  return http.get(`${api}/api/bookings/available-rooms?${nights}`, {
    headers: headers(address),
    ...params,
  });
}

function book(data, address, nights, email, params) {
  const route = data.verification ? "api/bookings/public/requests" : "api/bookings/public";
  return http.post(`${api}/${route}`, booking(nights, email), {
    headers: headers(address),
    ...params,
  });
}

export function guests(data) {
  const vu = exec.vu.idInTest;
  const visit = exec.vu.iterationInScenario;
  // A new address for each visit: an IPv6 address of the documentation range in a /64 of its
  // own, because the limits count an IPv6 address by its first 64 bits.
  const address = `2001:db8:${vu.toString(16)}:${visit.toString(16)}::1`;

  for (const page of ["/", "/book", "/admin/login"]) {
    const response = http.get(`${target}${page}`, { tags: { page } });
    check(response, { [`${page} answers 200`]: (r) => r.status === 200 });
  }
  sleep(1);

  const found = search(address, { tags: { name: "search" } });
  check(found, { "the search answers 200": (r) => r.status === 200 });
  sleep(1);

  if (visit % bookEvery === bookEvery - 1) {
    // One night per booking, on a night that no other booking of the test has.
    const own = firstNightOfTheGuests + (vu - 1) * nightsPerGuest;
    const nights = own + Math.floor(visit / bookEvery);
    const email = `load-${data.run}-${vu}-${visit}@example.com`;
    const response = book(data, address, nights, email, { tags: { name: "booking" } });
    // 202 for a request that waits for its verification by email, 200 for a booking made at
    // once; the name of the check says in the summary which of the two modes ran.
    const expected = data.verification ? 202 : 200;
    check(response, { [`the booking answers ${expected}`]: (r) => r.status === expected });
    sleep(1);
  }
}

// A refusal of a limit is a 429 with the code of the limits (ADR-0051); a 429 without it counts
// as unexpected, and so does any other answer that is neither a success nor of the 500 kind.
function count(response) {
  const refused =
    response.status === 429 && response.json("errors.0") === "booking.too_many_requests";
  if (response.status === 200 || response.status === 202) {
    burstAnswered.add(1);
  } else if (refused) {
    burstRefused.add(1);
  } else if (response.status === 0 || response.status >= 500) {
    burstServerErrors.add(1);
  } else {
    burstUnexpected.add(1);
  }
  check(response, {
    "the burst gets an answer or a refusal of a limit": (r) =>
      r.status === 200 || r.status === 202 || refused,
  });
}

export function burst(data) {
  // One address for the whole burst, in a /64 that no guest uses.
  const address = `2001:db8:ffff:${data.run}::1`;
  const accepted = { responseCallback: http.expectedStatuses(200, 202, 429) };
  for (let i = 0; i < searchesInBurst; i++) {
    count(search(address, { tags: { name: "burst search" }, ...accepted }));
  }
  for (let i = 0; i < bookingsInBurst; i++) {
    const email = `burst-${data.run}-${i}@example.com`;
    const nights = firstNightOfTheBurst + i;
    count(book(data, address, nights, email, { tags: { name: "burst booking" }, ...accepted }));
  }
}
