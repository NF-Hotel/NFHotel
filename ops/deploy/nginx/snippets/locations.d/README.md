# Extra locations of the HTTPS server

Every `*.conf` file in this folder is included in the 443 server of `templates/default.conf.template` (ADR-0011). The folder is empty by default, so nginx behaves exactly as before.

To turn on an optional route, add its file here in a reviewed change. For example, copy `deploy/nginx/optional/api.conf` once the API is deployed on the VM.
