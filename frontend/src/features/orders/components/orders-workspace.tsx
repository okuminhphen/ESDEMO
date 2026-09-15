"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ClipboardList, ArrowRight } from "lucide-react";
import { getOrders } from "@/features/orders/api";
import { queryKeys } from "@/lib/query/query-keys";
import { formatVnd } from "@/lib/utils";

export function OrdersWorkspace() {
  const orders = useQuery({ queryKey: queryKeys.orders.list({ page: 1, pageSize: 30 }), queryFn: () => getOrders({ page: 1, pageSize: 30 }) });
  return <main className="mx-auto max-w-6xl px-5 py-12 lg:px-8 lg:py-16"><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Customer account</p><h1 className="mt-3 text-5xl font-black tracking-[-0.06em] text-forest">Your orders.</h1><p className="mt-4 text-sm leading-6 text-forest/65">Only orders belonging to your authenticated account appear here.</p><section className="mt-10 overflow-hidden rounded-2xl border border-border bg-white">{orders.isLoading ? <Message text="Loading orders…" /> : orders.isError ? <Message text="Orders could not be loaded. Please sign in again and retry." /> : orders.data?.items.length ? <div className="divide-y divide-border">{orders.data.items.map((order) => <Link href={`/orders/${order.id}`} key={order.id} className="flex items-center justify-between gap-4 p-5 hover:bg-mist"><div><p className="font-mono text-xs font-bold text-forest/50">{order.orderNumber}</p><p className="mt-2 font-bold text-forest">{formatVnd(order.totalAmount)}</p><p className="mt-1 text-xs text-forest/55">{new Date(order.createdAt).toLocaleString("vi-VN")}</p></div><div className="flex items-center gap-4"><span className={`rounded-full px-2.5 py-1 text-xs font-bold ${order.status === "Paid" ? "bg-emerald-100 text-emerald-800" : order.status === "PendingPayment" ? "bg-amber-100 text-amber-800" : "bg-red-100 text-red-700"}`}>{order.status === "PendingPayment" ? "Pending payment" : order.status}</span><ArrowRight className="size-5 text-forest/45" /></div></Link>)}</div> : <Message text="You have not created any orders yet." />}</section></main>;
}
function Message({ text }: { text: string }) { return <div className="px-6 py-20 text-center"><ClipboardList className="mx-auto size-7 text-forest/40" /><p className="mt-4 text-sm text-forest/60">{text}</p><Link href="/products" className="mt-5 inline-flex font-bold text-forest underline">Browse products</Link></div>; }
