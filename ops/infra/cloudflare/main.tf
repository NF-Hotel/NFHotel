# The public edge of NF Hotel as code (ADR-0046, MIL-078): the routes of the tunnel nf-hotel
# in their order, and the Access application of the back office with its policy Team, as the
# wiki page Public-Access sets them up and RC-339 recorded them on 2026-11-11 (version 4 of the
# configuration of cloudflared). No state is kept: every plan imports the three objects through
# the import blocks below, so a plan that shows the three imports and nothing to add, change or
# destroy says that the live account matches this code. A change is a pull request, applied by
# S01 or S03 from their own machine with `bash ops/scripts/edge-tofu.sh apply`.

locals {
  public_hostname = "nfhotel.devmatiker.com"
  # The public listener of nginx, on the proxy network inside the VM (ADR-0028 decision 2).
  public_listener = "http://nginx:8082"
}

# The routes are matched from the top and the first match wins, so the route that checks the
# Access token comes first: below the route without a path it would never be used (MIL-072).
# `(?i)` makes its path ignore letter case, as the web application does (ADR-0029 decision 2).
# The last rule answers 404 to a request that no route matches.
resource "cloudflare_zero_trust_tunnel_cloudflared_config" "nf_hotel" {
  account_id = var.account_id
  tunnel_id  = var.tunnel_id
  # Managed by Cloudflare, not by a file of cloudflared: the VM holds only the token.
  source = "cloudflare"

  config = {
    ingress = [
      {
        hostname = local.public_hostname
        path     = "(?i)^/admin"
        service  = local.public_listener
        origin_request = {
          access = {
            required  = true
            team_name = var.access_team_name
            aud_tag   = [cloudflare_zero_trust_access_application.back_office.aud]
          }
        }
      },
      {
        hostname = local.public_hostname
        service  = local.public_listener
      },
      {
        service = "http_status:404"
      },
    ]
  }
}

import {
  to = cloudflare_zero_trust_tunnel_cloudflared_config.nf_hotel
  id = "${var.account_id}/${var.tunnel_id}"
}

# The Access application in front of /admin on the public name (ADR-0029 decision 1): the
# session lasts one week, and only the policy Team lets anyone in.
resource "cloudflare_zero_trust_access_application" "back_office" {
  account_id       = var.account_id
  name             = "NF Hotel back office"
  type             = "self_hosted"
  session_duration = "168h"

  destinations = [
    {
      type = "public"
      uri  = "${local.public_hostname}/admin"
    },
  ]

  policies = [
    {
      id         = cloudflare_zero_trust_access_policy.team.id
      precedence = 1
    },
  ]
}

import {
  to = cloudflare_zero_trust_access_application.back_office
  id = "accounts/${var.account_id}/${var.access_application_id}"
}

# The policy Team: allow the listed addresses of the staff, which come from a setting outside
# git (variables.tf).
resource "cloudflare_zero_trust_access_policy" "team" {
  account_id = var.account_id
  name       = "Team"
  decision   = "allow"
  include    = [for email in var.team_emails : { email = { email = email } }]
}

import {
  to = cloudflare_zero_trust_access_policy.team
  id = "${var.account_id}/${var.access_policy_id}"
}
