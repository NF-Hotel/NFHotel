# The values that stay outside git (ADR-0046 decision 4). tofu reads them from TF_VAR_* in its
# environment: the secrets of the environment cloudflare-read in the workflow
# edge-comparison.yaml, or the file edge.env (from edge.env.example, ignored by git) on the
# machine of S01 or S03. The wiki page Public-Access says where each one is in the dashboard.
#
# The four identifiers are not marked sensitive, because an import block refuses a sensitive
# identifier; they stay out of the log of the workflow because they are secrets there, which
# GitHub masks, and because the workflow never prints the plan.

variable "account_id" {
  description = "The identifier of the Cloudflare account: 32 hexadecimal characters."
  type        = string

  validation {
    condition     = can(regex("^[0-9a-f]{32}$", var.account_id))
    error_message = "The account_id must be 32 lowercase hexadecimal characters."
  }
}

variable "tunnel_id" {
  description = "The identifier (a UUID) of the tunnel nf-hotel."
  type        = string

  validation {
    condition     = can(regex("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", var.tunnel_id))
    error_message = "The tunnel_id must be a UUID in lowercase."
  }
}

variable "access_application_id" {
  description = "The identifier (a UUID) of the Access application NF Hotel back office."
  type        = string

  validation {
    condition     = can(regex("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", var.access_application_id))
    error_message = "The access_application_id must be a UUID in lowercase."
  }
}

variable "access_policy_id" {
  description = "The identifier (a UUID) of the Access policy Team."
  type        = string

  validation {
    condition     = can(regex("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", var.access_policy_id))
    error_message = "The access_policy_id must be a UUID in lowercase."
  }
}

variable "access_team_name" {
  description = "The team name of Zero Trust: the first part of <team>.cloudflareaccess.com."
  type        = string

  validation {
    condition     = can(regex("^[a-z0-9-]+$", var.access_team_name))
    error_message = "The access_team_name must be lowercase letters, digits and hyphens."
  }
}

# The addresses are personal data, so tofu never shows them in a plan or an error.
variable "team_emails" {
  description = "The e-mail addresses of the staff that the policy Team allows."
  type        = set(string)
  sensitive   = true

  validation {
    condition     = length(var.team_emails) > 0 && alltrue([for email in var.team_emails : can(regex("^[^@\\s]+@[^@\\s]+$", email))])
    error_message = "The team_emails must be a list of one or more e-mail addresses."
  }
}
