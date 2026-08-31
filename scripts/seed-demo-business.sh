#!/usr/bin/env bash
# Gets a fresh docker-compose stack from "empty database" to "a real, logged-in-
# ready demo business" using nothing but the real public API - no direct SQL,
# no bypassing the invitation/password flows Phase 2 built. Reads the owner
# invitation email back out of Mailpit (http://localhost:8025) rather than
# needing real SMTP just to try the app locally.
#
# Idempotent: safe to re-run. The SuperAdmin bootstrap step is a no-op once one
# already exists (by design - see AuthService.RegisterSuperAdmin); re-inviting
# the same demo owner email revokes the previous invitation and issues a new
# one, same as the real feature does.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

API="http://localhost:8080"
MAILPIT="http://localhost:8025"

SUPERADMIN_EMAIL="superadmin@merchforge.local"
SUPERADMIN_PASSWORD="DemoPassw0rd!"
OWNER_EMAIL="owner@merchforge.local"
OWNER_PASSWORD="DemoPassw0rd!"
BUSINESS_NAME="Demo Business"

echo "Waiting for the API to be healthy..."
for _ in $(seq 1 60); do
    if curl -fsS "$API/health" > /dev/null 2>&1; then
        break
    fi
    sleep 2
done
curl -fsS "$API/health" > /dev/null || { echo "API never became healthy." >&2; exit 1; }

echo "Bootstrapping the SuperAdmin account (a no-op if one already exists)..."
curl -fsS -X POST "$API/api/Auth/register/superAdmin" \
    -H "Content-Type: application/json" \
    -d "{\"firstName\":\"Demo\",\"lastName\":\"Admin\",\"email\":\"$SUPERADMIN_EMAIL\",\"password\":\"$SUPERADMIN_PASSWORD\"}" \
    > /dev/null 2>&1 || echo "  (already bootstrapped - continuing)"

echo "Logging in as SuperAdmin..."
LOGIN_RESPONSE="$(curl -fsS -X POST "$API/api/Auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$SUPERADMIN_EMAIL\",\"password\":\"$SUPERADMIN_PASSWORD\"}")"
ACCESS_TOKEN="$(echo "$LOGIN_RESPONSE" | grep -o '"accessToken":"[^"]*"' | head -1 | cut -d'"' -f4)"

if [[ -z "$ACCESS_TOKEN" ]]; then
    echo "Could not log in as SuperAdmin. Response was:" >&2
    echo "$LOGIN_RESPONSE" >&2
    exit 1
fi

echo "Fetching a business domain to use for the demo business..."
DOMAINS_RESPONSE="$(curl -fsS "$API/api/domains")"
DOMAIN_ID="$(echo "$DOMAINS_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)"

if [[ -z "$DOMAIN_ID" ]]; then
    echo "No business domains found - the migration seed data may not have applied." >&2
    exit 1
fi

echo "Inviting the demo owner ($OWNER_EMAIL)..."
curl -fsS -X POST "$API/api/Invitation/business-owner" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -d "{\"email\":\"$OWNER_EMAIL\"}" \
    > /dev/null

echo "Reading the invitation email out of Mailpit..."
INVITATION_LINK=""
for _ in $(seq 1 30); do
    MESSAGE_ID="$(curl -fsS "$MAILPIT/api/v1/messages?limit=50" \
        | grep -o "\"ID\":\"[^\"]*\"[^}]*\"To\":\[{\"Name\":\"\",\"Address\":\"$OWNER_EMAIL\"" \
        | grep -o '"ID":"[^"]*"' | head -1 | cut -d'"' -f4)"

    if [[ -n "$MESSAGE_ID" ]]; then
        BODY="$(curl -fsS "$MAILPIT/api/v1/message/$MESSAGE_ID")"
        INVITATION_LINK="$(echo "$BODY" | grep -o 'http://localhost:8081/accept-invitation?[^"\\ ]*' | head -1)"
        [[ -n "$INVITATION_LINK" ]] && break
    fi
    sleep 1
done

if [[ -z "$INVITATION_LINK" ]]; then
    echo "Couldn't find the invitation email in Mailpit within 30s. Check http://localhost:8025 manually." >&2
    exit 1
fi

echo "Found invitation link: $INVITATION_LINK"

INVITATION_TOKEN="$(echo "$INVITATION_LINK" | grep -o 'token=[^&]*' | cut -d= -f2)"
# URL-decode (the token is base64, which uses + and /, percent-encoded in the link).
INVITATION_TOKEN="$(printf '%b' "${INVITATION_TOKEN//%/\\x}")"

echo "Completing the owner's registration (setting their own password, per Phase 2)..."
REGISTRATION_RESPONSE="$(curl -fsS -X POST "$API/api/Auth/businessOwner/registration" \
    -H "Content-Type: application/json" \
    -d "{
        \"firstName\":\"Demo\",
        \"lastName\":\"Owner\",
        \"businessName\":\"$BUSINESS_NAME\",
        \"email\":\"$OWNER_EMAIL\",
        \"password\":\"$OWNER_PASSWORD\",
        \"invitationToken\":\"$INVITATION_TOKEN\",
        \"businessDomainId\":\"$DOMAIN_ID\",
        \"newCategoryNames\":[],
        \"selectedProductAttributeKeys\":[]
    }")"

if ! echo "$REGISTRATION_RESPONSE" | grep -q '"accessToken"'; then
    echo "Owner registration did not succeed. Response was:" >&2
    echo "$REGISTRATION_RESPONSE" >&2
    exit 1
fi

cat <<EOF

Demo business is ready.

  Dashboard:        http://localhost:8081
  SuperAdmin login: $SUPERADMIN_EMAIL / $SUPERADMIN_PASSWORD
  Owner login:      $OWNER_EMAIL / $OWNER_PASSWORD
  Business:         $BUSINESS_NAME
  Sent emails:       http://localhost:8025

EOF
