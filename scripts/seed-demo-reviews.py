#!/usr/bin/env python3
"""
Seeds product reviews for the demo/showcase businesses (Business.IsDemo = 1).

Demo storefronts exist to be looked at, and a catalog where every product reads
"No reviews yet" undersells the feature. This fills that in with data that obeys
the same rules the API enforces, rather than data that only looks right:

  * Only real purchasers review. Every row is derived from an actual non-cancelled
    order containing that product, so each seeded review would have been accepted
    by POST /storefront/products/{id}/reviews. Seeding a review for a customer who
    never bought the product would create a row the app itself could never produce.
  * One review per customer per product, matching
    UX_product_reviews_OnePerCustomerPerProduct.
  * A review is always dated after the order that qualified it.

Comment tone follows the rating - a 5-star review does not complain - and roughly a
quarter of reviews are rating-only, since ProductReview.Comment is nullable and
rating-only is the common case in practice.

Idempotent: re-running skips pairs that already have a review, so it tops up rather
than duplicating or failing on the unique index. It never updates or deletes an
existing review, so anything written through the real API survives.

Usage (from the repo root, with the dev DB container running):

    python scripts/seed-demo-reviews.py            # seed
    python scripts/seed-demo-reviews.py --dry-run  # show what it would do
    python scripts/seed-demo-reviews.py --purge    # delete all seeded demo reviews first

Targets the local dev container by default. Override with MERCHFORGE_DB_EXEC, e.g.
against a different container or a direct mariadb client.
"""

import argparse
import os
import random
import subprocess
import sys
import uuid
from datetime import datetime, timedelta, timezone

DB_EXEC = os.environ.get(
    "MERCHFORGE_DB_EXEC",
    "docker exec -i merchforge-db mariadb -uroot merchforge",
)

# Fixed so a re-seed after a database reset reproduces the same reviews, which keeps
# demo screenshots and any docs that reference them stable.
RANDOM_SEED = 20260903

# Share of eligible (customer, product) pairs that become a review. Deliberately not
# 100%: a catalog where every purchase produced a review looks generated.
REVIEW_RATE = 0.62

# Share of reviews that are a rating with no words.
RATING_ONLY_RATE = 0.25

# Weighted toward the top, like real storefront ratings, but not unanimously.
RATING_WEIGHTS = [(5, 44), (4, 30), (3, 15), (2, 8), (1, 3)]

POSITIVE, NEUTRAL, NEGATIVE = "positive", "neutral", "negative"

GENERIC = {
    POSITIVE: [
        "Exactly what I was hoping for. Arrived quickly and well packaged.",
        "Really pleased with this. Would order again without hesitating.",
        "Great quality for the price. No complaints at all.",
        "Second time ordering this. Just as good as the first.",
        "Better than I expected from the photos.",
    ],
    NEUTRAL: [
        "Does the job. Nothing remarkable either way.",
        "Fine, though I expected a little more at this price.",
        "Decent enough. Delivery took longer than I'd have liked.",
    ],
    NEGATIVE: [
        "Not what I expected from the description.",
        "Arrived damaged. The replacement process was slow.",
        "Quality feels well below the price.",
    ],
}

BY_DOMAIN = {
    "fashion": {
        POSITIVE: [
            "Fits true to size and the fabric feels substantial.",
            "The colour is just like the photos, which is rare.",
            "Washed it twice already and it still looks new.",
            "Really flattering cut. Got compliments the first day.",
            "Comfortable enough to wear all day.",
        ],
        NEUTRAL: [
            "Nice enough, but it runs slightly small - size up.",
            "The fabric is thinner than I expected, though it hangs well.",
            "Looks good, but the stitching on one seam is uneven.",
        ],
        NEGATIVE: [
            "Sizing is way off. Had to send it back.",
            "The colour faded noticeably after one wash.",
        ],
    },
    "electronics": {
        POSITIVE: [
            "Set up in minutes and it's been rock solid since.",
            "Battery life is genuinely as advertised.",
            "Build quality feels a tier above the price.",
            "Sound is much better than I expected at this price.",
            "Pairs instantly every time. No fiddling.",
        ],
        NEUTRAL: [
            "Works well, but the companion app is clunky.",
            "Good performance, though it runs warm under load.",
            "Fine for everyday use. Not a step up from my old one.",
        ],
        NEGATIVE: [
            "Stopped holding a charge within a few weeks.",
            "Connection drops constantly. Frustrating to use.",
        ],
    },
    "phonecase": {
        POSITIVE: [
            "Snaps on perfectly and the buttons still feel clicky.",
            "Survived a drop onto concrete with no damage to the phone.",
            "Slim enough that it still fits my car mount.",
            "Grippy without catching on my pocket.",
            "The print still looks sharp after months of use.",
        ],
        NEUTRAL: [
            "Good protection, but it does add noticeable bulk.",
            "Fits well, though the camera cutout is tighter than I'd like.",
            "Looks great. Picks up fingerprints easily.",
        ],
        NEGATIVE: [
            "Corners started lifting after a couple of weeks.",
            "The cutouts don't line up properly with my phone.",
        ],
    },
    "grocery": {
        POSITIVE: [
            "Arrived fresh and lasted the whole week.",
            "Better quality than what I get at my usual shop.",
            "Packed carefully - nothing bruised in transit.",
            "Genuinely tastes like it was picked recently.",
            "Will be adding this to my regular order.",
        ],
        NEUTRAL: [
            "Fresh enough, though a couple of pieces were undersized.",
            "Fine quality. Portion was smaller than I expected.",
            "Good, but it didn't keep as long as I'd hoped.",
        ],
        NEGATIVE: [
            "Arrived past its best. Had to throw half of it out.",
            "Poor condition on delivery - bruised throughout.",
        ],
    },
}

# Maps a demo business to its comment pool. Matched on the business name because
# BusinessDomain is about the storefront template, not the tone of a review.
DOMAIN_BY_BUSINESS = {
    "Fashion-01": "fashion",
    "Fashion-02": "fashion",
    "Electronics-01": "electronics",
    "PhoneCase Co": "phonecase",
    "Green Basket Market": "grocery",
}


def run_sql(sql: str) -> str:
    result = subprocess.run(
        DB_EXEC, shell=True, input=sql, capture_output=True, text=True
    )
    if result.returncode != 0:
        sys.stderr.write(result.stderr)
        raise SystemExit(f"Database command failed (exit {result.returncode}).")
    return result.stdout


def band_for(rating: int) -> str:
    if rating >= 4:
        return POSITIVE
    if rating == 3:
        return NEUTRAL
    return NEGATIVE


def fetch_pairs() -> list[dict]:
    """
    Every (customer, product) pair that has actually bought, for demo businesses only,
    excluding pairs that already have a review. OrderedAt is the earliest qualifying
    order so a review can never predate the purchase that allows it.
    """
    sql = """
    SELECT b.Name, b.Id, p.Id, c.Id, MIN(o.CreatedAt)
    FROM businesses b
    JOIN orders o        ON o.BusinessId = b.Id
    JOIN order_items oi  ON oi.OrderId = o.Id
    JOIN products p      ON p.Id = oi.ProductId
    JOIN customers c     ON c.Id = o.CustomerId
    WHERE b.IsDemo = 1
      AND o.CustomerId IS NOT NULL
      AND o.Status <> 'Cancelled'
      AND NOT EXISTS (
            SELECT 1 FROM product_reviews r
            WHERE r.ProductId = p.Id AND r.CustomerId = c.Id
      )
    GROUP BY b.Name, b.Id, p.Id, c.Id
    ORDER BY b.Name, p.Id, c.Id;
    """
    rows = []
    for line in run_sql(sql).splitlines()[1:]:
        parts = line.split("\t")
        if len(parts) != 5:
            continue
        rows.append(
            {
                "business_name": parts[0],
                "business_id": parts[1],
                "product_id": parts[2],
                "customer_id": parts[3],
                "ordered_at": parts[4],
            }
        )
    return rows


def build_rows(pairs: list[dict], rng: random.Random) -> list[tuple]:
    ratings = [r for r, _ in RATING_WEIGHTS]
    weights = [w for _, w in RATING_WEIGHTS]
    now = datetime.now(timezone.utc).replace(tzinfo=None)
    rows = []

    for pair in pairs:
        if rng.random() > REVIEW_RATE:
            continue

        rating = rng.choices(ratings, weights=weights, k=1)[0]

        if rng.random() < RATING_ONLY_RATE:
            comment = None
        else:
            domain = DOMAIN_BY_BUSINESS.get(pair["business_name"])
            pool = BY_DOMAIN.get(domain, {}).get(band_for(rating), [])
            # Generic lines keep every business from sounding like the same three
            # sentences on repeat.
            comment = rng.choice(pool + GENERIC[band_for(rating)]) if pool else rng.choice(
                GENERIC[band_for(rating)]
            )

        try:
            ordered_at = datetime.strptime(pair["ordered_at"][:19], "%Y-%m-%d %H:%M:%S")
        except ValueError:
            ordered_at = now - timedelta(days=60)

        # Somewhere between a couple of days and ~10 weeks after the order, never in
        # the future.
        created_at = ordered_at + timedelta(
            days=rng.randint(2, 70), hours=rng.randint(0, 23), minutes=rng.randint(0, 59)
        )
        if created_at > now:
            created_at = now - timedelta(hours=rng.randint(1, 72))

        rows.append(
            (
                str(uuid.uuid4()),
                pair["product_id"],
                pair["business_id"],
                pair["customer_id"],
                rating,
                comment,
                created_at.strftime("%Y-%m-%d %H:%M:%S"),
                pair["business_name"],
            )
        )

    return rows


def sql_literal(value) -> str:
    if value is None:
        return "NULL"
    return "'" + str(value).replace("\\", "\\\\").replace("'", "''") + "'"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true", help="Report only; write nothing.")
    parser.add_argument(
        "--purge",
        action="store_true",
        help="Delete every review on a demo business first, then re-seed.",
    )
    args = parser.parse_args()

    if args.purge and not args.dry_run:
        run_sql(
            "DELETE r FROM product_reviews r "
            "JOIN businesses b ON b.Id = r.BusinessId WHERE b.IsDemo = 1;"
        )
        print("Purged existing reviews on demo businesses.")

    pairs = fetch_pairs()
    print(f"Eligible (customer, product) pairs without a review: {len(pairs)}")

    rng = random.Random(RANDOM_SEED)
    rows = build_rows(pairs, rng)

    if not rows:
        print("Nothing to seed.")
        return

    per_business: dict[str, int] = {}
    for row in rows:
        per_business[row[7]] = per_business.get(row[7], 0) + 1

    print(f"\nWould insert {len(rows)} reviews:" if args.dry_run else f"\nInserting {len(rows)} reviews:")
    for name in sorted(per_business):
        print(f"  {name:<24} {per_business[name]}")

    if args.dry_run:
        return

    values = ",\n".join(
        "({}, {}, {}, {}, {}, {}, 0, {}, {})".format(
            sql_literal(r[0]),
            sql_literal(r[1]),
            sql_literal(r[2]),
            sql_literal(r[3]),
            r[4],
            sql_literal(r[5]),
            sql_literal(r[6]),
            sql_literal(r[6]),
        )
        for r in rows
    )

    run_sql(
        "INSERT INTO product_reviews "
        "(Id, ProductId, BusinessId, CustomerId, Rating, Comment, IsHidden, CreatedAt, UpdatedAt) "
        f"VALUES\n{values};"
    )

    print("\nDone. Totals per demo business:")
    print(
        run_sql(
            """
            SELECT b.Name,
                   COUNT(r.Id) AS reviews,
                   ROUND(AVG(r.Rating), 2) AS avg_rating,
                   COUNT(DISTINCT r.ProductId) AS reviewed_products
            FROM businesses b
            LEFT JOIN product_reviews r ON r.BusinessId = b.Id
            WHERE b.IsDemo = 1
            GROUP BY b.Name ORDER BY b.Name;
            """
        )
    )


if __name__ == "__main__":
    main()
