"use client";

import dynamic from "next/dynamic";

// Le tableau de bord dépend du navigateur (jeton en localStorage, date du jour) :
// il n'est pas pré-rendu côté serveur.
const Dashboard = dynamic(() => import("./dashboard"), { ssr: false });

export default function DashboardPage() {
  return <Dashboard />;
}
