# OpenTofu and the Cloudflare provider for the public edge of NF Hotel (ADR-0046, MIL-078).
# OpenTofu is pinned by the digest of its image in ops/scripts/edge-tofu.sh, so this file only
# refuses another minor version. The provider is pinned to one version here and to its
# checksums in .terraform.lock.hcl, which `bash ops/scripts/edge-tofu.sh lock` writes. There is
# no backend: no state is kept, because every plan imports the live objects (main.tf).
terraform {
  required_version = "~> 1.13.0"

  required_providers {
    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "5.29.0"
    }
  }
}

# The token comes from CLOUDFLARE_API_TOKEN in the environment of tofu, never from a file of
# the repository: the token that reads in the workflow edge-comparison.yaml, the token that
# writes only on the machine of S01 or S03 (ADR-0046 decision 5).
provider "cloudflare" {}
