#!/usr/bin/env bash
# The DORA metrics of NF-Hotel/NFHotel for one month (MIL-023 task 5), from the GitHub
# API: the merged pull requests, the "NF Hotel Release" and "NF Hotel CD" runs and the
# deployments to the environment production. Definitions: docs/sqa/dora-metrics.md.
#
# Subcommands:
#   fetch   read the month from the GitHub API (read-only) into data files in --out:
#           pulls.json, commits.json, releases.json and deployments.json
#   report  compute the five metrics from the data files in --data and print them as
#           Markdown (or JSON with --json)
#   job-fields  read a CD job log on standard input and print its release run and its
#           apply input as JSON (used by fetch; tested on its own)
#
# A deployment is one CD run that reached the environment production with apply true;
# a dry run is not a deployment. The deployed commit is the head commit of the release
# run that the CD run deployed.
#
# Usage: dora-metrics.sh fetch --month YYYY-MM --out DIR [--repo OWNER/NAME]
#        dora-metrics.sh report --month YYYY-MM --data DIR [--json]
#        dora-metrics.sh job-fields <LOG
# Env:   GH_TOKEN   a token that can read the repository's actions and deployments (fetch)
#        NOW        the end of the window when the month is not over (default: now;
#                   for tests)
# Exit:  0 done; 1 an API call or a data file failed; 2 usage error
# Needs: bash 4+, jq 1.6+, GNU date, gh (fetch)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

readonly RELEASE_WORKFLOW="NF Hotel Release" CD_WORKFLOW="NF Hotel CD"
readonly ENVIRONMENT=production
repo=NF-Hotel/NFHotel month="" dir="" format=markdown

# window <month>: set since and until (ISO 8601, UTC) for the month; until is NOW when
# the month is not over yet.
window() {
  [[ "$1" =~ ^[0-9]{4}-(0[1-9]|1[0-2])$ ]] || die "--month must be YYYY-MM: $1"
  since="$(date -u -d "$1-01" +%Y-%m-%dT%H:%M:%SZ)"
  until="$(date -u -d "$1-01 +1 month" +%Y-%m-%dT%H:%M:%SZ)"
  local now
  now="$(date -u -d "${NOW:-now}" +%Y-%m-%dT%H:%M:%SZ)"
  [[ "$now" > "$until" ]] || until="$now"
}

# job_fields: the release run and the apply input that the deploy-release action prints
# at the start of its step, from a job log on standard input.
job_fields() {
  awk '
    { sub(/\r$/, "") }
    $2 == "release-run-id:" && run == "" { run = $3 }
    $2 == "apply:" && apply == "" { apply = $3 }
    END { printf "{\"release_run\":%s,\"apply\":%s}\n", run == "" ? "null" : run,
      apply == "true" || apply == "false" ? apply : "null" }
  '
}

# text <jq arguments>: jq -r without the carriage returns that jq writes on Windows.
text() {
  jq -r "$@" | tr -d '\r'
}

# workflow_id <name>: the ID of the workflow of the repository with that name.
workflow_id() {
  gh api "repos/$repo/actions/workflows?per_page=100" \
    --jq ".workflows[] | select(.name == \"$1\") | .id" | grep . ||
    die "no workflow named $1 in $repo"
}

# runs <workflow name>: the runs of that workflow created in the window, as a JSON array.
runs() {
  local id
  id="$(workflow_id "$1")"
  gh api --paginate "repos/$repo/actions/workflows/$id/runs?per_page=100&created=$since..$until" \
    --jq '.workflow_runs[] | {id, event, head_sha, conclusion, created_at, updated_at}' |
    jq -s .
}

fetch() {
  mkdir -p "$dir"
  echo "==> pull requests merged into main from $since to $until"
  gh api --paginate "repos/$repo/pulls?state=closed&base=main&sort=updated&direction=desc&per_page=100" \
    --jq ".[] | select(.merged_at != null and .merged_at >= \"$since\" and .merged_at < \"$until\")
      | {number, title, merged_at, merge_commit_sha, labels: [.labels[].name]}" |
    jq -s . >"$dir/pulls.raw.json"
  local n first
  : >"$dir/pulls.jsonl"
  for n in $(text '.[].number' "$dir/pulls.raw.json"); do
    first="$(gh api "repos/$repo/pulls/$n/commits?per_page=100" --jq '[.[].commit.author.date] | min')"
    jq -c --argjson n "$n" --arg first "$first" '.[] | select(.number == $n)
      | . + {first_commit_at: $first}' "$dir/pulls.raw.json" >>"$dir/pulls.jsonl"
  done
  jq -s 'sort_by(.merged_at)' "$dir/pulls.jsonl" >"$dir/pulls.json"
  rm -f "$dir/pulls.raw.json" "$dir/pulls.jsonl"

  echo "==> commits on main (from 31 days before the window)"
  gh api --paginate "repos/$repo/commits?sha=main&per_page=100&since=$(date -u -d "$since -31 days" +%Y-%m-%dT%H:%M:%SZ)" \
    --jq '.[] | {sha, date: .commit.committer.date}' | jq -s 'reverse' >"$dir/commits.json"

  echo "==> runs of $RELEASE_WORKFLOW and $CD_WORKFLOW"
  runs "$RELEASE_WORKFLOW" >"$dir/releases.json"
  runs "$CD_WORKFLOW" >"$dir/cd-runs.json"

  echo "==> deployments to $ENVIRONMENT and their job logs"
  gh api --paginate "repos/$repo/deployments?environment=$ENVIRONMENT&per_page=100" \
    --jq '.[] | {id, created_at}' | jq -s . >"$dir/environment.json"
  local id statuses run job fields
  : >"$dir/deployments.jsonl"
  for id in $(text '.[].id' "$dir/environment.json"); do
    statuses="$(gh api "repos/$repo/deployments/$id/statuses?per_page=100")"
    run="$(text '[.[].log_url // empty | capture("/runs/(?<r>[0-9]+)/job/") | .r] | first // empty' <<<"$statuses")"
    job="$(text '[.[].log_url // empty | capture("/job/(?<j>[0-9]+)") | .j] | first // empty' <<<"$statuses")"
    # Only a deployment of a CD run of the window; other workflows may use production too.
    if [[ -z "$run" ]] || ! jq -e --argjson r "$run" 'any(.[]; .id == $r)' "$dir/cd-runs.json" >/dev/null; then
      continue
    fi
    fields="$(gh api "repos/$repo/actions/jobs/$job/logs" 2>/dev/null | job_fields)" ||
      fields='{"release_run":null,"apply":null}'
    jq -c --argjson run "$run" --argjson fields "$fields" --argjson statuses "$statuses" \
      --slurpfile cd "$dir/cd-runs.json" --slurpfile rel "$dir/releases.json" -n '
      ($cd[0][] | select(.id == $run)) as $r
      | ($statuses | map(select(.state == "success" or .state == "failure" or .state == "error"))
         | sort_by(.created_at) | last) as $final
      | ($fields.release_run // null) as $release
      | {run_id: $run, event: $r.event, conclusion: $r.conclusion,
         started_at: $r.created_at, finished_at: ($final.created_at // $r.updated_at),
         apply: ($fields.apply // ($r.event == "workflow_run")),
         release_run: $release,
         sha: (if $release then ([$rel[0][] | select(.id == $release) | .head_sha] | first)
               elif $r.event == "workflow_run" then $r.head_sha else null end)}' \
      >>"$dir/deployments.jsonl"
  done
  jq -s 'sort_by(.finished_at)' "$dir/deployments.jsonl" >"$dir/deployments.json"
  rm -f "$dir/deployments.jsonl" "$dir/environment.json"
  echo "==> data in $dir"
}

# The metrics, from the data files. A failed deployment is a CD run with apply true that
# failed, or a successful one that the next successful deployment rolled back (an older
# commit of main). Consecutive failed deployments are one incident; it is recovered by
# the next successful deployment. A rework deployment is that recovering deployment, or
# one that ships a pull request labelled hotfix.
# shellcheck disable=SC2016 # $variables in the jq program are jq variables
readonly METRICS='
  def hours(a; b): (((b | fromdateiso8601) - (a | fromdateiso8601)) / 36) | round / 100;
  def median: sort | if length == 0 then null
    elif length % 2 == 1 then .[length / 2 | floor]
    else (.[length / 2 - 1] + .[length / 2]) / 2 end;
  def pct(a; b): if b == 0 then null else (a * 1000 / b | round) / 10 end;

  ($commits | to_entries | map({key: .value.sha, value: .key}) | from_entries) as $pos
  | [$deployments[] | select(.apply == true and (.conclusion == "success" or .conclusion == "failure")
      and .finished_at >= $since and .finished_at < $until)
     | . + {pos: (if .sha then $pos[.sha] else null end)}] as $all
  | [$all[] | select(.conclusion == "success")] as $ok
  | [range(0; $ok | length) as $i
     | select($i + 1 < ($ok | length) and $ok[$i].pos != null and $ok[$i + 1].pos != null
         and $ok[$i + 1].pos < $ok[$i].pos) | $ok[$i].run_id] as $rolled_back
  | [$all[] | . + {failed: (.conclusion == "failure" or (.run_id | IN($rolled_back[])))}] as $attempts
  | [range(0; $attempts | length) as $i
     | select($attempts[$i].failed and ($i == 0 or ($attempts[$i - 1].failed | not)))
     | {start: $attempts[$i],
        end: ([$attempts[$i + 1:][] | select(.failed | not)] | first)}] as $incidents
  | [$incidents[] | select(.end) | .end.run_id] as $recovering
  | [$pulls[] | select(.merged_at >= $since and .merged_at < $until) | . as $p | ($pos[$p.merge_commit_sha]) as $at
     | {number, title, labels, first_commit_at, merged_at,
        deployed: (if $at == null then null else
          ([$ok[] | select(.pos != null and .pos >= $at)] | first) end)}] as $changes
  | [$changes[] | select(.deployed) | hours(.first_commit_at; .deployed.finished_at)] as $lead
  | [$ok[] | select((.run_id | IN($recovering[])) or
       (. as $d | any($changes[]; .deployed.run_id == $d.run_id and (.labels | index("hotfix")))))
     | .run_id] as $rework
  | (((($until | fromdateiso8601) - ($since | fromdateiso8601)) / 86400 * 10 | round) / 10) as $days
  | {repository: $repository, since: $since, until: $until, days: $days,
     change_lead_time: {median_hours: ($lead | median), changes: ($changes | length),
       deployed: ($lead | length), not_deployed: [$changes[] | select(.deployed == null) | .number]},
     deployment_frequency: {deployments: ($ok | length),
       per_week: (if $days == 0 then null else (($ok | length) * 7 * 10 / $days | round) / 10 end),
       days_with_deployment: ([$ok[] | .finished_at[0:10]] | unique | length)},
     change_failure_rate: {percent: pct([$attempts[] | select(.failed)] | length; $attempts | length),
       failed: ([$attempts[] | select(.failed)] | length), attempts: ($attempts | length),
       rolled_back: ($rolled_back | length)},
     failed_deployment_recovery_time: {median_hours:
       ([$incidents[] | select(.end) | hours(.start.finished_at; .end.finished_at)] | median),
       incidents: ($incidents | length), open: ([$incidents[] | select(.end == null)] | length)},
     deployment_rework_rate: {percent: pct($rework | length; $attempts | length),
       rework: ($rework | length), attempts: ($attempts | length)},
     releases: {successful: ([$releases[] | select(.conclusion == "success")] | length),
       failed: ([$releases[] | select(.conclusion == "failure")] | length),
       not_deployed: [$releases[] | select(.conclusion == "success") | .head_sha as $s
         | select(any($ok[]; .sha == $s) | not) | .id]},
     attempts: [$attempts[] | {run_id, event, conclusion, failed, sha: (.sha // "" | .[0:7]),
       finished_at, release_run}]}
'

readonly MARKDOWN='
  def h: if . == null then "n/a" else "\(.) h" end;
  def p: if . == null then "n/a" else "\(.) %" end;
  "| Metric | Value | Basis |",
  "| --- | --- | --- |",
  "| Change lead time (median, first commit to production) | \(.change_lead_time.median_hours | h) | \(.change_lead_time.deployed) of \(.change_lead_time.changes) merged pull requests deployed |",
  "| Deployment frequency | \(.deployment_frequency.deployments) (\(.deployment_frequency.per_week) per week) | deployments on \(.deployment_frequency.days_with_deployment) of \(.days) days |",
  "| Change failure rate | \(.change_failure_rate.percent | p) | \(.change_failure_rate.failed) of \(.change_failure_rate.attempts) deployments failed (\(.change_failure_rate.rolled_back) rolled back) |",
  "| Failed deployment recovery time (median) | \(.failed_deployment_recovery_time.median_hours | h) | \(.failed_deployment_recovery_time.incidents) incidents, \(.failed_deployment_recovery_time.open) not recovered |",
  "| Deployment rework rate | \(.deployment_rework_rate.percent | p) | \(.deployment_rework_rate.rework) of \(.deployment_rework_rate.attempts) deployments unplanned |",
  "",
  "Window: \(.since) to \(.until) (\(.days) days), \(.repository). Releases: \(.releases.successful) successful, \(.releases.failed) failed, \(.releases.not_deployed | length) successful releases never deployed.",
  "",
  "| CD run | Event | Conclusion | Failed deployment | Commit | Finished |",
  "| --- | --- | --- | --- | --- | --- |",
  (.attempts[] | "| \(.run_id) | \(.event) | \(.conclusion) | \(if .failed then "yes" else "no" end) | \(.sha) | \(.finished_at) |")
'

report() {
  local f
  for f in pulls commits releases deployments; do
    [[ -r "$dir/$f.json" ]] || die "missing $dir/$f.json (run fetch first)"
  done
  local metrics
  metrics="$(jq -n --arg since "$since" --arg until "$until" --arg repository "$repo" \
    --slurpfile pulls "$dir/pulls.json" --slurpfile commits "$dir/commits.json" \
    --slurpfile releases "$dir/releases.json" --slurpfile deployments "$dir/deployments.json" \
    '$pulls[0] as $pulls | $commits[0] as $commits | $releases[0] as $releases
     | $deployments[0] as $deployments | '"$METRICS")" || die "the data files in $dir are not valid"
  if [[ "$format" == json ]]; then
    jq . <<<"$metrics"
  else
    jq -r "$MARKDOWN" <<<"$metrics"
  fi
}

command="${1:-}"
[[ $# -gt 0 ]] && shift
while [[ $# -gt 0 ]]; do
  case "$1" in
    --month) month="${2:-}" && shift 2 ;;
    --out | --data) dir="${2:-}" && shift 2 ;;
    --repo) repo="${2:-}" && shift 2 ;;
    --json) format=json && shift ;;
    -h | --help) usage && exit 0 ;;
    *) usage >&2 && exit 2 ;;
  esac
done

case "$command" in
  fetch | report)
    [[ -n "$month" && -n "$dir" ]] || {
      usage >&2
      exit 2
    }
    window "$month"
    "$command"
    ;;
  job-fields) job_fields ;;
  -h | --help) usage ;;
  *) usage >&2 && exit 2 ;;
esac
