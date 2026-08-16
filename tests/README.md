# ZapChat end-to-end tests

Three black-box suites that drive the running platform through the API gateway. They are
not unit tests: they exist to answer "does the feature actually work", end to end, against
real MongoDB data.

| Suite | Assertions | Question it answers |
|---|---|---|
| `api-e2e.sh` | 70 | Does the backend behave correctly? Auth, rooms, messaging, private chat, polls, notifications, reporting, session lifecycle, rate limiting. |
| `signalr-e2e.mjs` | 25 | Does realtime work? All four hubs with two live clients: delivery, payload shapes, access control, read receipts, reconnection. |
| `frontend-contract-e2e.mjs` | 66 | Does the frontend still agree with the backend? Route table, DTO fields, `isMine` semantics, `Availability` wrappers, viewer-neutral broadcasts — plus file upload and the whole admin surface, which the other two do not cover. |

The third suite overlaps the first two on the core flows on purpose: it is checking a
different failure mode, where both sides work correctly and disagree with each other.

## Running

Start the backend first:

```powershell
. .\scripts\dev-env.ps1
.\scripts\start-backend.ps1
```

Then, from the repository root:

```bash
bash tests/api-e2e.sh
```

```bash
cd tests
npm install                        # once — installs the SignalR client
node signalr-e2e.mjs
node frontend-contract-e2e.mjs
```

## Notes

* **Why `tests/` has its own `package.json`.** The two Node suites need a real hub client.
  Node resolves bare ESM imports relative to the importing *file*, not the working
  directory, so running it from `frontend/` could never resolve `@microsoft/signalr` out of
  that project's `node_modules`. Declaring the dependency here makes the suite runnable on
  its own.

* **Fixtures bypass the gateway.** Registration and login for the test users go straight to
  the auth service on port 5111. The gateway's strict five-per-minute limiter on those
  routes is correct and is exercised deliberately in section 16 of `api-e2e.sh` — it must
  not throttle test setup.

* **OTP codes are read from the log.** `api-e2e.sh` greps `logs/Auth.log`, which works
  because the development mail transport writes codes there instead of sending email
  (`ZAPCHAT_EMAIL__USELOGTRANSPORT=true`). The path is resolved relative to the repository,
  so the suite runs on any machine.

* `signalr-e2e.mjs` and `frontend-contract-e2e.mjs` expect `alpha@zapcg.com` and
  `bravo@zapcg.com` with the password `Str0ngPass!23`, created by a previous `api-e2e.sh`
  run or by hand. `alpha` must hold the Admin role for the admin assertions to pass — set
  `ZAPCHAT_ADMINSETTINGS__ADMINEMAIL` before that account signs in.

* All three suites are safe to re-run; they create uniquely named users each time.
