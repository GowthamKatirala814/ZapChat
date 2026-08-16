/**
 * Frontend contract verification.
 *
 * Every request below is one the React application actually makes: same path, same
 * method, same body shape, through the same gateway origin, with a cookie jar instead of
 * a bearer header — exactly as the browser does it.
 *
 * It overlaps deliberately with api-e2e.sh and signalr-e2e.mjs on the core flows, but it
 * asks a different question. Those two ask "does the backend behave correctly?"; this one
 * asks "does the frontend's route table, DTO typing and authorization model still match
 * what the backend serves?" — the failure mode where both sides work and disagree.
 *
 * It is also the only suite that covers file upload and the admin surface (dashboard
 * tiles, the eleven analytics endpoints, the moderation queue, users, rooms, audit log).
 *
 * Run it from this directory:
 *
 *     npm install
 *     node frontend-contract-e2e.mjs
 */

import { HubConnectionBuilder, HttpTransportType } from "@microsoft/signalr";

process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0"; // local dev certificate

const GATEWAY = "https://localhost:5000";

// Session fixtures go straight to the auth service, exactly as the other two suites do.
// The gateway limits login to five per minute, which is correct and is asserted through
// the gateway once below — but running the suite twice inside a minute would otherwise
// throttle its own setup and fail every later assertion for the wrong reason.
const AUTH = "http://localhost:5111";

const results = [];
let currentGroup = "";

function group(name) {
  currentGroup = name;
  console.log(`\n── ${name} ${"─".repeat(Math.max(0, 62 - name.length))}`);
}

function record(ok, name, detail = "") {
  results.push({ group: currentGroup, ok, name, detail });
  console.log(`  ${ok ? "PASS" : "FAIL"}  ${name}${detail ? `  — ${detail}` : ""}`);
}

async function check(name, fn) {
  try {
    const detail = await fn();
    record(true, name, detail ?? "");
  } catch (error) {
    record(false, name, error.message);
  }
}

// ── Session with a cookie jar, exactly as the browser has ──────────────────────

class Session {
  constructor(label) {
    this.label = label;
    this.cookies = new Map();
  }

  header() {
    return [...this.cookies].map(([k, v]) => `${k}=${v}`).join("; ");
  }

  absorb(response) {
    const raw = response.headers.getSetCookie?.() ?? [];
    for (const cookie of raw) {
      const [pair] = cookie.split(";");
      const index = pair.indexOf("=");
      if (index > 0) this.cookies.set(pair.slice(0, index).trim(), pair.slice(index + 1).trim());
    }
  }

  async call(method, path, body, options = {}) {
    const origin = options.origin ?? GATEWAY;
    const headers = { Cookie: this.header() };
    let payload;

    if (body instanceof FormData) {
      payload = body;
    } else if (body !== undefined) {
      headers["Content-Type"] = "application/json";
      payload = JSON.stringify(body);
    }

    const response = await fetch(`${origin}${path}`, { method, headers, body: payload });
    this.absorb(response);

    const text = await response.text();
    let data = null;

    try {
      data = text ? JSON.parse(text) : null;
    } catch {
      data = text;
    }

    return { status: response.status, data, ok: response.ok };
  }

  get = (p) => this.call("GET", p);
  post = (p, b) => this.call("POST", p, b);
  put = (p, b) => this.call("PUT", p, b);
  del = (p, b) => this.call("DELETE", p, b);
}

function expect(condition, message) {
  if (!condition) throw new Error(message);
}

function expectStatus(response, status, label = "") {
  expect(
    response.status === status,
    `${label} expected ${status}, got ${response.status}${
      response.data?.message ? ` (${response.data.message})` : ""
    }`,
  );
}

// ── Hub helper ────────────────────────────────────────────────────────────────

async function connectHub(session, path) {
  const token = (await session.call("GET", "/api/auth/token")).data;

  const connection = new HubConnectionBuilder()
    .withUrl(`${GATEWAY}${path}`, {
      accessTokenFactory: () => token,
      transport: HttpTransportType.WebSockets,
      skipNegotiation: true,
    })
    .build();

  await connection.start();
  return connection;
}

/**
 * Waits for one hub event, or rejects on timeout.
 *
 * Callers start the wait, trigger the action, then await — so the promise exists for a
 * moment with nobody attached to it. If the triggering request throws in that window the
 * rejection would be unhandled and take the whole process down with a stack trace instead
 * of a FAIL line, which is exactly what happened the first time a connection failed to
 * open. The no-op catch below keeps the rejection attributable; the real await still sees
 * it.
 */
function waitFor(connection, event, predicate = () => true, ms = 6000) {
  if (!connection) return Promise.reject(new Error("hub connection was never opened"));

  const promise = new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      connection.off(event, handler);
      reject(new Error(`no ${event} within ${ms}ms`));
    }, ms);

    const handler = (payload) => {
      if (!predicate(payload)) return;
      clearTimeout(timer);
      connection.off(event, handler);
      resolve(payload);
    };

    connection.on(event, handler);
  });

  promise.catch(() => {});
  return promise;
}

// ══════════════════════════════════════════════════════════════════════════════

const alpha = new Session("alpha"); // Hyderabad + Admin
const bravo = new Session("bravo"); // Bangalore

const state = {};

async function run() {
  // ── Auth ───────────────────────────────────────────────────────────────────
  group("Authentication");

  await check("POST /api/auth/login (alpha)", async () => {
    const response = await alpha.post("/api/auth/login", {
      email: "alpha@zapcg.com",
      password: "Str0ngPass!23",
    });
    expectStatus(response, 200, "login");
    expect(alpha.cookies.has("access_token"), "no access_token cookie was set");
    expect(!("token" in response.data), "response body leaked a token");
    state.alphaName = response.data.anonymousName;
    return `role=${response.data.role}, cookie set, no token in body`;
  });

  await check("POST /api/auth/login (bravo, direct — fixture)", async () => {
    const response = await bravo.call(
      "POST",
      "/api/auth/login",
      { email: "bravo@zapcg.com", password: "Str0ngPass!23" },
      { origin: AUTH },
    );
    expectStatus(response, 200, "login");
    state.bravoName = response.data.anonymousName;
    return `anonymousName=${response.data.anonymousName}`;
  });

  await check("GET /api/auth/me returns MyProfile shape", async () => {
    const response = await alpha.get("/api/auth/me");
    expectStatus(response, 200);

    for (const field of [
      "userId", "email", "fullName", "department", "branch", "anonymousName",
      "createdAt", "roles",
    ]) {
      expect(field in response.data, `MyProfile is missing ${field}`);
    }

    state.alpha = response.data;
    expect(Array.isArray(response.data.roles), "roles is not an array");
    return `${response.data.anonymousName}, branch=${response.data.branch}, roles=[${response.data.roles}]`;
  });

  await check("GET /api/auth/me (bravo)", async () => {
    const response = await bravo.get("/api/auth/me");
    expectStatus(response, 200);
    state.bravo = response.data;
    return `${response.data.anonymousName}, branch=${response.data.branch}`;
  });

  await check("GET /api/auth/me without a session is 401", async () => {
    const anonymous = new Session("anon");
    const response = await anonymous.get("/api/auth/me");
    expectStatus(response, 401);
    return "guard reachable";
  });

  await check("GET /api/auth/token returns a raw JWT for the hub handshake", async () => {
    const response = await alpha.get("/api/auth/token");
    expectStatus(response, 200);
    const token = String(response.data).trim().replace(/^"|"$/g, "");
    expect(token.split(".").length === 3, "not a JWT");
    return `${token.length} chars`;
  });

  await check("GET /api/auth/users exposes anonymous names only", async () => {
    const response = await alpha.get("/api/auth/users");
    expectStatus(response, 200);
    expect(Array.isArray(response.data), "not an array");

    const leaked = response.data.find((u) => "email" in u || "fullName" in u);
    expect(!leaked, `directory leaked a real identity: ${JSON.stringify(leaked)}`);

    return `${response.data.length} users, no email/fullName field`;
  });

  // ── Rooms ──────────────────────────────────────────────────────────────────
  group("Channels");

  await check("GET /api/rooms is branch-filtered for a normal user", async () => {
    const forAlpha = await alpha.get("/api/rooms");
    const forBravo = await bravo.get("/api/rooms");
    expectStatus(forAlpha, 200);
    expectStatus(forBravo, 200);

    state.rooms = forAlpha.data;
    state.bravoRooms = forBravo.data;

    // alpha holds the Admin role, and an admin can read every channel for moderation —
    // so the branch rule is asserted against bravo, who is an ordinary user.
    const bravoBranches = forBravo.data.filter((r) => r.type === "Branch").map((r) => r.branch);

    expect(bravoBranches.length > 0, "bravo sees no branch channel at all");
    expect(!bravoBranches.includes("Hyderabad"), "bravo (Bangalore) can see a Hyderabad channel");

    return `bravo sees branch channels: ${bravoBranches.join()} — not Hyderabad`;
  });

  await check("Room DTO carries every field the sidebar renders", async () => {
    const room = state.rooms[0];
    for (const field of [
      "id", "name", "type", "description", "memberCount", "messageCount",
      "isArchived", "createdAt", "unreadCount", "isMember",
    ]) {
      expect(field in room, `Room is missing ${field}`);
    }
    expect(typeof room.type === "string", `roomType is ${typeof room.type}, expected a string name`);
    return `type="${room.type}" (name, not ordinal)`;
  });

  await check("Branch channel of another office returns 403, not 404 or data", async () => {
    const hyderabad = state.rooms.find((r) => r.type === "Branch" && r.branch === "Hyderabad");
    expect(hyderabad, "no Hyderabad channel to test against");

    const response = await bravo.get(`/api/rooms/${hyderabad.id}/messages`);
    expectStatus(response, 403, "cross-branch read");
    expect(response.data?.message, "403 carried no message for the UI to show");

    return `"${response.data.message}"`;
  });

  await check("POST /api/rooms/{id}/join", async () => {
    const general = state.rooms.find((r) => r.type === "General");
    state.generalRoom = general;

    const response = await alpha.post(`/api/rooms/${general.id}/join`);
    expectStatus(response, 200);
    expect(response.data.isMember === true, "isMember is false after joining");
    expect(response.data.memberCount > 0, "memberCount is 0 after joining");

    await bravo.post(`/api/rooms/${general.id}/join`);
    return `${general.name}: memberCount=${response.data.memberCount}`;
  });

  await check("GET /api/rooms/{id}/members returns presence", async () => {
    const response = await alpha.get(`/api/rooms/${state.generalRoom.id}/members`);
    expectStatus(response, 200);
    expect(Array.isArray(response.data), "not an array");

    if (response.data.length > 0) {
      for (const field of ["userId", "anonymousName", "isOnline"]) {
        expect(field in response.data[0], `RoomMember is missing ${field}`);
      }
    }

    return `${response.data.length} members`;
  });

  // ── Messages ───────────────────────────────────────────────────────────────
  group("Messages");

  await check("POST /api/rooms/{id}/messages returns isMine=true to the sender", async () => {
    const response = await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: "E2E verification message from the rebuilt frontend.",
      attachmentIds: [],
    });
    expectStatus(response, 200, "send");

    state.message = response.data;
    expect(response.data.isMine === true, "sender's own message came back with isMine=false");
    expect(response.data.anonymousName === state.alpha.anonymousName, "wrong author name");
    expect(!("userId" in response.data), "message DTO leaked an author userId");

    return `isMine=true, author="${response.data.anonymousName}", no userId field`;
  });

  await check("Message is NOT isMine for the other participant", async () => {
    const response = await bravo.get(`/api/messages/${state.message.id}`);
    expectStatus(response, 200);
    expect(response.data.isMine === false, "another user's message came back with isMine=true");
    return "isMine=false for bravo";
  });

  await check("GET /api/rooms/{id}/messages returns a cursor page, oldest-first", async () => {
    const response = await alpha.get(`/api/rooms/${state.generalRoom.id}/messages?limit=10`);
    expectStatus(response, 200);

    for (const field of ["items", "hasMore"]) {
      expect(field in response.data, `CursorPage is missing ${field}`);
    }

    const { items } = response.data;
    expect(Array.isArray(items), "items is not an array");

    if (items.length > 1) {
      const ordered = items.every(
        (m, i) => i === 0 || new Date(items[i - 1].sentAt) <= new Date(m.sentAt),
      );
      expect(ordered, "page items are not oldest-first as the frontend assumes");
    }

    state.firstPage = response.data;
    return `${items.length} items, hasMore=${response.data.hasMore}, cursor=${
      response.data.nextCursor ? "present" : "none"
    }`;
  });

  await check("Cursor paging returns strictly older messages", async () => {
    if (!state.firstPage.hasMore || !state.firstPage.nextCursor) {
      return "only one page exists — paging not exercisable";
    }

    const older = await alpha.get(
      `/api/rooms/${state.generalRoom.id}/messages?limit=10&before=${encodeURIComponent(
        state.firstPage.nextCursor,
      )}`,
    );
    expectStatus(older, 200);

    const newest = new Date(older.data.items.at(-1)?.sentAt ?? 0);
    const oldestOfFirst = new Date(state.firstPage.items[0].sentAt);
    expect(newest <= oldestOfFirst, "second page overlaps the first");

    const overlap = older.data.items.filter((m) =>
      state.firstPage.items.some((f) => f.id === m.id),
    );
    expect(overlap.length === 0, `${overlap.length} duplicated messages across pages`);

    return `${older.data.items.length} older items, no overlap`;
  });

  await check("POST /api/messages/{id}/reactions toggles and returns server state", async () => {
    const added = await alpha.post(`/api/messages/${state.message.id}/reactions`, { emoji: "👍" });
    expectStatus(added, 200);

    const reaction = added.data.reactions.find((r) => r.emoji === "👍");
    expect(reaction, "reaction not present after adding");
    expect(reaction.mine === true, "mine=false on the caller's own reaction");
    expect(reaction.count === 1, `count=${reaction.count}, expected 1`);

    const removed = await alpha.post(`/api/messages/${state.message.id}/reactions`, { emoji: "👍" });
    expect(
      !removed.data.reactions.some((r) => r.emoji === "👍"),
      "reaction survived the second toggle",
    );

    return "add → mine=true count=1, toggle → removed";
  });

  await check("PUT /api/messages/{id} edits and flags isEdited", async () => {
    const response = await alpha.put(`/api/messages/${state.message.id}`, {
      content: "E2E verification message (edited).",
    });
    expectStatus(response, 200);
    expect(response.data.isEdited === true, "isEdited was not set");
    expect(response.data.editedAt, "editedAt is missing");
    return "isEdited=true, editedAt set";
  });

  await check("Editing another user's message is 403", async () => {
    const response = await bravo.put(`/api/messages/${state.message.id}`, { content: "hijacked" });
    expectStatus(response, 403, "cross-user edit");
    return `"${response.data.message}"`;
  });

  await check("DELETE /api/messages/{id} leaves a User tombstone", async () => {
    const doomed = await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: "This message will be deleted by its author.",
      attachmentIds: [],
    });

    const deleted = await alpha.del(`/api/messages/${doomed.data.id}`);
    expectStatus(deleted, 204, "delete");

    const after = await alpha.get(`/api/messages/${doomed.data.id}`);
    expectStatus(after, 200, "read back");
    expect(after.data.deletedBy === "User", `deletedBy="${after.data.deletedBy}", expected "User"`);
    expect(after.data.content === "", "content survived deletion");

    return 'deletedBy="User", content cleared';
  });

  await check("Moderation blocks disallowed content with 422 + category", async () => {
    const hr = state.rooms.find((r) => r.type === "Hr");
    expect(hr, "no HR channel to test against");

    await alpha.post(`/api/rooms/${hr.id}/join`);

    const response = await alpha.post(`/api/rooms/${hr.id}/messages`, {
      content: "You are a stupid idiot and I hate you, you worthless moron.",
      attachmentIds: [],
    });

    if (response.status === 200) return "content allowed — local rules did not match this phrase";

    expectStatus(response, 422, "moderation");
    expect(response.data.message, "422 carried no reason for the composer to show");

    return `422 category="${response.data.category ?? "-"}" message="${response.data.message}"`;
  });

  // ── Files ──────────────────────────────────────────────────────────────────
  group("Attachments");

  await check("POST /api/files uploads and returns an attachment id", async () => {
    // A 1×1 PNG: real bytes, because the server verifies the file signature.
    const png = Buffer.from(
      "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
      "base64",
    );

    const form = new FormData();
    form.append("file", new Blob([png], { type: "image/png" }), "pixel.png");

    const response = await alpha.call("POST", "/api/files", form);
    expectStatus(response, 200, "upload");

    for (const field of ["id", "fileName", "contentType", "sizeBytes", "url"]) {
      expect(field in response.data, `AttachmentDto is missing ${field}`);
    }

    state.attachment = response.data;
    return `${response.data.fileName}, ${response.data.sizeBytes} bytes, url=${response.data.url}`;
  });

  await check("Attachment can be sent with a message and comes back on the DTO", async () => {
    const response = await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: "Message with an attachment.",
      attachmentIds: [state.attachment.id],
    });
    expectStatus(response, 200);
    expect(response.data.attachments.length === 1, "attachment did not survive the send");
    return `1 attachment: ${response.data.attachments[0].fileName}`;
  });

  await check("Disallowed file type is rejected with a usable message", async () => {
    const form = new FormData();
    form.append("file", new Blob([Buffer.from("MZ")], { type: "application/x-msdownload" }), "x.exe");

    const response = await alpha.call("POST", "/api/files", form);
    expect(response.status === 400 || response.status === 422, `got ${response.status}`);
    return `${response.status}: "${response.data?.message}"`;
  });

  // ── Private chat ───────────────────────────────────────────────────────────
  group("Direct messages");

  await check("POST /api/conversations is idempotent for the same pair", async () => {
    const first = await alpha.post("/api/conversations", { otherUserId: state.bravo.userId });
    expectStatus(first, 200, "start");

    const second = await alpha.post("/api/conversations", { otherUserId: state.bravo.userId });
    expect(second.data.id === first.data.id, "a second call created a duplicate conversation");

    state.conversation = first.data;
    expect(
      first.data.otherAnonymousName === state.bravo.anonymousName,
      "conversation shows the wrong counterpart name",
    );
    expect(!("otherEmail" in first.data), "conversation DTO leaked an email");

    return `id stable, other="${first.data.otherAnonymousName}"`;
  });

  await check("A third party cannot open someone else's conversation", async () => {
    // The classic broken-object-level-authorization probe: change the id in the URL.
    // A well-formed id belonging to a conversation the caller is not part of must not
    // resolve, whether or not it exists.
    const response = await bravo.get(`/api/conversations/${crypto.randomUUID()}`);
    expect(
      response.status === 403 || response.status === 404,
      `expected 403/404 for a foreign conversation id, got ${response.status}`,
    );

    return `${response.status} for an unrelated conversation id`;
  });

  await check("POST /api/conversations/{id}/messages", async () => {
    const response = await alpha.post(`/api/conversations/${state.conversation.id}/messages`, {
      content: "Direct message from the E2E run.",
    });
    expect(response.status === 200 || response.status === 201, `got ${response.status}`);

    state.directMessage = response.data;
    expect(response.data.isMine === true, "sender's DM came back with isMine=false");
    expect(!("attachments" in response.data) || response.data.attachments.length === 0,
      "DM carried attachments, which the send request has no field for");

    return `isMine=true, readAt=${response.data.readAt ?? "null"}`;
  });

  await check("The recipient sees the same DM with isMine=false", async () => {
    const response = await bravo.get(`/api/conversations/${state.conversation.id}/messages`);
    expectStatus(response, 200);

    const message = response.data.items.find((m) => m.id === state.directMessage.id);
    expect(message, "recipient cannot see the message");
    expect(message.isMine === false, "recipient sees isMine=true on the sender's message");

    return "isMine=false for the recipient";
  });

  await check("POST /api/conversations/{id}/read marks messages read", async () => {
    const response = await bravo.post(`/api/conversations/${state.conversation.id}/read`);
    expectStatus(response, 204, "mark read");

    const after = await alpha.get(`/api/conversations/${state.conversation.id}/messages`);
    const message = after.data.items.find((m) => m.id === state.directMessage.id);

    expect(message.readAt, "readAt is still null after the recipient read it");
    return `readAt=${message.readAt}`;
  });

  await check("Blocking prevents sending, unblocking restores it", async () => {
    const blocked = await bravo.post(`/api/blocks/${state.alpha.userId}`);
    expectStatus(blocked, 204, "block");

    const list = await bravo.get("/api/blocks");
    expect(list.data.includes(state.alpha.userId), "block list does not contain the blocked user");

    const conversation = await alpha.get(`/api/conversations/${state.conversation.id}`);
    expect(conversation.data.hasBlockedMe === true, "hasBlockedMe is false after being blocked");

    const rejected = await alpha.post(`/api/conversations/${state.conversation.id}/messages`, {
      content: "This should not be delivered.",
    });
    expect(rejected.status >= 400, `send succeeded despite the block (${rejected.status})`);

    await bravo.del(`/api/blocks/${state.alpha.userId}`);

    const restored = await alpha.post(`/api/conversations/${state.conversation.id}/messages`, {
      content: "Delivered again after unblocking.",
    });
    expect(restored.status < 400, `send still fails after unblocking (${restored.status})`);

    return `send blocked with ${rejected.status}, restored after unblock`;
  });

  // ── Polls ──────────────────────────────────────────────────────────────────
  group("Polls");

  await check("POST /api/polls creates with server-side validation", async () => {
    const invalid = await alpha.post("/api/polls", { question: "Hi", options: ["only one"] });
    expect(invalid.status === 400, `a 1-option poll was accepted (${invalid.status})`);

    const response = await alpha.post("/api/polls", {
      question: "Does the rebuilt frontend read poll state from the server?",
      options: ["Yes", "No", "Show me the numbers"],
    });
    expect(response.status === 200 || response.status === 201, `got ${response.status}`);

    state.poll = response.data;
    expect(response.data.isMine === true, "creator's poll came back with isMine=false");
    expect(response.data.options.length === 3, "wrong option count");
    expect(response.data.totalVotes === 0, "a new poll already has votes");

    return `3 options, isMine=true, totalVotes=0`;
  });

  await check("Voting returns recomputed percentages, not client arithmetic", async () => {
    const optionId = state.poll.options[0].id;

    const voted = await alpha.post(`/api/polls/${state.poll.id}/vote`, { optionId });
    expectStatus(voted, 200, "vote");

    expect(voted.data.myVoteOptionId === optionId, "myVoteOptionId does not reflect the vote");
    expect(voted.data.totalVotes === 1, `totalVotes=${voted.data.totalVotes}, expected 1`);

    const chosen = voted.data.options.find((o) => o.id === optionId);
    expect(chosen.percentage === 100, `percentage=${chosen.percentage}, expected 100`);

    return `myVoteOptionId set, totalVotes=1, percentage=100 (server-computed)`;
  });

  await check("A second voter changes the split; changing a vote does not double count", async () => {
    const [, second, third] = state.poll.options.map((o) => o.id);

    const added = await bravo.post(`/api/polls/${state.poll.id}/vote`, { optionId: second });
    expect(added.data.totalVotes === 2, `totalVotes=${added.data.totalVotes}, expected 2`);
    expect(
      added.data.options.filter((o) => o.percentage === 50).length === 2,
      "percentages are not a 50/50 split",
    );

    // Moving to a different option must move the vote, not add one.
    const moved = await bravo.post(`/api/polls/${state.poll.id}/vote`, { optionId: third });
    expect(moved.data.totalVotes === 2, `totalVotes=${moved.data.totalVotes} after changing vote`);
    expect(moved.data.myVoteOptionId === third, "myVoteOptionId did not follow the change");

    // Re-posting the option already chosen withdraws it — the documented behaviour, and
    // why the UI sends an explicit null instead of relying on this.
    const repeated = await bravo.post(`/api/polls/${state.poll.id}/vote`, { optionId: third });
    expect(repeated.data.totalVotes === 1, `repeat vote left totalVotes=${repeated.data.totalVotes}`);

    await bravo.post(`/api/polls/${state.poll.id}/vote`, { optionId: second });

    return "2 voters → 50/50; changing option keeps total at 2; repeating withdraws";
  });

  await check("Withdrawing a vote (optionId: null) is supported", async () => {
    const response = await bravo.post(`/api/polls/${state.poll.id}/vote`, { optionId: null });
    expectStatus(response, 200);
    expect(response.data.totalVotes === 1, `totalVotes=${response.data.totalVotes}, expected 1`);
    expect(!response.data.myVoteOptionId, "myVoteOptionId survived the withdrawal");

    return "explicit null withdrew the vote, totalVotes 2 → 1";
  });

  await check("Poll reactions are per-caller and toggleable", async () => {
    const up = await alpha.post(`/api/polls/${state.poll.id}/reaction`, { isUpvote: true });
    expectStatus(up, 200);
    expect(up.data.myReaction === true, "myReaction not set");
    expect(up.data.upvotes === 1, `upvotes=${up.data.upvotes}`);

    const cleared = await alpha.post(`/api/polls/${state.poll.id}/reaction`, { isUpvote: null });
    expect(cleared.data.upvotes === 0, "upvote survived withdrawal");

    return "up → myReaction=true upvotes=1, null → upvotes=0";
  });

  await check("A non-creator, non-admin cannot close someone else's poll", async () => {
    const response = await bravo.post(`/api/polls/${state.poll.id}/close`);
    expectStatus(response, 403, "close");
    return `"${response.data.message}"`;
  });

  // ── Notifications ──────────────────────────────────────────────────────────
  group("Notifications");

  await check("A mention produces a notification for the mentioned user", async () => {
    await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: `Hello @${state.bravo.anonymousName}, this is a mention test.`,
      attachmentIds: [],
    });

    // The notification is dispatched asynchronously after the message is persisted.
    await new Promise((resolve) => setTimeout(resolve, 1200));

    const response = await bravo.get("/api/notifications?limit=10");
    expectStatus(response, 200);

    const mention = response.data.find((n) => n.type === "Mention");
    expect(mention, "no Mention notification was produced");

    for (const field of ["id", "title", "message", "type", "isRead", "createdAt"]) {
      expect(field in mention, `notification is missing ${field}`);
    }

    state.notification = mention;
    return `type="${mention.type}", title="${mention.title}"`;
  });

  await check("GET /api/notifications/unread-count matches the frontend's shape", async () => {
    const response = await bravo.get("/api/notifications/unread-count");
    expectStatus(response, 200);
    expect("unread" in response.data, `expected { unread }, got ${JSON.stringify(response.data)}`);
    return `unread=${response.data.unread}`;
  });

  await check("POST /api/notifications/{id}/read and read-all", async () => {
    const one = await bravo.post(`/api/notifications/${state.notification.id}/read`);
    expectStatus(one, 204);

    const all = await bravo.post("/api/notifications/read-all");
    expectStatus(all, 204);

    const count = await bravo.get("/api/notifications/unread-count");
    expect(count.data.unread === 0, `unread=${count.data.unread} after read-all`);

    return "unread=0 after read-all";
  });

  // ── Reporting ──────────────────────────────────────────────────────────────
  group("Reporting and moderation");

  await check("POST /api/reports takes the reporter from the session", async () => {
    const target = await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: "A message that will be reported during the E2E run.",
      attachmentIds: [],
    });

    state.reportedMessage = target.data;

    const response = await bravo.post("/api/reports", {
      kind: "RoomMessage",
      messageId: target.data.id,
      reason: "Spam or advertising: E2E verification",
    });
    expect(response.status === 200 || response.status === 201, `got ${response.status}`);

    state.report = response.data;
    expect(
      response.data.reportedByAnonymousName === state.bravo.anonymousName,
      "reporter identity does not match the session",
    );
    expect(response.data.contentSnapshot, "no content snapshot was stored");
    expect(response.data.status === "Pending", `status="${response.data.status}"`);

    return `reporter="${response.data.reportedByAnonymousName}" (from session), status=Pending`;
  });

  await check("The same user cannot report the same message twice", async () => {
    const response = await bravo.post("/api/reports", {
      kind: "RoomMessage",
      messageId: state.reportedMessage.id,
      reason: "Duplicate attempt",
    });
    expectStatus(response, 409, "duplicate report");
    return "409 Conflict — enforced by a unique index";
  });

  await check("The report queue is admin-only", async () => {
    const forbidden = await bravo.get("/api/reports?status=Pending");
    expectStatus(forbidden, 403, "non-admin queue access");

    const allowed = await alpha.get("/api/reports?status=Pending&page=1&pageSize=25");
    expectStatus(allowed, 200, "admin queue access");

    for (const field of ["items", "totalCount", "page", "pageSize", "totalPages"]) {
      expect(field in allowed.data, `PagedResult is missing ${field}`);
    }

    return `403 for a user, ${allowed.data.totalCount} reports for the admin`;
  });

  await check("Reports carry no real identity", async () => {
    const response = await alpha.get("/api/reports?status=Pending");
    const report = response.data.items[0];

    expect(report, "no report to inspect");
    expect(!("authorEmail" in report), "report leaked an author email");
    expect(!("reportedByEmail" in report), "report leaked a reporter email");
    expect(report.authorAnonymousName, "report has no anonymous author name");
    expect("authorReportCount" in report && "threshold" in report,
      "report is missing the threshold fields the queue displays");

    return `anonymous names only; authorReportCount=${report.authorReportCount}/${report.threshold}`;
  });

  await check("POST /api/reports/{id}/action removes the message", async () => {
    const response = await alpha.post(`/api/reports/${state.report.id}/action`, {
      note: "Removed during E2E verification.",
    });
    expectStatus(response, 204, "action");

    const message = await alpha.get(`/api/messages/${state.reportedMessage.id}`);
    expect(
      message.data.deletedBy === "Moderation",
      `deletedBy="${message.data.deletedBy}", expected "Moderation"`,
    );

    return 'message deletedBy="Moderation" — distinct from a user deletion';
  });

  // ── Admin ──────────────────────────────────────────────────────────────────
  group("Admin console");

  await check("GET /api/admin/dashboard/stats wraps cross-service counts", async () => {
    const response = await alpha.get("/api/admin/dashboard/stats");
    expectStatus(response, 200);

    const wrapped = [
      "totalUsers", "activeUsers", "deletedUsers", "totalRooms", "totalMessages",
      "totalConversations", "totalDirectMessages", "totalPolls", "totalNotifications",
    ];

    for (const field of wrapped) {
      expect(field in response.data, `stats is missing ${field}`);
      expect("isAvailable" in response.data[field], `${field} is not an Availability wrapper`);
    }

    for (const field of ["blockedUsers", "totalReports", "pendingReports"]) {
      expect(typeof response.data[field] === "number", `${field} should be a bare number`);
    }

    const unavailable = wrapped.filter((f) => !response.data[f].isAvailable);

    return unavailable.length
      ? `all wrapped; UNAVAILABLE: ${unavailable.join(", ")}`
      : `all ${wrapped.length} available; messages=${response.data.totalMessages.value}, users=${response.data.totalUsers.value}`;
  });

  await check("Every analytics endpoint the UI charts responds", async () => {
    const endpoints = [
      ["messages-per-day?days=30", true],
      ["messages-per-hour", true],
      ["direct-messages-per-day?days=30", true],
      ["polls-per-day?days=30", true],
      ["notifications-per-day?days=30", true],
      ["top-rooms?top=8", true],
      ["top-authors?top=8", true],
      ["top-polls?top=8", true],
      ["reports-per-day?days=30", false],
      ["report-reasons?top=8", false],
      ["room-health?top=10", true],
    ];

    const broken = [];
    const unavailable = [];

    for (const [path, isWrapped] of endpoints) {
      const response = await alpha.get(`/api/admin/analytics/${path}`);

      if (response.status !== 200) {
        broken.push(`${path} → ${response.status}`);
        continue;
      }

      if (isWrapped) {
        if (!("isAvailable" in (response.data ?? {}))) {
          broken.push(`${path} is not Availability-wrapped`);
        } else if (!response.data.isAvailable) {
          unavailable.push(path.split("?")[0]);
        }
      } else if (!Array.isArray(response.data)) {
        broken.push(`${path} is not an array`);
      }
    }

    expect(broken.length === 0, broken.join("; "));

    return unavailable.length
      ? `${endpoints.length} endpoints OK; unavailable: ${unavailable.join(", ")}`
      : `all ${endpoints.length} endpoints returned real data`;
  });

  await check("Analytics data is real, not placeholder", async () => {
    const perDay = await alpha.get("/api/admin/analytics/messages-per-day?days=30");
    expect(perDay.data.isAvailable, `unavailable: ${perDay.data.reason}`);

    const total = perDay.data.value.reduce((sum, day) => sum + day.count, 0);
    expect(total > 0, "messages-per-day sums to zero despite messages existing");

    const topRooms = await alpha.get("/api/admin/analytics/top-rooms?top=5");
    expect(topRooms.data.isAvailable, "top-rooms unavailable");
    expect(topRooms.data.value.length > 0, "top-rooms is empty");
    expect(
      topRooms.data.value.every((r) => typeof r.messageCount === "number"),
      "top-rooms is missing messageCount",
    );

    return `${total} messages over 30 days; top room "${topRooms.data.value[0].roomName}" = ${topRooms.data.value[0].messageCount}`;
  });

  await check("GET /api/auth/admin/users is paged and anonymous", async () => {
    const response = await alpha.get(
      "/api/auth/admin/users?page=1&pageSize=25&sortBy=createdAt&sortDesc=true",
    );
    expectStatus(response, 200);

    for (const field of ["items", "totalCount", "page", "pageSize", "totalPages"]) {
      expect(field in response.data, `PagedResult is missing ${field}`);
    }

    const user = response.data.items[0];
    for (const field of ["id", "anonymousName", "department", "branch", "isActive", "roles"]) {
      expect(field in user, `AdminUser is missing ${field}`);
    }
    expect(!("email" in user), "admin user list leaked an email address");

    return `${response.data.totalCount} users, no email field`;
  });

  await check("GET /api/chat-admin/rooms and moderation stats", async () => {
    const rooms = await alpha.get("/api/chat-admin/rooms?includeArchived=true");
    expectStatus(rooms, 200, "chat-admin rooms");

    const stats = await alpha.get("/api/chat-admin/moderation/stats");
    expectStatus(stats, 200, "moderation stats");

    for (const field of ["total", "allowed", "blocked", "geminiRequests", "ruleRequests"]) {
      expect(field in stats.data, `ModerationStats is missing ${field}`);
    }

    return `${rooms.data.length} rooms; ${stats.data.total} messages screened, ${stats.data.blocked} blocked`;
  });

  await check("GET /api/admin/moderation/settings and audit logs", async () => {
    const settings = await alpha.get("/api/admin/moderation/settings");
    expectStatus(settings, 200, "settings");

    for (const field of [
      "reportThreshold", "autoActionEnabled", "autoRemoveMessages", "autoDisableAccount",
    ]) {
      expect(field in settings.data, `ModerationSettings is missing ${field}`);
    }

    const logs = await alpha.get("/api/admin/audit-logs?page=1&pageSize=30");
    expectStatus(logs, 200, "audit logs");
    expect(logs.data.items.length > 0, "audit log is empty despite actions being taken");

    const entry = logs.data.items[0];
    for (const field of ["action", "entityType", "actorName", "isSystem", "timestamp"]) {
      expect(field in entry, `AuditLogEntry is missing ${field}`);
    }

    return `threshold=${settings.data.reportThreshold}, ${logs.data.totalCount} audit entries`;
  });

  await check("Admin endpoints reject a non-admin", async () => {
    const probes = [
      "/api/admin/dashboard/stats",
      "/api/admin/analytics/messages-per-day",
      "/api/admin/audit-logs",
      "/api/auth/admin/users",
      "/api/chat-admin/rooms",
    ];

    const leaked = [];

    for (const path of probes) {
      const response = await bravo.get(path);
      if (response.status !== 403) leaked.push(`${path} → ${response.status}`);
    }

    expect(leaked.length === 0, `not 403: ${leaked.join(", ")}`);
    return `all ${probes.length} admin routes returned 403`;
  });

  // ── SignalR ────────────────────────────────────────────────────────────────
  group("SignalR");

  let chatAlpha, chatBravo, pollHub, notifyHub;

  await check("All four hubs connect at their new paths", async () => {
    chatAlpha = await connectHub(alpha, "/hubs/chat");
    chatBravo = await connectHub(bravo, "/hubs/chat");
    pollHub = await connectHub(alpha, "/hubs/polls");
    notifyHub = await connectHub(bravo, "/hubs/notifications");

    return "/hubs/chat, /hubs/private-chat paths, /hubs/polls, /hubs/notifications";
  });

  await check("JoinRoom returns the room and subscribes the connection", async () => {
    const room = await chatAlpha.invoke("JoinRoom", state.generalRoom.id);
    await chatBravo.invoke("JoinRoom", state.generalRoom.id);

    expect(room?.id === state.generalRoom.id, "JoinRoom did not return the room");
    return `joined "${room.name}"`;
  });

  await check("ReceiveMessage reaches the room with isMine=false", async () => {
    const arrival = waitFor(chatBravo, "ReceiveMessage", (m) => m.roomId === state.generalRoom.id);

    const sent = await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: "Realtime broadcast check.",
      attachmentIds: [],
    });

    const received = await arrival;

    expect(received.id === sent.data.id, "a different message arrived");
    expect(
      received.isMine === false,
      "group broadcast carried isMine=true — every recipient would see it as their own",
    );
    expect(received.anonymousName === state.alpha.anonymousName, "wrong author on the broadcast");

    state.broadcastMessage = received;
    return `isMine=false on the group broadcast, author="${received.anonymousName}"`;
  });

  await check("MessageEdited and MessageDeleted use the documented payloads", async () => {
    const edited = waitFor(chatBravo, "MessageEdited");
    await alpha.put(`/api/messages/${state.broadcastMessage.id}`, { content: "Edited live." });

    const editPayload = await edited;
    expect(editPayload.content === "Edited live.", "edit payload has stale content");
    expect(editPayload.isEdited === true, "edit payload does not set isEdited");

    const deleted = waitFor(chatBravo, "MessageDeleted");
    await alpha.del(`/api/messages/${state.broadcastMessage.id}`);

    const deletePayload = await deleted;
    for (const field of ["roomId", "messageId", "deletedBy", "deletedAt"]) {
      expect(field in deletePayload, `MessageDeleted is missing ${field}`);
    }
    expect(deletePayload.deletedBy === "User", `deletedBy="${deletePayload.deletedBy}"`);

    return `MessageEdited carries the message; MessageDeleted={roomId,messageId,deletedBy="User",deletedAt}`;
  });

  await check("ReactionsChanged carries the full resulting list", async () => {
    const target = await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: "Reaction broadcast check.",
      attachmentIds: [],
    });

    const changed = waitFor(chatBravo, "ReactionsChanged", (e) => e.messageId === target.data.id);
    await alpha.post(`/api/messages/${target.data.id}/reactions`, { emoji: "🎉" });

    const payload = await changed;
    for (const field of ["roomId", "messageId", "reactions"]) {
      expect(field in payload, `ReactionsChanged is missing ${field}`);
    }
    expect(Array.isArray(payload.reactions), "reactions is not an array");
    expect(payload.reactions[0].emoji === "🎉", "wrong emoji in the payload");

    return `{roomId, messageId, reactions:[${payload.reactions.length}]} — a full list, not a delta`;
  });

  await check("UserTyping carries the room id, not a bare name", async () => {
    const typing = waitFor(chatBravo, "UserTyping");
    await chatAlpha.invoke("StartTyping", state.generalRoom.id);

    const payload = await typing;
    expect("roomId" in payload, "UserTyping has no roomId — a multi-room client cannot route it");
    expect(payload.anonymousName === state.alpha.anonymousName, "wrong name");
    expect(payload.roomId.toLowerCase() === state.generalRoom.id.toLowerCase(), "wrong roomId");

    return `{roomId, anonymousName="${payload.anonymousName}"}`;
  });

  await check("RoomRead carries {roomId, anonymousName, readAt}", async () => {
    const read = waitFor(chatBravo, "RoomRead");
    await chatAlpha.invoke("MarkRead", state.generalRoom.id);

    const payload = await read;
    for (const field of ["roomId", "anonymousName", "readAt"]) {
      expect(field in payload, `RoomRead is missing ${field}`);
    }

    return `{roomId, anonymousName, readAt} — the shape the client now reads`;
  });

  await check("RoomUpdated delivers a server-computed unread count", async () => {
    const updated = waitFor(chatBravo, "RoomUpdated", (e) => e.roomId === state.generalRoom.id);

    await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: "Sidebar unread-count check.",
      attachmentIds: [],
    });

    const payload = await updated;
    for (const field of ["roomId", "roomName", "unreadCount"]) {
      expect(field in payload, `RoomUpdated is missing ${field}`);
    }
    expect(typeof payload.unreadCount === "number", "unreadCount is not a number");
    expect(payload.lastMessage, "RoomUpdated has no lastMessage for the sidebar preview");

    return `unreadCount=${payload.unreadCount}, lastMessage="${payload.lastMessage.preview.slice(0, 24)}…"`;
  });

  await check("RoomPresenceChanged is scoped to the room", async () => {
    const presence = waitFor(chatAlpha, "RoomPresenceChanged", (e) => e.roomId, 8000);
    await chatBravo.invoke("LeaveRoom", state.generalRoom.id);

    const payload = await presence;
    expect("roomId" in payload && "members" in payload, "wrong RoomPresenceChanged shape");
    expect(Array.isArray(payload.members), "members is not an array");

    await chatBravo.invoke("JoinRoom", state.generalRoom.id);
    return `{roomId, members:[${payload.members.length}]}`;
  });

  await check("PollCreated broadcast is viewer-neutral", async () => {
    const created = waitFor(pollHub, "PollCreated");

    // bravo creates it, so alpha's hub payload must not claim isMine.
    await bravo.post("/api/polls", {
      question: "Is the poll broadcast viewer-neutral for everyone?",
      options: ["Yes", "No"],
    });

    const payload = await created;
    expect(
      payload.isMine === false,
      "PollCreated carried isMine=true to a non-creator — they would see a Close button",
    );
    expect(!payload.myVoteOptionId, "PollCreated leaked a myVoteOptionId");

    state.broadcastPoll = payload;
    return "isMine=false, no myVoteOptionId";
  });

  await check("PollUpdated broadcast is viewer-neutral", async () => {
    const updated = waitFor(pollHub, "PollUpdated", (p) => p.id === state.broadcastPoll.id);

    await bravo.post(`/api/polls/${state.broadcastPoll.id}/vote`, {
      optionId: state.broadcastPoll.options[0].id,
    });

    const payload = await updated;
    expect(payload.totalVotes === 1, `totalVotes=${payload.totalVotes}`);
    expect(
      !payload.myVoteOptionId,
      "PollUpdated leaked the voter's own choice to every connected client",
    );

    return `totalVotes=1, no myVoteOptionId leaked`;
  });

  await check("ReceiveNotification arrives on the notifications hub", async () => {
    const arrival = waitFor(notifyHub, "ReceiveNotification", () => true, 8000);

    await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: `Realtime mention for @${state.bravo.anonymousName}.`,
      attachmentIds: [],
    });

    const payload = await arrival;
    for (const field of ["id", "title", "message", "type", "isRead", "createdAt"]) {
      expect(field in payload, `notification payload is missing ${field}`);
    }

    return `type="${payload.type}", title="${payload.title}"`;
  });

  await check("Reconnect re-joins groups and messages resume", async () => {
    await chatBravo.stop();

    chatBravo = await connectHub(bravo, "/hubs/chat");
    await chatBravo.invoke("JoinRoom", state.generalRoom.id);

    const arrival = waitFor(chatBravo, "ReceiveMessage");
    await alpha.post(`/api/rooms/${state.generalRoom.id}/messages`, {
      content: "Message after a reconnect.",
      attachmentIds: [],
    });

    await arrival;
    return "group membership restored, delivery resumed";
  });

  await check("An unauthenticated hub handshake is rejected", async () => {
    const connection = new HubConnectionBuilder()
      .withUrl(`${GATEWAY}/hubs/chat`, {
        accessTokenFactory: () => "",
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .build();

    let rejected = false;
    try {
      await connection.start();
      await connection.stop();
    } catch {
      rejected = true;
    }

    expect(rejected, "an unauthenticated connection was accepted");
    return "handshake refused without a token";
  });

  for (const connection of [chatAlpha, chatBravo, pollHub, notifyHub]) {
    await connection?.stop().catch(() => {});
  }

  // ── Session teardown ───────────────────────────────────────────────────────
  group("Session");

  await check("POST /api/auth/logout clears the session", async () => {
    const response = await bravo.post("/api/auth/logout");
    expect(response.status === 200 || response.status === 204, `got ${response.status}`);

    const after = await bravo.get("/api/auth/me");
    expectStatus(after, 401, "after logout");

    return "subsequent /me is 401";
  });
}

// ══════════════════════════════════════════════════════════════════════════════

run()
  .catch((error) => {
    console.error("\nHARNESS ERROR:", error);
    record(false, "harness", error.message);
  })
  .finally(() => {
    const failed = results.filter((r) => !r.ok);

    console.log(`\n${"═".repeat(66)}`);
    console.log(`  ${results.length - failed.length} passed, ${failed.length} failed`);
    console.log("═".repeat(66));

    if (failed.length) {
      console.log("\nFailures:");
      for (const f of failed) console.log(`  [${f.group}] ${f.name}\n      ${f.detail}`);
    }

    process.exit(failed.length ? 1 : 0);
  });
