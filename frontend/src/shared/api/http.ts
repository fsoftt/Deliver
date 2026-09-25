/**
 * Thin fetch wrapper. Every call goes to the API gateway (`/api/...`), never to a service directly:
 * the browser does not know the internal topology.
 */

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with ${status}`)
    this.status = status
    this.problem = problem
  }

  /** Field errors from a 400 validation problem, flattened for display. */
  get fieldErrors(): string[] {
    return Object.entries(this.problem.errors ?? {}).flatMap(([field, messages]) =>
      messages.map((message) => `${field}: ${message}`),
    )
  }
}

// One correlation id per browser session makes it easy to find this user's flow in logs and Jaeger.
const correlationId = `web-${crypto.randomUUID()}`

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const response = await fetch(path, {
    method,
    headers: {
      Accept: 'application/json',
      'X-Correlation-Id': correlationId,
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new ApiError(response.status, problem)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export const http = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T = void>(path: string, body?: unknown) => request<T>('POST', path, body ?? {}),
  put: <T = void>(path: string, body?: unknown) => request<T>('PUT', path, body),
}

/** GET that treats 404 as "not there yet" (eventual consistency) instead of an error. */
export async function getOptional<T>(path: string): Promise<T | null> {
  try {
    return await http.get<T>(path)
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null
    throw error
  }
}
