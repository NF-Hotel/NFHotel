// Load test of the public pages and the staff sign-in page of NF Hotel (MIL-036 task 3), run
// by ops/scripts/load-test.sh against the throwaway stack, never against production. Twenty
// virtual guests open the pages for two minutes, about ten times what the hotel sees at its
// busiest; the thresholds are the objectives of MIL-034, so a release that would breach them
// under load fails here first.
import http from "k6/http";
import { check, sleep } from "k6";

const target = __ENV.TARGET || "https://nginx";

export const options = {
  insecureSkipTLSVerify: true, // the stack has a self-signed certificate
  scenarios: {
    guests: {
      executor: "ramping-vus",
      startVUs: 1,
      stages: [
        { duration: "30s", target: 20 },
        { duration: "1m", target: 20 },
        { duration: "30s", target: 0 },
      ],
    },
  },
  thresholds: {
    // MIL-034: 95 % of the requests within 1 second, fewer than 5 % failed.
    http_req_duration: ["p(95)<1000"],
    http_req_failed: ["rate<0.05"],
    checks: ["rate>0.95"],
  },
};

const pages = ["/", "/book", "/admin/login"];

export default function () {
  for (const page of pages) {
    const response = http.get(`${target}${page}`, { tags: { page } });
    check(response, { [`${page} answers 200`]: (r) => r.status === 200 });
    sleep(1);
  }
}
