# Vercel Frontend Deployment Guide

ZapChat's frontend is a React application built with Vite. It is optimized for static hosting on Vercel.

## Prerequisites

1. A Vercel account.
2. The ZapChat repository pushed to GitHub, GitLab, or Bitbucket.
3. The deployed Gateway API URL (e.g., `https://zapchat-gateway.onrender.com`).

## Deployment Steps

1. **Import Project**: In Vercel, click "Add New" -> "Project" and import your ZapChat repository.
2. **Configure Project**:
   - **Framework Preset**: Vercel should auto-detect "Vite".
   - **Root Directory**: `frontend` (Make sure to specify this so Vercel knows where the React app lives).
   - **Build Command**: `npm run build`
   - **Output Directory**: `dist`
3. **Environment Variables**:
   Add the following environment variable in the Vercel dashboard:
   - `VITE_API_BASE_URL`: Set this to your deployed Gateway API URL (e.g., `https://zapchat-gateway.onrender.com`). No trailing slash.
4. **Deploy**: Click "Deploy". Vercel will install dependencies, build the project, and assign it a live URL.

## Troubleshooting

- **CORS Errors**: If you get CORS errors, make sure your Vercel URL (e.g., `https://zapchat-frontend.vercel.app`) is added to the `AllowedOrigins` environment variable on the Gateway, Auth, Chat, Admin, Notification, Poll, and PrivateChat backend services.
- **WebSocket Failures**: SignalR requires WebSockets. Ensure `VITE_API_BASE_URL` is correctly pointing to the Gateway, which routes the hub requests to the respective microservices.
