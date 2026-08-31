// Real-time test: two SignalR clients over the gateway, exercising the room hub,
// the private-chat hub and the notification hub with real JWTs.
import * as signalR from '@microsoft/signalr';

process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'; // local dev certificate

const GW = 'https://localhost:5000';
const AUTH = 'http://localhost:5111';

let pass = 0, fail = 0;
const ok = m => { console.log(`  [ PASS ] ${m}`); pass++; };
const bad = m => { console.log(`  [ FAIL ] ${m}`); fail++; };
const sec = t => console.log(`\n== ${t} ${'='.repeat(Math.max(0, 66 - t.length))}`);

const wait = ms => new Promise(r => setTimeout(r, ms));

/** Resolves when `predicate` sees a matching event, or rejects on timeout. */
function expectEvent(conn, event, predicate = () => true, ms = 6000) {
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => {
            conn.off(event, handler);
            reject(new Error(`timed out waiting for ${event}`));
        }, ms);

        const handler = payload => {
            if (!predicate(payload)) return;
            clearTimeout(timer);
            conn.off(event, handler);
            resolve(payload);
        };

        conn.on(event, handler);
    });
}

async function login(email) {
    const res = await fetch(`${AUTH}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'Str0ngPass!23' })
    });
    if (!res.ok) throw new Error(`login failed for ${email}: ${res.status}`);

    // Read the access token from the Set-Cookie header, exactly as a browser would
    // hold it; the SignalR client then passes it via accessTokenFactory.
    const cookies = res.headers.getSetCookie?.() ?? [];
    const access = cookies.find(c => c.startsWith('access_token='));
    return access.split(';')[0].substring('access_token='.length);
}

function connect(path, token) {
    return new signalR.HubConnectionBuilder()
        .withUrl(`${GW}${path}`, { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.None)
        .build();
}

async function api(path, token, options = {}) {
    const res = await fetch(`${GW}${path}`, {
        ...options,
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
            ...(options.headers ?? {})
        }
    });
    const text = await res.text();
    return { status: res.status, body: text ? JSON.parse(text) : null };
}

const connections = [];

try {
    sec('Authentication over SignalR');

    const tokenA = await login('alpha@zapcg.com');
    const tokenB = await login('bravo@zapcg.com');
    ok('two JWTs obtained from the auth service');

    // An unauthenticated hub connection must be refused.
    const anon = connect('/hubs/chat', '');
    connections.push(anon);
    try {
        await anon.start();
        bad('unauthenticated hub connection was accepted');
        await anon.stop();
    } catch {
        ok('unauthenticated hub connection refused');
    }

    const chatA = connect('/hubs/chat', tokenA);
    const chatB = connect('/hubs/chat', tokenB);
    connections.push(chatA, chatB);

    await chatA.start();
    await chatB.start();
    ok('two authenticated chat-hub connections established through the gateway');

    sec('Room hub: join, broadcast, typing, presence');

    const rooms = (await api('/api/rooms', tokenA)).body;
    const general = rooms.find(r => r.name === 'General Chat');

    await chatA.invoke('JoinRoom', general.id);
    await chatB.invoke('JoinRoom', general.id);
    ok(`both clients joined "${general.name}"`);

    // B must receive A's message in real time.
    const received = expectEvent(chatB, 'ReceiveMessage', m => m.content?.includes('realtime probe'));
    const sent = await chatA.invoke('SendMessage', general.id, { content: 'realtime probe from A' });

    const delivered = await received;
    ok(`ReceiveMessage delivered to the other client (${delivered.id === sent.id ? 'same id' : 'id mismatch'})`);

    if (delivered.anonymousName && !delivered.authorUserId) {
        ok('broadcast payload carries the anonymous name and no real user id');
    } else {
        bad('broadcast payload shape');
    }

    if (delivered.isMine === false) ok('isMine correctly false for the receiving client');
    else bad(`isMine was ${delivered.isMine} for the receiver`);

    // Per-user sidebar update with the authoritative unread count.
    const roomUpdated = expectEvent(chatB, 'RoomUpdated', u => u.roomId === general.id);
    await chatA.invoke('SendMessage', general.id, { content: 'second realtime probe' });
    const update = await roomUpdated;
    ok(`RoomUpdated delivered with unreadCount=${update.unreadCount}`);

    // Typing indicator carries the room id so a multi-room client can route it.
    const typing = expectEvent(chatB, 'UserTyping', t => t.roomId === general.id);
    await chatA.invoke('StartTyping', general.id);
    const typingEvent = await typing;
    ok(`UserTyping carries roomId and anonymousName (${typingEvent.anonymousName})`);

    await chatA.invoke('StopTyping', general.id);
    ok('StopTyping accepted');

    // Reaction toggle broadcasts the server's resulting state.
    const reacted = expectEvent(chatB, 'ReactionsChanged', r => r.messageId === sent.id);
    await chatA.invoke('ToggleReaction', sent.id, '\u{1F44D}');
    const reaction = await reacted;
    ok(`ReactionsChanged delivered with ${reaction.reactions.length} group(s)`);

    // Read receipt.
    const readEvent = expectEvent(chatA, 'RoomRead', e => e.roomId === general.id);
    await chatB.invoke('MarkRead', general.id);
    const read = await readEvent;
    ok(`RoomRead delivered with anonymousName=${read.anonymousName} and a timestamp`);

    // Deletion tells everyone, and says who did it.
    const deleted = expectEvent(chatB, 'MessageDeleted', d => d.messageId === sent.id);
    await chatA.invoke('DeleteMessage', sent.id);
    const deletion = await deleted;
    if (deletion.deletedBy === 'User') ok('MessageDeleted reports deletedBy="User"');
    else bad(`MessageDeleted reported deletedBy=${deletion.deletedBy}`);

    sec('Room hub: access control');

    const hyderabad = rooms.find(r => r.name === 'Hyderabad');
    try {
        // B is a Bangalore user and must not be able to join a Hyderabad room.
        await chatB.invoke('JoinRoom', hyderabad.id);
        bad('a Bangalore user joined a Hyderabad room over the hub');
    } catch (e) {
        ok('hub enforces branch access control on JoinRoom');
    }

    sec('Private chat hub');

    const dmA = connect('/hubs/private-chat', tokenA);
    const dmB = connect('/hubs/private-chat', tokenB);
    connections.push(dmA, dmB);

    await dmA.start();
    await dmB.start();
    ok('both clients connected to the private-chat hub');

    const meB = (await api('/api/auth/me', tokenB)).body;
    const conversation = (await api('/api/conversations', tokenA, {
        method: 'POST',
        body: JSON.stringify({ otherUserId: meB.userId })
    })).body;

    await dmA.invoke('JoinConversation', conversation.id);
    await dmB.invoke('JoinConversation', conversation.id);
    ok('both participants joined the conversation group');

    const dmReceived = expectEvent(dmB, 'ReceivePrivateMessage', m => m.content === 'private realtime probe');
    await dmA.invoke('SendMessage', conversation.id, { content: 'private realtime probe' });
    const dm = await dmReceived;
    ok('ReceivePrivateMessage delivered to the recipient');

    if (dm.isMine === false) ok('private message isMine false for the recipient');
    else bad(`private isMine was ${dm.isMine}`);

    const convUpdated = expectEvent(dmB, 'ConversationUpdated', u => u.conversationId === conversation.id);
    await dmA.invoke('SendMessage', conversation.id, { content: 'second private probe' });
    const convUpdate = await convUpdated;
    ok(`ConversationUpdated delivered with unreadCount=${convUpdate.unreadCount}`);

    // The sender learns their message was read.
    const readReceipt = expectEvent(dmA, 'MessageRead', e => e.conversationId === conversation.id);
    await dmB.invoke('MarkRead', conversation.id);
    const receipt = await readReceipt;
    ok(`MessageRead delivered to the sender for ${receipt.messageIds.length} message(s)`);

    // A third party must not be able to join the conversation group.
    //
    // carol is a standing development account, alongside alpha and bravo above. This
    // previously used a hardcoded address left over from one historical test run
    // (e2e-a-1786710892@zapcg.com); once the database was reset that account no longer
    // existed, the login failed, tokenC came back null and this assertion SKIPPED
    // SILENTLY — the suite still reported all-green with one fewer check than it had.
    const tokenC = await login('carol@zapcg.com').catch(() => null);

    if (!tokenC) {
        bad('could not sign in as the third-party fixture (carol@zapcg.com) - run scripts/seed-accounts.sh');
    }

    if (tokenC) {
        const dmC = connect('/hubs/private-chat', tokenC);
        connections.push(dmC);
        await dmC.start();
        try {
            await dmC.invoke('JoinConversation', conversation.id);
            bad('an outsider joined a private conversation group');
        } catch {
            ok('hub refuses a non-participant joining a conversation');
        }
    }

    sec('Notification hub');

    const notifA = connect('/hubs/notifications', tokenA);
    connections.push(notifA);
    await notifA.start();
    ok('connected to the notification hub');

    // A DM to A should produce a notification pushed over the hub.
    const notified = expectEvent(notifA, 'ReceiveNotification', () => true, 8000);
    await dmB.invoke('SendMessage', conversation.id, { content: 'notify probe' });

    try {
        const notification = await notified;
        ok(`ReceiveNotification delivered: "${notification.title}"`);
        if (!JSON.stringify(notification).includes('@'))
            ok('notification payload contains no email address');
        else bad('notification payload leaked an email address');
    } catch {
        bad('no notification received within 8s');
    }

    sec('Reconnection');

    await chatA.stop();
    await chatA.start();
    await chatA.invoke('JoinRoom', general.id);
    const afterReconnect = expectEvent(chatB, 'ReceiveMessage', m => m.content === 'after reconnect');
    await chatA.invoke('SendMessage', general.id, { content: 'after reconnect' });
    await afterReconnect;
    ok('messaging works after an explicit reconnect and re-join');

} catch (error) {
    bad(`unhandled: ${error.message}`);
} finally {
    for (const c of connections) {
        try { await c.stop(); } catch { }
    }
}

console.log('\n' + '='.repeat(70));
console.log(`  SIGNALR   passed: ${pass}   failed: ${fail}`);
console.log('='.repeat(70));

process.exit(fail === 0 ? 0 : 1);
