import { api } from './client'
import type { User, UserInfo } from '../types'

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
  name: string; email: string; password: string; inviteCode?: string; pin?: string
}): Promise<User> {
  const { data } = await api.post('/auth/register', payload)
  return data
}

export async function getRegistrationStatus(): Promise<{ requiresCode: boolean }> {
  const { data } = await api.get('/auth/registration-status')
  return data
}

export async function getFamilyCode(): Promise<{ inviteCode: string; hasCode: boolean }> {
  const { data } = await api.get('/auth/family-code')
  return data
}

export async function regenerateFamilyCode(): Promise<{ inviteCode: string; pin: string }> {
  const { data } = await api.post('/auth/family-code/regenerate')
  return data
}

export async function getUsers(): Promise<UserInfo[]> {
  const { data } = await api.get('/auth/users')
  return data
}
