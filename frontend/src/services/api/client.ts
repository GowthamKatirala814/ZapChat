import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from "axios";
import { api, config } from "../../config";
import type { ApiErrorBody } from "../../types/api";

/**
 * The single HTTP client.
 *
 * The previous frontend created six axios instances, each carrying its own copy of a
 * bearer interceptor, a retry interceptor, and a hardcoded base URL — plus a module-level
 * token cache fed by an endpoint that echoed the HttpOnly JWT back in plaintext.
 *
 * None of that is needed now: every backend service reads the `access_token` cookie
 * directly, so the browser sends the credential and there is no token for JavaScript to
 * hold. `withCredentials` is the whole authentication story for HTTP.
 */

export const http: AxiosInstance = axios.create({
  baseURL: config.apiUrl,
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
  timeout: 20_000,
});

// ── Session expiry ────────────────────────────────────────────────────────────

let onSessionLost: (() => void) | null = null;

/** Registered once by AuthProvider so a terminal 401 clears the session. */
export function setSessionLostHandler(handler: (() => void) | null) {
  onSessionLost = handler;
}

/**
 * A burst of parallel requests hitting an expired token must trigger ONE refresh.
 * Refresh tokens rotate and reuse revokes the whole family, so a stampede of concurrent
 * refreshes would log the user out — which is exactly the failure the old client had.
 */
let inFlightRefresh: Promise<boolean> | null = null;

function refreshOnce(): Promise<boolean> {
  inFlightRefresh ??= axios
    .post(`${config.apiUrl}${api.auth.refresh}`, null, { withCredentials: true })
    .then(() => true)
    .catch(() => false)
    .finally(() => {
      // Cleared on the next tick so requests queued during the refresh still join it.
      setTimeout(() => {
        inFlightRefresh = null;
      }, 0);
    });

  return inFlightRefresh;
}

/** Endpoints where a 401 is the answer, not a signal to refresh. */
const NEVER_REFRESH = [
  api.auth.login,
  api.auth.refresh,
  api.auth.logout,
  api.auth.registerInitiate,
  api.auth.registerVerify,
  api.auth.registerComplete,
  api.auth.forgotPassword,
  api.auth.verifyResetOtp,
  api.auth.resetPassword,
];

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as
      | (InternalAxiosRequestConfig & { _refreshed?: boolean })
      | undefined;

    const url = original?.url ?? "";

    if (
      error.response?.status === 401 &&
      original &&
      !original._refreshed &&
      !NEVER_REFRESH.some((path) => url.includes(path))
    ) {
      original._refreshed = true;

      if (await refreshOnce()) return http(original);

      onSessionLost?.();
    }

    return Promise.reject(error);
  },
);

/**
 * Transient retry. Only idempotent methods, only network failures and 502/503/504 —
 * never a 4xx, which will not succeed on a second attempt.
 */
const MAX_RETRIES = 2;

http.interceptors.response.use(undefined, async (error: AxiosError) => {
  const original = error.config as
    | (InternalAxiosRequestConfig & { _retries?: number })
    | undefined;

  if (!original) return Promise.reject(error);

  const status = error.response?.status;
  const transient = !error.response || status === 502 || status === 503 || status === 504;
  const idempotent = ["get", "head", "options"].includes(
    (original.method ?? "get").toLowerCase(),
  );

  original._retries ??= 0;

  if (transient && idempotent && original._retries < MAX_RETRIES) {
    original._retries++;
    await new Promise((resolve) => setTimeout(resolve, 400 * 2 ** (original._retries! - 1)));
    return http(original);
  }

  return Promise.reject(error);
});

// ── Error model ───────────────────────────────────────────────────────────────

/**
 * A normalised view of any failure.
 *
 * Every service returns the same JSON error shape, so the UI can show the server's own
 * message — "This channel is limited to the Hyderabad office" — instead of the generic
 * "An unexpected error occurred" that the old backend produced for every failure.
 */
export class ApiError {
  readonly status: number;
  readonly code: string;
  readonly message: string;
  readonly traceId?: string;
  readonly fieldErrors?: Record<string, string[]>;
  /** Present on a 422: the moderation category that blocked the content. */
  readonly category?: string;
  readonly isNetworkError: boolean;

  private constructor(init: {
    status: number;
    code: string;
    message: string;
    traceId?: string;
    fieldErrors?: Record<string, string[]>;
    category?: string;
    isNetworkError: boolean;
  }) {
    this.status = init.status;
    this.code = init.code;
    this.message = init.message;
    this.traceId = init.traceId;
    this.fieldErrors = init.fieldErrors;
    this.category = init.category;
    this.isNetworkError = init.isNetworkError;
  }

  static from(error: unknown): ApiError | null {
    if (error instanceof ApiError) return error;

    if (axios.isAxiosError<ApiErrorBody>(error)) {
      if (!error.response) {
        return new ApiError({
          status: 0,
          code: "network_error",
          message: "Cannot reach the server.",
          isNetworkError: true,
        });
      }

      const body = error.response.data;

      return new ApiError({
        status: error.response.status,
        code: body?.code ?? "error",
        message: body?.message ?? error.message,
        traceId: body?.traceId,
        fieldErrors: body?.errors,
        category: body?.category,
        isNetworkError: false,
      });
    }

    if (error instanceof Error) {
      return new ApiError({
        status: 0,
        code: "client_error",
        message: error.message,
        isNetworkError: false,
      });
    }

    return null;
  }

  get isUnauthorized() {
    return this.status === 401;
  }
  get isForbidden() {
    return this.status === 403;
  }
  get isNotFound() {
    return this.status === 404;
  }
  get isConflict() {
    return this.status === 409;
  }
  /** A moderation rejection. The composer renders these inline, not as a page error. */
  get isRejectedByModeration() {
    return this.status === 422;
  }
}

/** Human-readable message for a toast or inline error. */
export function errorMessage(error: unknown, fallback = "Something went wrong."): string {
  return ApiError.from(error)?.message ?? fallback;
}

/** Unwraps an axios response to its data. */
export const unwrap = <T>(promise: Promise<{ data: T }>): Promise<T> =>
  promise.then((response) => response.data);
