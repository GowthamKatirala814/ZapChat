/**
 * Populates a fresh ZapChat database with realistic demo content.
 *
 * Everything is created through the real HTTP API — registration, login, join, send,
 * react, vote — never by inserting documents. That matters for more than tidiness:
 * API-created records get properly hashed passwords, server-allocated pseudonyms,
 * moderation screening, unread fan-out, notification dispatch, and the exact document
 * shape the application writes. Hand-built documents drift from that shape the moment a
 * model changes, and they skip every side effect — so the app looks populated while
 * unread counts, mentions and read receipts stay empty.
 *
 * Talks to the services directly rather than the gateway: registration and login are
 * limited to five per minute per IP there, which is correct and would throttle a seeder.
 *
 * Requires the backend started with -EmailToLog, because it reads verification codes out
 * of logs/Auth.log.
 *
 *     node scripts/seed-demo-data.mjs
 */

import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const AUTH = "http://localhost:5111";
const CHAT = "http://localhost:5139";
const DM = "http://localhost:5172";
const POLLS_API = "http://localhost:5292";
const AUTH_LOG = join(ROOT, "logs", "Auth.log");

const PASSWORD = "Str0ngPass!23";

// ── HTTP with a per-user cookie jar ───────────────────────────────────────────

class Session {
  constructor(email) {
    this.email = email;
    this.cookies = new Map();
    this.profile = null;
  }

  async call(method, url, body) {
    const headers = {};

    if (this.cookies.size > 0) {
      headers.Cookie = [...this.cookies].map(([k, v]) => k + "=" + v).join("; ");
    }
    if (body !== undefined) headers["Content-Type"] = "application/json";

    const response = await fetch(url, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });

    for (const cookie of response.headers.getSetCookie?.() ?? []) {
      const [pair] = cookie.split(";");
      const eq = pair.indexOf("=");
      if (eq > 0) this.cookies.set(pair.slice(0, eq).trim(), pair.slice(eq + 1).trim());
    }

    const text = await response.text();
    let data = null;

    try {
      data = text ? JSON.parse(text) : null;
    } catch {
      data = text;
    }

    return { status: response.status, ok: response.ok, data };
  }

  get(url) {
    return this.call("GET", url);
  }

  post(url, body) {
    return this.call("POST", url, body);
  }
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const pick = (list, n) => list[Math.abs(n) % list.length];

/**
 * The newest verification code in the Auth log.
 *
 * Decoded as latin1, not utf8: a service killed mid-write can leave NUL bytes in the
 * file, and those have previously made tools treat it as binary. The code is ASCII
 * digits, so a byte-preserving decode always finds it.
 */
function latestCode() {
  const text = readFileSync(AUTH_LOG, "latin1");
  const matches = [...text.matchAll(/Your code is: (\d{6})/g)];
  return matches.length > 0 ? matches[matches.length - 1][1] : null;
}

// ── People ───────────────────────────────────────────────────────────────────

const PEOPLE = [
  { email: "alpha@zapcg.com", name: "Alpha Tester", dept: "Engineering", branch: "Hyderabad" },
  { email: "bravo@zapcg.com", name: "Bravo Tester", dept: "Product", branch: "Bangalore" },
  { email: "carol@zapcg.com", name: "Carol Tester", dept: "Design", branch: "Hyderabad" },
  { email: "priya.nair@zapcg.com", name: "Priya Nair", dept: "Engineering", branch: "Hyderabad" },
  { email: "rahul.menon@zapcg.com", name: "Rahul Menon", dept: "Engineering", branch: "Bangalore" },
  { email: "sneha.reddy@zapcg.com", name: "Sneha Reddy", dept: "Quality Assurance", branch: "Hyderabad" },
  { email: "arjun.rao@zapcg.com", name: "Arjun Rao", dept: "Product", branch: "Bangalore" },
  { email: "divya.krishnan@zapcg.com", name: "Divya Krishnan", dept: "Human Resources", branch: "Hyderabad" },
  { email: "kiran.kumar@zapcg.com", name: "Kiran Kumar", dept: "Operations", branch: "Bangalore" },
  { email: "meera.iyer@zapcg.com", name: "Meera Iyer", dept: "Design", branch: "Hyderabad" },
];

/**
 * Channel content.
 *
 * Written to read like an anonymous internal channel actually reads: candid feedback
 * people would not put their name to, mixed with ordinary logistics. Every line clears
 * the moderation dictionaries (profanity, bullying, threats, spam patterns, and the
 * confidential project codenames) — a blocked message is silently missing from the seed,
 * so content that trips the filter would quietly thin the result.
 */
const GENERAL = [
  "Morning everyone. Standup notes are on the shared drive if you missed the call.",
  "Genuine question: does anyone else find two-week sprints too short for anything involving a migration?",
  "Two weeks is fine for features. It is not fine for anything that touches the database.",
  "Coffee machine on the third floor is working again.",
  "Can we stop scheduling reviews at 5pm on Fridays? The attendance speaks for itself.",
  "Strongly agree. Nobody is doing their best thinking at that hour of the week.",
  "The laptop refresh has made more difference to my day than any process change this year.",
  "Anonymous appreciation: whoever fixed the flaky CI pipeline, thank you. It was ruining my mornings.",
  "Is there a reason we still have three separate places to log time?",
  "Parking has got noticeably worse since the new floor opened.",
  "Small thing, but the wifi in the north wing drops every afternoon around three.",
  "Reminder that the quarterly all-hands is next Thursday.",
];

const HR = [
  "Is the work-from-home policy being reviewed this quarter? The current wording is ambiguous about client weeks.",
  "Question on carrying leave into next year — is the cap per calendar year or per financial year?",
  "Would it be possible to publish the band structure? Not the numbers, just the framework.",
  "The onboarding checklist has not been updated since the tooling changed. New joiners are confused by it.",
  "Can we get clarity on notice periods for internal transfers versus external moves?",
  "The insurance provider changed, but the communication about it arrived after the enrolment deadline.",
];

const HYDERABAD = [
  "Anyone driving in from the Gachibowli side and open to carpooling?",
  "The cafeteria menu rotation here is much better than it was last quarter.",
  "Heads up: lift maintenance on Saturday, stairs only above the fourth floor.",
  "Is the Friday evening shuttle still at 6:30 or has it moved?",
];

const BANGALORE = [
  "Traffic on the Outer Ring Road was unusually bad this morning. Plan for an extra half hour.",
  "The new seating layout on our floor is a real improvement for focus work.",
  "Does anyone know whether the gym badge works for the other building as well?",
  "Standing desks arrived. First come first served, apparently.",
];

const POLL_DEFINITIONS = [
  {
    question: "What should we prioritise next quarter?",
    options: ["Reducing technical debt", "New features", "Performance and reliability", "Developer tooling"],
  },
  {
    question: "Preferred format for the weekly team sync?",
    options: ["Keep it as a call", "Written updates only", "Short call plus written detail"],
  },
  {
    question: "How do you feel about the current sprint length?",
    options: ["Too short", "About right", "Too long"],
  },
];

const DM_THREADS = [
  [
    "Do you have five minutes to look at the pull request when you get a chance?",
    "Sure, I will send comments shortly.",
    "No rush at all, tomorrow is fine.",
  ],
  [
    "Are you going to the all-hands in person or joining remotely?",
    "Remote — I have a client call right before it.",
    "Same here. See you on the call.",
  ],
  [
    "Did the deployment go through in the end?",
    "Yes, the second attempt worked once the config was corrected.",
    "Good. I will let the team know.",
  ],
];

// ── Steps ────────────────────────────────────────────────────────────────────

const log = (...parts) => console.log(...parts);

async function ensureUser(person) {
  const session = new Session(person.email);

  // A previous run may already have created this account.
  const existing = await session.post(AUTH + "/api/auth/login", {
    email: person.email,
    password: PASSWORD,
  });

  if (existing.status === 200) {
    session.profile = existing.data;
    return { session, created: false };
  }

  await session.post(AUTH + "/api/auth/register/initiate", {
    fullName: person.name,
    email: person.email,
    department: person.dept,
    branch: person.branch,
  });

  await sleep(1400);

  const code = latestCode();
  if (!code) throw new Error("no verification code appeared for " + person.email);

  const verified = await session.post(AUTH + "/api/auth/register/verify-otp", {
    email: person.email,
    otpCode: code,
  });

  if (!verified.data || !verified.data.token) {
    throw new Error("verification code rejected for " + person.email);
  }

  await session.post(AUTH + "/api/auth/register/complete", {
    verificationToken: verified.data.token,
    password: PASSWORD,
    confirmPassword: PASSWORD,
  });

  const login = await session.post(AUTH + "/api/auth/login", {
    email: person.email,
    password: PASSWORD,
  });

  if (login.status !== 200) throw new Error("login failed for " + person.email);

  session.profile = login.data;
  return { session, created: true };
}

async function fillChannel(room, lines, audience, label, reactions) {
  if (!room) {
    log("   " + label + ": channel missing, skipped");
    return;
  }

  const sent = [];

  for (const [index, content] of lines.entries()) {
    const author = pick(audience, index);
    const result = await author.session.post(CHAT + "/api/rooms/" + room.id + "/messages", {
      content,
      attachmentIds: [],
    });

    if (result.status === 200 && result.data && result.data.id) {
      sent.push(result.data.id);
    } else {
      // A moderation rejection is 422. Surfaced rather than swallowed, because a
      // silently dropped line makes the seed thinner than it reports.
      const detail = result.data && result.data.message ? result.data.message : "";
      log('   ! "' + content.slice(0, 38) + '..." -> HTTP ' + result.status + " " + detail);
    }
  }

  // Replies, so threads are exercised rather than only flat messages.
  let replies = 0;
  for (const offset of [1, 4]) {
    if (!sent[offset]) continue;

    const responder = pick(audience, offset + 3);
    const text = offset === 1
      ? "This matches my experience too, for what it is worth."
      : "Adding to this — it came up in our retro as well.";

    const result = await responder.session.post(CHAT + "/api/rooms/" + room.id + "/messages", {
      content: text,
      replyToMessageId: sent[offset],
      attachmentIds: [],
    });

    if (result.status === 200) replies++;
  }

  // Reactions drawn from the server's catalogue, spread unevenly so the UI shows
  // messages with none, one, and several.
  let applied = 0;
  for (const [index, messageId] of sent.entries()) {
    const howMany = index % 4 === 0 ? 3 : index % 3 === 0 ? 2 : index % 2 === 0 ? 1 : 0;

    for (let n = 0; n < howMany; n++) {
      const reactor = pick(audience, index + n + 1);
      const emoji = pick(reactions, index + n * 3);
      const result = await reactor.session.post(
        CHAT + "/api/messages/" + messageId + "/reactions",
        { emoji },
      );
      if (result.status === 200) applied++;
    }
  }

  log(
    "   " + label.padEnd(14) +
    String(sent.length).padStart(3) + " messages, " +
    replies + " replies, " +
    String(applied).padStart(2) + " reactions",
  );
}

async function main() {
  log("ZapChat demo data");
  log("=".repeat(66));

  // ── 1. Accounts ───────────────────────────────────────────────────────────
  log("\n1. Accounts");
  const users = [];

  for (const person of PEOPLE) {
    const { session, created } = await ensureUser(person);
    users.push({ ...person, session, anon: session.profile.anonymousName });
    log(
      "   " + (created ? "created" : "exists ") + "  " +
      person.email.padEnd(26) + " -> " + session.profile.anonymousName,
    );
  }

  const inBranch = (branch) => users.filter((u) => u.branch === branch);

  // ── 2. Reaction catalogue, from the server ────────────────────────────────
  const catalogue = await users[0].session.get(CHAT + "/api/rooms/reaction-options");
  const reactions = (catalogue.data ?? []).map((r) => r.emoji);
  log("\n2. Reactions published by the server: " + reactions.length);

  // ── 3. Channels ──────────────────────────────────────────────────────────
  log("\n3. Channels");
  const rooms = (await users[0].session.get(CHAT + "/api/rooms")).data ?? [];
  const room = (name) => rooms.find((r) => r.name === name);

  for (const r of rooms) log("   " + r.name.padEnd(16) + " type=" + r.type);

  // Everyone joins whatever they are allowed to see. Branch access is enforced by the
  // server, so a Bangalore account simply will not be offered the Hyderabad channel.
  for (const user of users) {
    const visible = (await user.session.get(CHAT + "/api/rooms")).data ?? [];
    for (const r of visible) {
      await user.session.post(CHAT + "/api/rooms/" + r.id + "/join");
    }
  }

  // ── 4. Messages, replies, reactions ──────────────────────────────────────
  log("\n4. Messages");
  await fillChannel(room("General Chat"), GENERAL, users, "General Chat", reactions);
  await fillChannel(room("HR Issues"), HR, users, "HR Issues", reactions);
  await fillChannel(room("Hyderabad"), HYDERABAD, inBranch("Hyderabad"), "Hyderabad", reactions);
  await fillChannel(room("Bangalore"), BANGALORE, inBranch("Bangalore"), "Bangalore", reactions);

  // ── 5. A mention, so the activity feed is not empty ──────────────────────
  log("\n5. Mentions");
  const general = room("General Chat");

  if (general) {
    const mentioner = users[3];
    const mentioned = users[5];

    const result = await mentioner.session.post(CHAT + "/api/rooms/" + general.id + "/messages", {
      content: "@" + mentioned.anon + " did you get anywhere with the test flakiness? Happy to pair on it.",
      attachmentIds: [],
    });

    log("   " + mentioner.anon + " mentioned " + mentioned.anon + " -> HTTP " + result.status);
  }

  // ── 6. Direct messages ──────────────────────────────────────────────────
  log("\n6. Direct messages");

  for (const [n, [aIndex, bIndex]] of [[0, 1], [3, 5], [6, 8]].entries()) {
    const a = users[aIndex];
    const b = users[bIndex];

    const started = await a.session.post(DM + "/api/conversations", {
      otherUserId: b.session.profile.userId,
    });

    if (started.status !== 200) {
      log("   ! " + a.anon + " <-> " + b.anon + " -> HTTP " + started.status);
      continue;
    }

    const conversationId = started.data.id;
    const thread = pick(DM_THREADS, n);

    for (const [turn, content] of thread.entries()) {
      const speaker = turn % 2 === 0 ? a : b;
      await speaker.session.post(DM + "/api/conversations/" + conversationId + "/messages", { content });
    }

    // The recipient reads it, so read receipts are populated rather than all-unread.
    await b.session.post(DM + "/api/conversations/" + conversationId + "/read");

    log("   " + a.anon + " <-> " + b.anon + ": " + thread.length + " messages, read");
  }

  // ── 7. Polls ────────────────────────────────────────────────────────────
  log("\n7. Polls");

  for (const [index, definition] of POLL_DEFINITIONS.entries()) {
    const creator = pick(users, index + 2);
    const created = await creator.session.post(POLLS_API + "/api/polls", definition);

    if (created.status !== 200 && created.status !== 201) {
      log('   ! "' + definition.question.slice(0, 30) + '" -> HTTP ' + created.status);
      continue;
    }

    const poll = created.data;
    let votes = 0;
    let opinions = 0;

    for (const [n, voter] of users.entries()) {
      // Deliberately not unanimous — a poll where everyone voted the same way tells the
      // UI nothing about how it renders a split.
      if ((n + index) % 3 === 0) continue;

      const option = pick(poll.options, n + index);
      const result = await voter.session.post(POLLS_API + "/api/polls/" + poll.id + "/vote", {
        optionId: option.id,
      });
      if (result.status === 200) votes++;

      if (n % 4 === 0) {
        const reaction = await voter.session.post(
          POLLS_API + "/api/polls/" + poll.id + "/reaction",
          { isUpvote: n % 8 === 0 },
        );
        if (reaction.status === 200) opinions++;
      }
    }

    log('   "' + definition.question.slice(0, 42).padEnd(42) + '" ' + votes + " votes, " + opinions + " reactions");
  }

  log("\n" + "=".repeat(66));
  log("  " + users.length + " accounts, all with the password: " + PASSWORD);
  log("=".repeat(66));
}

main().catch((error) => {
  console.error("\nSEED FAILED: " + error.message);
  process.exit(1);
});
