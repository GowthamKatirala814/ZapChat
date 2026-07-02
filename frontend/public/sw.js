self.addEventListener('push', function(event) {
    if (event.data) {
        let payload = null;
        try {
            payload = event.data.json();
        } catch (e) {
            payload = { title: 'New Notification', body: event.data.text(), url: '/' };
        }

        const title = payload.title || 'ZapChat Notification';
        const options = {
            body: payload.body || 'You have a new message.',
            icon: '/icons.svg',
            badge: '/icons.svg',
            data: { url: payload.url || '/' }
        };

        event.waitUntil(self.registration.showNotification(title, options));
    }
});

self.addEventListener('notificationclick', function(event) {
    event.notification.close();
    event.waitUntil(
        clients.matchAll({ type: 'window' }).then(windowClients => {
            for (let i = 0; i < windowClients.length; i++) {
                const client = windowClients[i];
                if (client.url.includes(event.notification.data.url) && 'focus' in client) {
                    return client.focus();
                }
            }
            if (clients.openWindow) {
                return clients.openWindow(event.notification.data.url);
            }
        })
    );
});
