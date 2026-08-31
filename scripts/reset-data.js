// ─────────────────────────────────────────────────────────────────────────────
//  Clears every ZapChat record while KEEPING collections and their indexes.
//
//  deleteMany({}) rather than drop(): dropping a collection takes its indexes with it,
//  and several of those indexes are business rules, not optimisations — one vote per
//  person per poll, one report per person per message, and the TTLs that expire refresh
//  tokens, OTPs and presence. They are recreated on service startup, but only if a
//  service happens to restart, so dropping leaves a window where those guarantees are
//  simply absent.
//
//  Usage:  mongosh --quiet mongodb://localhost:27017 --file scripts/reset-data.js
// ─────────────────────────────────────────────────────────────────────────────

const databases = [
  "zapchat_auth",
  "zapchat_chat",
  "zapchat_privatechat",
  "zapchat_polls",
  "zapchat_notifications",
  "zapchat_admin",
];

print("ZapChat data reset");
print("=".repeat(62));

let totalRemoved = 0;
let indexesKept = 0;

for (const name of databases) {
  const database = db.getSiblingDB(name);
  const collections = database.getCollectionNames().sort();

  if (collections.length === 0) {
    print(`  ${name}: no collections`);
    continue;
  }

  print(`\n  ${name}`);

  for (const collection of collections) {
    const handle = database.getCollection(collection);
    const before = handle.countDocuments();
    const indexes = handle.getIndexes().length;

    if (before === 0) {
      print(`    ${collection.padEnd(18)} already empty        (${indexes} indexes kept)`);
      indexesKept += indexes;
      continue;
    }

    handle.deleteMany({});
    const after = handle.countDocuments();

    print(
      `    ${collection.padEnd(18)} ${String(before).padStart(5)} -> ${after}` +
      `        (${indexes} indexes kept)`,
    );

    totalRemoved += before - after;
    indexesKept += indexes;
  }
}

print("\n" + "=".repeat(62));
print(`  removed ${totalRemoved} documents, kept ${indexesKept} indexes`);
print("=".repeat(62));
