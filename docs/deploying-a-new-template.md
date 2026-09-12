# Deploying a new template: dev → prod

Checklist for taking a website template that's fully built and working in the
dev environment — template project pushed to `MerchForge-Website-templates`,
`WebsiteTemplate` row and a populated demo business (`IsDemo=1`) already in the
dev database — and making it live on production. Written after doing this by
hand for the first five templates (fashion×2, electronics×2, grocery); every
step below is what that actually took, including the two mistakes that cost
the most time.

Three systems are involved and none of them talk to each other automatically:
the **dev database** (source of truth for the demo content), the **prod
database** (needs its own copy), and the **`MerchForge-Website-templates`**
GitHub repo (needs to know which business to render). Nothing here copies
image files — dev and prod share one R2 bucket, and every image column stores
a relative key (`businesses/{id}/products/{id}/images/{id}.ext`), never a full
URL, so a row that copies verbatim already points at a real object.

Replace every `<...>` placeholder below with your actual values as you go.

---

## 0. What you should already have in dev

- A `website_templates` row: `Name` (e.g. `fashion-template-03`), `Label`,
  `PreviewImageUrl` (a real uploaded image, not the `coming-soon.jpg`
  placeholder), `BusinessDomainId`.
- A demo business for it (`Business.IsDemo = true`, `WebsiteTemplateId`
  pointing at that row), created via SuperAdmin → `POST
  Dashboard/businesses/demo` — real owner login, products, customers, orders.
- The template project itself already pushed to `MerchForge-Website-templates`
  under a folder matching `Name` (minus its own numbering convention — the
  folder is what the deploy workflow discovers and builds).

If any of these don't exist yet, this doc doesn't cover building them — only
moving what already exists in dev into prod.

Get the dev IDs you'll need for everything below:

```bash
docker exec merchforge-db sh -c 'MYSQL_PWD="$MARIADB_ROOT_PASSWORD" mysql -uroot merchforge' <<'EOF'
SELECT Id, Name, Label, BusinessDomainId, PreviewImageUrl FROM website_templates WHERE Name = '<template-name>';
SELECT Id, Name, OwnerUserId FROM businesses WHERE WebsiteTemplateId = (SELECT Id FROM website_templates WHERE Name = '<template-name>') AND IsDemo = 1;
EOF
```

Note the template's dev `Id` and the business's `Id` — call them
`$DEV_TEMPLATE_ID` and `$BUSINESS_ID` below.

---

## 1. Create the WebsiteTemplate row in prod

**Skip this step entirely if `$DEV_TEMPLATE_ID` is one of the two seeded
IDs** (`e1000000-0000-4000-8000-000000000001` fashion-template-01, or
`...002` electronic-template-01) — those are seeded by an EF migration and
already exist, identically, in both databases. Just confirm prod's copy has
the real preview image rather than the migration's placeholder, and edit it
through the admin UI if not.

For any other template: in **prod's** SuperAdmin dashboard (Templates page),
add a new template with the **exact same `Name`** as dev (it has a unique
index and is how a later deployment step identifies the physical template
project — it must match character-for-character) and the same Label/preview
image.

Then get prod's own ID for it (will differ from dev's — nothing forces these
to match, and for anything not seeded, they won't):

```bash
docker exec -i merchforge-db-prod sh -c 'MYSQL_PWD="$MARIADB_ROOT_PASSWORD" mysql -uroot merchforge' <<'EOF'
SELECT Id FROM website_templates WHERE Name = '<template-name>';
EOF
```

Call this `$PROD_TEMPLATE_ID`.

---

## 2. Copy the demo business's full data graph, dev → prod

Run this **on the machine that can reach the dev database** (`mysqldump`
against the local `merchforge-db` container). It dumps everything scoped to
one business, remaps the template ID if it changed in step 1, and produces a
plain, reviewable `.sql` file — read it before running it against prod.

```bash
BUSINESS_ID="<paste the dev business id>"
DEV_TEMPLATE_ID="<dev template id, if it changed>"
PROD_TEMPLATE_ID="<prod template id from step 1, if it changed>"
OUT="/tmp/migrate-${BUSINESS_ID}.sql"

dump_table() {
  local table="$1" where="$2"
  # --single-transaction is required, not optional: mysqldump's default table
  # locking conflicts with a --where clause that subqueries a different table
  # ("Table '...' was not locked with LOCK TABLES"). This avoids the lock
  # entirely rather than working around it.
  docker exec merchforge-db sh -c "MYSQL_PWD=\"\$MARIADB_ROOT_PASSWORD\" mysqldump -uroot --single-transaction --no-create-info --complete-insert --skip-triggers --skip-add-locks --skip-comments --compact merchforge $table --where=\"$where\""
}

{
  echo "SET FOREIGN_KEY_CHECKS=0;"
  echo "START TRANSACTION;"
  dump_table users "Id = (SELECT OwnerUserId FROM businesses WHERE Id='$BUSINESS_ID')"
  dump_table businesses "Id='$BUSINESS_ID'"
  dump_table business_users "BusinessId='$BUSINESS_ID'"
  dump_table subscriptions "BusinessId='$BUSINESS_ID'"
  dump_table business_feature_credits "BusinessId='$BUSINESS_ID'"
  dump_table business_website_drafts "BusinessId='$BUSINESS_ID'"
  dump_table products "BusinessId='$BUSINESS_ID'"
  dump_table product_images "ProductId IN (SELECT Id FROM products WHERE BusinessId='$BUSINESS_ID')"
  dump_table product_drafts "BusinessId='$BUSINESS_ID'"
  dump_table stock_movements "BusinessId='$BUSINESS_ID'"
  dump_table customers "Id IN (SELECT CustomerId FROM orders WHERE BusinessId='$BUSINESS_ID' AND CustomerId IS NOT NULL)"
  dump_table orders "BusinessId='$BUSINESS_ID'"
  dump_table order_items "OrderId IN (SELECT Id FROM orders WHERE BusinessId='$BUSINESS_ID')"
  dump_table order_status_history "OrderId IN (SELECT Id FROM orders WHERE BusinessId='$BUSINESS_ID')"
  dump_table product_reviews "BusinessId='$BUSINESS_ID'"
  echo "COMMIT;"
  echo "SET FOREIGN_KEY_CHECKS=1;"
} > "$OUT"

# Idempotent: safe to re-run if something needs redoing.
sed -i 's/^INSERT INTO/INSERT IGNORE INTO/' "$OUT"

# Only if the template ID actually changed in step 1.
if [ -n "$DEV_TEMPLATE_ID" ] && [ -n "$PROD_TEMPLATE_ID" ] && [ "$DEV_TEMPLATE_ID" != "$PROD_TEMPLATE_ID" ]; then
  sed -i "s/$DEV_TEMPLATE_ID/$PROD_TEMPLATE_ID/g" "$OUT"
fi

wc -l "$OUT"
```

**Read the file once before running it.** Sanity-check the row counts against
what you expect (product count, order count) — an empty or truncated section
usually means a typo in `$BUSINESS_ID`.

Copy it to the server and run it:

```bash
scp "$OUT" your-user@your-server:/tmp/
ssh your-user@your-server "docker exec -i merchforge-db-prod sh -c 'MYSQL_PWD=\"\$MARIADB_ROOT_PASSWORD\" mysql -uroot merchforge' < /tmp/$(basename "$OUT")"
```

**Why the IDs are kept identical to dev, on purpose:** every image column
(`product_images.Url`, `products.ImageUrl`, `order_items.ProductImageUrl`)
stores a key built from the business/product ID
(`businesses/{businessId}/products/{productId}/images/{imageId}.ext`). Keep
the same IDs and these keys resolve against the already-uploaded objects in
the shared R2 bucket automatically — no file copy, no re-upload. Change the
ID and you'd have to re-upload every image by hand.

**What's safe to copy verbatim without remapping**, because these are seeded
identically in every environment via EF migration `HasData` (verified once,
holds forever unless someone edits the seed data itself): `BusinessDomainId`,
`CategoryId`, `SubscriptionPlanId`, role IDs (`SystemRoleId`, `RoleId`,
`BusinessUserRoleId`), `FeatureId`. The **only** FK that can legitimately
differ between environments is `WebsiteTemplateId` (and
`business_website_drafts.TemplateFieldsWebsiteTemplateId`, if that row has
one) — handled in step 1/above.

---

## 3. Point the template at its live demo, and mark the business published

Two fields, both otherwise only set by flows a demo business never goes
through (a real merchant's `WebsiteTemplateRequest` closing), so they need a
direct update:

```bash
docker exec -i merchforge-db-prod sh -c 'MYSQL_PWD="$MARIADB_ROOT_PASSWORD" mysql -uroot merchforge' <<EOF
UPDATE businesses SET WebsiteUrl = 'https://templates.merchforge.site/<template-folder>/'
  WHERE Id = '$BUSINESS_ID';
UPDATE website_templates SET PreviewWebsiteUrl = 'https://templates.merchforge.site/<template-folder>/'
  WHERE Id = '$PROD_TEMPLATE_ID';
SELECT Name, WebsiteUrl FROM businesses WHERE Id = '$BUSINESS_ID';
EOF
```

`<template-folder>` is the folder name in `MerchForge-Website-templates`
(`fashion-template`, `fashion-template-02`, `electronic-template-02`, etc.) —
the domain `templates.merchforge.site` is already wired to GitHub Pages for
that repo, confirmed live at that path.

---

## 4. Tell the deployed template which business to render

In **`MerchForge-Website-templates`** → Settings → Secrets and variables →
Actions → **Variables tab** (not Secrets — see the warning below):

Add one variable, named after the folder with dashes turned to underscores
and uppercased (`scripts/template-env.mjs` builds this suffix from the folder
name exactly this way — check that script if a name ever looks wrong):

| Variable | Value |
|---|---|
| `VITE_BUSINESS_ID_<TEMPLATE_FOLDER_UPPERCASE>` | `$BUSINESS_ID` |

`VITE_API_ORIGIN` and `VITE_PLATFORM_ORIGIN` are shared across every template
and only need setting once, ever (both are `https://merchforge.site`) — skip
them if they're already there.

> **This must be a repository Variable, not a Secret.** They read from
> completely different GitHub contexts (`vars.*` vs `secrets.*`), and the
> workflow's per-template lookup (`scripts/template-env.mjs`) only ever reads
> from `vars` — it has no fallback path for a per-template value placed in
> Secrets. Put `VITE_BUSINESS_ID_FASHION_TEMPLATE` in Secrets and the build
> won't fail (a *shared* secrets fallback exists for the three base names),
> it will silently render some *other* business's catalog instead — which
> looks like "the storefront is broken" with no error pointing at the actual
> cause. This is the mistake that ate the most time doing this the first
> time; it will not announce itself if you make it again.

Then rebuild: **Actions → Deploy templates → Run workflow** (`workflow_dispatch`
— no code push needed, these are build-time values).

---

## 5. Verify

- `https://templates.merchforge.site/<template-folder>/` loads and shows real
  products, not the "This store isn't available right now" screen. If you see
  that screen after redeploying, it means the business ID the build actually
  used doesn't match a real business — almost always the Secrets-vs-Variables
  mistake above.
- Log into prod as the demo owner (credentials below) — dashboard Overview
  should show the website as published.
- `docker exec -i merchforge-db-prod ...` a quick row-count check
  (`SELECT COUNT(*) FROM products WHERE BusinessId='$BUSINESS_ID'`, etc.)
  against what step 2's dump reported, to confirm nothing silently failed.

---

## 6. Record the credentials

Add the business to
[`demoBusinessCredentials.ts`](../../MerchForgeClient/src/features/Dashboard/SuperAdminDashboard/data/demoBusinessCredentials.ts)
(frontend repo) — no other frontend change is needed, since the business ID
is identical in both environments:

```ts
"<businessId>": {
    email: "demo-<name>@merchforge.internal",
    password: "<whatever was set when the demo business was created>",
},
```
