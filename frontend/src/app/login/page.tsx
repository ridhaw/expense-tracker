"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { api, ApiError } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [mode, setMode] = useState<"login" | "register">("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await (mode === "login" ? api.login(email, password) : api.register(email, password));
      router.push("/dashboard");
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Serveur injoignable.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-sm flex-col justify-center px-4">
      <h1 className="mb-1 text-2xl font-semibold">💰 Suivi des dépenses</h1>
      <p className="mb-6 text-sm text-slate-500">
        {mode === "login" ? "Connectez-vous à votre compte." : "Créez votre compte en quelques secondes."}
      </p>
      <form onSubmit={submit} className="space-y-3 rounded-xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <label className="block text-sm">
          Email
          <input
            type="email" required value={email} onChange={(e) => setEmail(e.target.value)}
            className="mt-1 w-full rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700"
          />
        </label>
        <label className="block text-sm">
          Mot de passe
          <input
            type="password" required minLength={mode === "register" ? 8 : 1} value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="mt-1 w-full rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700"
          />
        </label>
        {mode === "register" && <p className="text-xs text-slate-500">8 caractères minimum.</p>}
        {error && <p role="alert" className="text-sm text-red-600">{error}</p>}
        <button
          disabled={busy}
          className="w-full rounded-md bg-emerald-600 py-2 font-medium text-white hover:bg-emerald-700 disabled:opacity-60"
        >
          {busy ? "…" : mode === "login" ? "Se connecter" : "Créer le compte"}
        </button>
      </form>
      <button
        onClick={() => { setMode(mode === "login" ? "register" : "login"); setError(null); }}
        className="mt-4 text-sm text-emerald-700 hover:underline dark:text-emerald-400"
      >
        {mode === "login" ? "Pas encore de compte ? S'inscrire" : "Déjà inscrit ? Se connecter"}
      </button>
    </main>
  );
}
