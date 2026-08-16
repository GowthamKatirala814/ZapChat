import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ command, mode }) => {
    const env = loadEnv(mode, process.cwd(), 'VITE_')

    // Fail the BUILD, not the browser.
    //
    // src/config validates this too, but that check runs when the module is first
    // evaluated — which is after the bundle has shipped and the page has loaded. A
    // deployment missing its gateway URL would build cleanly and then break on first
    // paint for every user. Catching it here turns that into a failed build.
    if (command === 'build' && !env.VITE_API_URL?.trim()) {
        throw new Error(
            'VITE_API_URL is not set.\n\n' +
            'It is the API gateway origin and must be supplied before building for\n' +
            'production. Copy .env.example to .env.local for local builds, or set it\n' +
            'in the deployment environment.',
        )
    }

    return {
        plugins: [react(), tailwindcss()],

        server: {
            port: 5173,

            // Proxy the API through the dev server so the browser sees ONE origin.
            //
            // That is what lets the auth cookies be SameSite=Lax instead of SameSite=None:
            // serving the SPA from http://localhost:5173 while calling the API on
            // https://localhost:5000 makes every request cross-site, which forces
            // SameSite=None and removes the browser's built-in CSRF protection.
            //
            // Set VITE_API_URL to a relative value (e.g. "") to route through this proxy.
            proxy: {
                '/api': {
                    target: 'https://localhost:5000',
                    changeOrigin: true,
                    secure: false, // local development certificate
                },
                '/hubs': {
                    target: 'https://localhost:5000',
                    changeOrigin: true,
                    secure: false,
                    ws: true, // SignalR WebSocket upgrade
                },
            },
        },
    }
})
