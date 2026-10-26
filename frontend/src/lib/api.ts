export const CATEGORIES = [
  "Alimentation",
  "Transport",
  "Logement",
  "Sante",
  "Loisirs",
  "Factures",
  "Autre",
] as const;

export type Category = (typeof CATEGORIES)[number];

export const CATEGORY_LABELS: Record<Category, string> = {
  Alimentation: "Alimentation",
  Transport: "Transport",
  Logement: "Logement",
  Sante: "Santé",
  Loisirs: "Loisirs",
  Factures: "Factures",
  Autre: "Autre",
};

export type Expense = {
  id: string;
  label: string;
  amount: number;
  category: Category;
  date: string;
};

export type ExpenseInput = Omit<Expense, "id">;

export type Summary = {
  month: string;
  total: number;
  count: number;
  byCategory: { category: Category; total: number; count: number }[];
};

type Auth = { token: string; expiresAt: string; email: string };

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";
const STORAGE_KEY = "expense-tracker.auth";

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

export function getSession(): Auth | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const auth = JSON.parse(raw) as Auth;
    return new Date(auth.expiresAt) > new Date() ? auth : null;
  } catch {
    return null;
  }
}

export function logout() {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    /* stockage indisponible : rien à effacer */
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const session = getSession();
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(session ? { Authorization: `Bearer ${session.token}` } : {}),
      ...init.headers,
    },
  });
  if (response.status === 401 && session) logout();
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    const details = problem?.errors ? Object.values(problem.errors).flat().join(" ") : null;
    throw new ApiError(response.status, details || problem?.detail || "Une erreur est survenue.");
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

async function authenticate(path: string, email: string, password: string) {
  const auth = await request<Auth>(path, { method: "POST", body: JSON.stringify({ email, password }) });
  localStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
  return auth;
}

export const api = {
  register: (email: string, password: string) => authenticate("/api/auth/register", email, password),
  login: (email: string, password: string) => authenticate("/api/auth/login", email, password),
  list: (month: string) => request<Expense[]>(`/api/expenses?month=${month}`),
  summary: (month: string) => request<Summary>(`/api/expenses/summary?month=${month}`),
  create: (input: ExpenseInput) => request<Expense>("/api/expenses", { method: "POST", body: JSON.stringify(input) }),
  update: (id: string, input: ExpenseInput) =>
    request<Expense>(`/api/expenses/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  remove: (id: string) => request<void>(`/api/expenses/${id}`, { method: "DELETE" }),
};

export const formatAmount = (value: number) =>
  new Intl.NumberFormat("fr-TN", { style: "currency", currency: "TND", minimumFractionDigits: 3 }).format(value);
