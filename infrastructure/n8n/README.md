# n8n Workflows — SportsClubEventManager

This repository does **not** deploy its own n8n container, in any environment. It reuses the
existing shared homelab n8n instance (`n8n`, image `n8nio/n8n:latest`, `192.168.1.100:5678`).
`Notifications:N8n:Enabled` stays `false` everywhere except production, and Infrastructure tests
run against a WireMock.Net stub, never against a real n8n instance. See the full design at
`.claude/docs/sdlc/design/issue-37-workflows-n8n-notificaciones-eventos.md`.

## Workflow files

`workflows/*.json` are exported directly from the n8n UI (`Workflows > Download`) and versioned here
as the source of truth, per issue #37 AC "store n8n workflow definitions as code for version
control". They are built and tested directly in the shared instance, then exported — never
hand-written and imported, since n8n's export format embeds instance-specific credential IDs that
only exist once the credential has already been created there (see issue #37 discussion, 2026-07-13).

- [x] `registration-confirmed.json` — built, tagged, activated, tested end-to-end (email delivered to a real inbox, 2026-07-13).
- [x] `event-updated.json` — built, tagged, activated, tested end-to-end including multi-recipient
      fan-out via a Split Out node (`body.Recipients` → one `Send an Email` per recipient, verified
      with 2 recipients both received a separate email, 2026-07-13).
- [x] `event-cancelled.json` — same structure as `event-updated` (`EventChangedPayload`,
      `ChangeType: "cancelled"`), built and tested, 2026-07-13.
- [x] `event-reminder.json` — same Split Out pattern, `EventReminderPayload` with `IntervalHours`
      instead of `ChangeType`, built and tested, 2026-07-13.

All 4 workflows built, tagged, activated, and functionally tested end-to-end against real Brevo
sends.

## Manual runbook — homelab owner checklist

None of the steps below are code in this repository. They are manual actions against the shared
n8n instance, Cloudflare DNS, and Brevo, performed by the homelab owner. Mirrored as a checklist on
[issue #37](https://github.com/AlejBlasco/SportsClubEventManager/issues/37) for tracking.

- [x] **1. Header Auth credential** — Created in n8n (`X-N8n-Webhook-Token`), attached to the
      Webhook node of `registration-confirmed`. Token generated 2026-07-13, stored in the homelab
      owner's password manager — reuse the same value for the remaining 3 workflows and for
      `N8N_WEBHOOK_TOKEN` in production `.env` later.
- [x] **2. Build/import the 4 workflows** — all 4 done (`registration-confirmed`, `event-updated`,
      `event-cancelled`, `event-reminder`). Note: the "Split Out" node's "Destination Field Name"
      option must be set explicitly (e.g. `recipient`) — left at its default, it reuses the literal
      "Field to Split Out" string (`body.Recipients`, dot included) as the output key name, which
      breaks any `{{ $json.Email }}`-style expression downstream.
- [x] **3. Tag the 4 workflows** with `sportsclubeventmanager` — all 4 done.
- [x] **4. Verify a Brevo sender subdomain by DNS in Cloudflare** — `notifications.ablasco.com`.
      SPF, DKIM (2 selectors), and DMARC (`p=none`) all published and verified; Brevo confirms
      "compliant with Google, Yahoo, and Microsoft's requirements" (2026-07-13). Note: domain
      authentication alone wasn't enough — also had to add a **Sender** in Brevo
      (`notificaciones@notifications.ablasco.com`) and delete the old free-domain sender inherited
      from account signup, which was itself flagged as non-compliant.
- [x] **5. Configure the Brevo credential in n8n** — SMTP credential created (`smtp-relay.brevo.com:465`,
      SSL/TLS), tested via a real send, confirmed working.
- [ ] **6. Copy the 4 real webhook URLs** into production `.env` — waiting until the application code
      exists and all 4 workflows are built (only `registration-confirmed`'s URL exists today).
- [x] **7. Functional verification (AC 5)** — all 4 done, tested via manual webhook calls with the
      exact PascalCase payload shape the .NET code will send; delivered to a real Hotmail inbox
      (landed in Spam — expected for a brand-new sending domain with no reputation yet, not a
      configuration issue).
- [ ] **8. End-to-end verification** — register a real test user for a test event in production (or
      against the real n8n instance over Tailscale), confirm the execution shows success in
      `n8n > Executions` and the email arrives.
- [ ] **9. Error-monitoring verification (AC 6)** — deliberately break something (e.g. temporarily
      disable the Brevo credential), confirm the failed execution is visible in
      `n8n > Executions`, matching the `sportsclubeventmanager_workflow_notifications_total{result="failure"}`
      counter and `LogWarning` already emitted on the API side.

**What NOT to do**: never expose the n8n instance (admin UI or webhooks) through the public reverse
proxy / Cloudflare Tunnel used by `web` and Grafana's public dashboard — n8n's public exposure is
explicitly out of scope for issue #37.

Turns out steps 1, 3, 4, 5, and 7 don't actually require the application code to exist first — the
design doc already fixes the exact JSON payload shape for each workflow, so each one can be built
and tested against a manual payload ahead of the .NET implementation. Only steps 6, 8, and 9 need
the real Api code calling these webhooks for real.
