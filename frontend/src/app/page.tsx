"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { getSession } from "@/lib/api";

export default function Home() {
  const router = useRouter();
  useEffect(() => {
    router.replace(getSession() ? "/dashboard" : "/login");
  }, [router]);
  return null;
}
