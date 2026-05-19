import { api } from './client'
import type { User } from '../types'

export async function login(email: string, password: string): Promise<User> {
  const { data } = await api.post('/auth/login', { email, password })
  return data
}

export async function logout(): Promise<void> {
  await api.post('/auth/logout')
}

export async function refreshToken(): Promise<User> {
  const { data } = await api.post('/auth/refresh')
  return data
}
