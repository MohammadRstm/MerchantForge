# Production health checks

Read-only commands for checking the MerchForge stack on the production host, and
what the output means. Written after the September 2026 outage review — see that
report for why several of these look at the *host* rather than at our containers.

All commands here are safe to run at any time. Nothing restarts, prunes, or
modifies anything.

---

## The routine check

One command, covering restarts, OOM kills, health, resource use against the caps,
and host CPU:

```bash
echo "── $(date -u '+%F %T') UTC ──"; for c in merchforge-api-prod merchforge-db-prod merchforge-frontend-prod; do docker inspect -f '{{.Name}}  restarts={{.RestartCount}}  oom={{.State.OOMKilled}}  {{.State.Health.Status}}' "$c" 2>/dev/null; done; docker stats --no-stream --format '{{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}' | grep merchforge; echo "cpu:  user  nice  sys  iowait  steal  idle"; sar -u 1 1 | tail -1
```

### Reading it

| Field | Healthy | Act if |
|---|---|---|
| `restarts=` | `0` | any increase — a crash loop is starting |
| `oom=` | `false` | `true` — a memory cap is too tight |
| health | `healthy` | `unhealthy` for more than a minute |
| api CPU | ~1% idle | sustained >30% with no users |
| db CPU | <1% idle | sustained >25% with no users |
| frontend CPU | 0.00% | anything sustained — it only serves static files |
| api MEM | ~100M / 768M | approaching 768M |
| db MEM | ~80M / 512M | approaching 512M (see note below) |
| `steal` | varies wildly | nothing — this is the host's, not ours |

**`restarts=` and `oom=` matter most.** They are the two failure modes the
resource caps could plausibly introduce, and a plain `docker stats` snapshot
would not show either.

**On `steal`:** it has been observed between 0.5% and 57% *within 90 seconds* on
this host, with our containers idle throughout. It is time the hypervisor gave
our vCPU to a different virtual machine. Nothing inside this VM causes it and
nothing inside this VM can fix it. It is context, not a fault to chase.

**On db memory:** InnoDB's buffer pool is 256M (visible in the db container's
startup log) against a 512M cap. Resident memory is far lower today because the
pool fills lazily. If `oom=true` ever appears on the db container, this is why,
and the fix is raising that one cap in `docker-compose.prod.yml`.

---

## Idle database write rate

The Hangfire tuning (2 workers, 30s queue poll) was meant to cut a measured
~500 MB/hour of database writes with no users on the system. This measures
whether it did:

```bash
printf '%s  ' "$(date -u '+%F %T')"; docker stats --no-stream --format '{{.Name}} {{.BlockIO}}' | grep merchforge-db
```

Run it twice, an hour apart, with nobody using the site. The second number in
`BlockIO` is cumulative writes; the difference between the two readings is the
hourly rate.

| Delta over one hour | Meaning |
|---|---|
| under ~150 MB | Working as intended |
| 150–300 MB | Improved. Next lever is `SchedulePollingInterval` (below) |
| near 500 MB | The change did not take effect — investigate |

Do not measure across a container restart or a deploy: startup, migrations, and
InnoDB buffer-pool load all write heavily and will make an ordinary hour look
terrible.

**The remaining lever**, if the rate is still high: Hangfire's
`SchedulePollingInterval` is still at its 15-second default, so it checks for due
recurring jobs 240 times an hour to find nothing 239 of those times. The only
recurring job here runs hourly, so raising it to 60s costs nothing that matters.

---

## After a deploy

```bash
docker ps --filter name=merchforge --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
```

Expect:

```
merchforge-frontend-prod   Up ... (healthy)   127.0.0.1:8080->80/tcp
merchforge-api-prod        Up ... (healthy)   8080/tcp
merchforge-db-prod         Up ... (healthy)   3306/tcp
```

**The frontend must show `127.0.0.1:8080`, not `0.0.0.0:8080`.** The public
binding was removed because it let the internet reach the app directly —
bypassing Cloudflare, Bot Fight Mode, TLS, and the host vhost. Port changes only
apply when a container is *recreated*, so `docker restart` will silently keep the
old binding; `docker compose up -d` recreates properly.

Security headers:

```bash
curl -sI https://merchforge.site/ | grep -iE "x-content-type|x-frame|strict-transport|referrer"
```

Four lines expected. Fewer means a stale frontend image — check that CI finished
publishing before the deploy ran.

The api is expected to sit at **30–50% CPU for the first few minutes** after a
deploy. That is .NET warming up: JIT, EF Core model building, Hangfire schema
init. It should settle to ~1% within ten minutes.

---

## When something is wrong

### A container is restarting

```bash
docker inspect -f '{{.Name}} restarts={{.RestartCount}} exit={{.State.ExitCode}} oom={{.State.OOMKilled}}' merchforge-api-prod merchforge-db-prod merchforge-frontend-prod
docker logs merchforge-api-prod --tail 100
```

### Errors in the API

```bash
docker logs merchforge-api-prod 2>&1 | grep -iE "error|exception|fail" | tail -40
```

Two warnings are expected and benign:

- **DataProtection keys not persisted** — nothing in this codebase uses
  `IDataProtector`; JWTs are signed with a configured secret and refresh tokens
  live in the database. Verified by grep, not assumed.
- **"Failed to determine the https port for redirect"** — the container only
  listens on HTTP by design; the host vhost terminates TLS and does the redirect.

### Rate limiting is rejecting real users

```bash
docker logs merchforge-frontend-prod 2>&1 | grep " 429 " | tail -20
```

Should be empty. If real users appear here, either the nginx zone (30r/s, burst
60) or the ASP.NET dashboard policy (180/min per business) is too tight.

Confirm the real client IP is being read — access log lines should start with
visitor addresses, not `172.x` or `10.x`:

```bash
docker logs merchforge-frontend-prod --tail 20
```

If every line shows a Docker gateway address, `CF-Connecting-IP` is not arriving
and the rate limit has collapsed into one shared bucket for the whole internet.

### High CPU on the host — whose is it?

This is the question the September outage got wrong. Answer it in this order:

```bash
sar -u 1 1 | tail -1
```

- **High `%user`** → a workload on this box is busy. Find it:
  ```bash
  docker stats --no-stream | sort -k3 -h -r | head -10
  ps -eo pid,cmd,%cpu --sort=-%cpu | head -10
  ```
  Remember this host runs ~26 containers across a dozen unrelated applications.
  Ours are three of them.

- **High `%steal`, low `%user`** → the hypervisor is taking the CPU. Not ours,
  not fixable from inside the VM. This is what the September incident actually
  was.

Never use `100 - idle` as "CPU usage" — it counts steal, iowait and kernel time
as though they were application load. That single mistake is what caused a
healthy stack to be deleted from this host.

### Historical evidence, after the fact

```bash
sar -u -f /var/log/sysstat/sa$(date -u +%d) | tail -30      # today, 10-min granularity
last -x reboot shutdown | head -20                           # reboots; "still running" on a
                                                             # replaced boot = unclean halt
journalctl -u docker --since "2 days ago" --no-pager | grep -iE "oom|kill|restart|died"
dmesg -T | grep -iE "oom|killed process"                     # OOM kills
```

`/root/monitor/log.txt` holds five-minute `docker stats` snapshots going back
months — the single most useful artefact on this host for reconstructing what a
container was doing at a given moment.

---

## What the resource caps do and do not protect against

Worth being precise, because it is easy to over-read.

**Protected:** MerchForge can no longer take the host down. The three containers
are capped at 1.5 of 2 cores and 1408 MiB total, enforced by the kernel. If our
code goes into a loop, it throttles at its ceiling instead of starving the other
twelve applications on this box.

**Not protected:** a *different* tenant saturating the host. Our cap constrains
us, not them. A Next.js build on a neighbouring site was measured at 135% CPU
while MerchForge was not even running.

**Not protected, and this is the important one:** hypervisor steal. If the host
starts giving our vCPU away, we receive less CPU no matter what our limits say.
Caps divide the CPU we are given; they cannot increase it. The September outage
was this case — the machine was **60% idle with a 0.66 load average** at its last
reading before it stopped, which is not a CPU exhaustion failure at all.

So the caps close a real gap and remove MerchForge from suspicion permanently.
They do not prevent a repeat of what actually happened. Only the host provider
can address that.
