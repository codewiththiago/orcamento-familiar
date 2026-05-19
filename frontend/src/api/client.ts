import axios from 'axios'

const BASE_URL = import.meta.env.VITE_API_URL ?? '/api'

export const api = axios.create({
  baseURL: BASE_URL,
  withCredentials: true,
  timeout: 10000,
})

let accessToken: string | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export function getAccessToken() {
  return accessToken
}

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    const isRefreshRequest = original?.url?.includes('/auth/refresh')
    if (error.response?.status === 401 && !original._retry && !isRefreshRequest) {
      original._retry = true
      try {
        const { data } = await axios.post(`${BASE_URL}/auth/refresh`, {}, { withCredentials: true })
        setAccessToken(data.accessToken)
        original.headers.Authorization = `Bearer ${data.accessToken}`
        return api(original)
      } catch {
        setAccessToken(null)
        window.dispatchEvent(new Event('auth:session-expired'))
      }
    }
    return Promise.reject(error)
  }
)
