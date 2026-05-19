import React, { createContext, useContext, useEffect, useState } from 'react'
import { setAccessToken } from '../api/client'
import { login as apiLogin, logout as apiLogout, refreshToken } from '../api/auth'
import type { User } from '../types'

interface AuthContextValue {
  user: User | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue>(null!)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    refreshToken()
      .then((data) => {
        setAccessToken(data.accessToken)
        setUser(data)
      })
      .catch(() => {
        setUser(null)
      })
      .finally(() => setLoading(false))
  }, [])

  async function login(email: string, password: string) {
    const data = await apiLogin(email, password)
    setAccessToken(data.accessToken)
    setUser(data)
  }

  async function logout() {
    await apiLogout()
    setAccessToken(null)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  return useContext(AuthContext)
}
