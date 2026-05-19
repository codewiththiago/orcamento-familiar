import { api } from './client'
import type { Category } from '../types'

export async function getCategories(): Promise<Category[]> {
  const { data } = await api.get('/categories')
  return data
}

export async function createCategory(name: string): Promise<Category> {
  const { data } = await api.post('/categories', { name })
  return data
}

export async function deleteCategory(id: number): Promise<void> {
  await api.delete(`/categories/${id}`)
}
