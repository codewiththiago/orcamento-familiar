import { api } from './client'
import type { User, UserInfo, Invite } from '../types'

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

export async function register(payload: {
  token?: string; name: string; email: string; password: string
}): Promise<User> {
  const { data } = await api.post('/auth/register', payload)
  return data
}

export async function validateInvite(token: string): Promise<{ email: string; isValid: boolean }> {
  const { data } = await api.get(`/auth/invite/${token}`)
  return data
}

export async function createInvite(email: string): Promise<Invite> {
  const { data } = await api.post('/auth/invite', { email })
  return data
}

export async function deleteInvite(id: number): Promise<void> {
  await api.delete(`/auth/invite/${id}`)
}

export async function getInvites(): Promise<Invite[]> {
  const { data } = await api.get('/auth/invites')
  return data
}

export async function getUsers(): Promise<UserInfo[]> {
  const { data } = await api.get('/auth/users')
  return data
}
