"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import {
  api, ApiError, CATEGORIES, CATEGORY_LABELS, formatAmount, getSession, logout,
  type Category, type Expense, type ExpenseInput, type Summary,
} from "@/lib/api";

const currentMonth = () => new Date().toISOString().slice(0, 7);
const today = () => new Date().toISOString().slice(0, 10);
const emptyForm = (): ExpenseInput => ({ label: "", amount: 0, category: "Alimentation", date: today() });

export default function Dashboard() {
  const router = useRouter();
  // Ce composant n'est rendu que dans le navigateur (voir page.tsx) : la session se lit à l'initialisation.
  const [session] = useState(getSession);
  const [month, setMonth] = useState(currentMonth);
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [summary, setSummary] = useState<Summary | null>(null);
  const [form, setForm] = useState<ExpenseInput>(emptyForm);
  const [editing, setEditing] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  // Incrémenté après chaque modification pour recharger la liste et la synthèse.
  const [version, setVersion] = useState(0);
  const reload = () => setVersion((v) => v + 1);

  useEffect(() => {
    if (!session) {
      router.replace("/login");
      return;
    }
    let cancelled = false;
    Promise.all([api.list(month), api.summary(month)])
      .then(([list, sum]) => {
        if (cancelled) return;
        setExpenses(list);
        setSummary(sum);
      })
      .catch((e) => {
        if (cancelled) return;
        if (e instanceof ApiError && e.status === 401) router.replace("/login");
        else setError("Impossible de charger les dépenses.");
      });
    return () => {
      cancelled = true;
    };
  }, [session, month, version, router]);

  async function save(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    try {
      if (editing) await api.update(editing, form);
      else await api.create(form);
      setForm(emptyForm());
      setEditing(null);
      reload();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Enregistrement impossible.");
    }
  }

  async function remove(id: string) {
    if (!confirm("Supprimer cette dépense ?")) return;
    await api.remove(id);
    reload();
  }

  const max = Math.max(1, ...(summary?.byCategory.map((c) => c.total) ?? [1]));

  return (
    <main className="mx-auto max-w-5xl px-4 py-8">
      <header className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">💰 Mes dépenses</h1>
          <p className="text-sm text-slate-500">{session?.email}</p>
        </div>
        <div className="flex items-center gap-3">
          <input
            type="month" value={month} onChange={(e) => setMonth(e.target.value)} aria-label="Mois"
            className="rounded-md border border-slate-300 bg-transparent px-3 py-1.5 dark:border-slate-700"
          />
          <button onClick={() => { logout(); router.replace("/login"); }} className="text-sm text-slate-500 hover:underline">
            Déconnexion
          </button>
        </div>
      </header>

      <section className="mb-6 grid gap-4 md:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
          <p className="text-sm text-slate-500">Total du mois</p>
          <p className="mt-1 text-3xl font-semibold tabular-nums">{formatAmount(summary?.total ?? 0)}</p>
          <p className="mt-1 text-sm text-slate-500">{summary?.count ?? 0} dépense(s)</p>
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-5 md:col-span-2 dark:border-slate-800 dark:bg-slate-900">
          <p className="mb-3 text-sm text-slate-500">Par catégorie</p>
          {summary?.byCategory.length ? (
            <ul className="space-y-2">
              {summary.byCategory.map((c) => (
                <li key={c.category} className="grid grid-cols-[7rem_1fr_7rem] items-center gap-3 text-sm">
                  <span>{CATEGORY_LABELS[c.category]}</span>
                  <span className="h-2.5 rounded-full bg-slate-100 dark:bg-slate-800">
                    <span className="block h-full rounded-full bg-emerald-500" style={{ width: `${(c.total / max) * 100}%` }} />
                  </span>
                  <span className="text-right tabular-nums">{formatAmount(c.total)}</span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-slate-400">Aucune dépense ce mois-ci.</p>
          )}
        </div>
      </section>

      <form onSubmit={save} className="mb-6 grid gap-3 rounded-xl border border-slate-200 bg-white p-5 md:grid-cols-[2fr_1fr_1fr_1fr_auto] dark:border-slate-800 dark:bg-slate-900">
        <input
          required maxLength={120} placeholder="Libellé" value={form.label} aria-label="Libellé"
          onChange={(e) => setForm({ ...form, label: e.target.value })}
          className="rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700"
        />
        <input
          type="number" required min={0.001} step={0.001} placeholder="Montant" aria-label="Montant"
          value={form.amount || ""} onChange={(e) => setForm({ ...form, amount: Number(e.target.value) })}
          className="rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700"
        />
        <select
          value={form.category} aria-label="Catégorie"
          onChange={(e) => setForm({ ...form, category: e.target.value as Category })}
          className="rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700"
        >
          {CATEGORIES.map((c) => <option key={c} value={c}>{CATEGORY_LABELS[c]}</option>)}
        </select>
        <input
          type="date" required value={form.date} aria-label="Date"
          onChange={(e) => setForm({ ...form, date: e.target.value })}
          className="rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700"
        />
        <div className="flex gap-2">
          <button className="rounded-md bg-emerald-600 px-4 py-2 font-medium text-white hover:bg-emerald-700">
            {editing ? "Modifier" : "Ajouter"}
          </button>
          {editing && (
            <button type="button" onClick={() => { setEditing(null); setForm(emptyForm()); }} className="text-sm text-slate-500">
              Annuler
            </button>
          )}
        </div>
        {error && <p role="alert" className="text-sm text-red-600 md:col-span-5">{error}</p>}
      </form>

      <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-sm">
          <thead className="text-left text-slate-500">
            <tr>
              <th className="px-4 py-3 font-medium">Date</th>
              <th className="px-4 py-3 font-medium">Libellé</th>
              <th className="px-4 py-3 font-medium">Catégorie</th>
              <th className="px-4 py-3 text-right font-medium">Montant</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {expenses.map((e) => (
              <tr key={e.id} className="border-t border-slate-100 dark:border-slate-800">
                <td className="px-4 py-2 tabular-nums">{new Date(e.date).toLocaleDateString("fr-FR")}</td>
                <td className="px-4 py-2">{e.label}</td>
                <td className="px-4 py-2">{CATEGORY_LABELS[e.category]}</td>
                <td className="px-4 py-2 text-right tabular-nums">{formatAmount(e.amount)}</td>
                <td className="whitespace-nowrap px-4 py-2 text-right">
                  <button
                    onClick={() => { setEditing(e.id); setForm({ label: e.label, amount: e.amount, category: e.category, date: e.date }); }}
                    className="mr-3 text-emerald-700 hover:underline dark:text-emerald-400"
                  >
                    Modifier
                  </button>
                  <button onClick={() => remove(e.id)} className="text-red-600 hover:underline">Supprimer</button>
                </td>
              </tr>
            ))}
            {!expenses.length && (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-slate-400">Aucune dépense pour ce mois.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </main>
  );
}
