"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, CheckCircle2 } from "lucide-react";
import { getOrder } from "@/features/orders/api";
import { queryKeys } from "@/lib/query/query-keys";
import { formatVnd } from "@/lib/utils";

export function OrderDetail({ id }: { id: string }) {
  const order = useQuery({ queryKey: queryKeys.orders.detail(id), queryFn: () => getOrder(id) });
  if (order.isLoading) return <main className="mx-auto max-w-4xl px-5 py-20 text-forest/60">Loading order…</main>;
  if (order.isError || !order.data) return <main className="mx-auto max-w-4xl px-5 py-20"><p className="text-forest/60">This order is unavailable.</p><Link href="/orders" className="mt-5 inline-flex font-bold text-forest underline">Back to orders</Link></main>;
  const data = order.data;
  return <main className="mx-auto max-w-4xl px-5 py-12 lg:px-8 lg:py-16"><Link href="/orders" className="inline-flex items-center gap-2 text-sm font-bold text-forest/70"><ArrowLeft className="size-4" />Orders</Link><section className="mt-10 rounded-2xl border border-border bg-white p-7 sm:p-10"><div className="flex flex-col justify-between gap-5 sm:flex-row"><div><p className="font-mono text-xs font-bold tracking-[0.14em] text-forest/50">{data.orderNumber}</p><h1 className="mt-3 text-4xl font-black tracking-[-0.05em] text-forest">Order details</h1></div><span className={`h-fit rounded-full px-3 py-1.5 text-xs font-bold ${data.status === "Paid" ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"}`}>{data.status}</span></div><div className="mt-10 divide-y divide-border border-y border-border">{data.items.map((item) => <div key={item.productId} className="flex justify-between gap-5 py-5"><div><p className="font-bold text-forest">{item.productName}</p><p className="mt-1 text-sm text-forest/60">{item.quantity} × {formatVnd(item.unitPrice)}</p></div><p className="font-bold text-forest">{formatVnd(item.unitPrice * item.quantity)}</p></div>)}</div><div className="mt-6 flex justify-between text-xl font-black text-forest"><span>Total</span><span>{formatVnd(data.totalAmount)}</span></div>{data.status === "PendingPayment" ? <Link href={`/checkout?productId=${data.items[0]?.productId}&orderId=${data.id}`} className="mt-8 inline-flex min-h-11 items-center justify-center rounded-lg bg-forest px-5 text-sm font-bold text-white hover:bg-forest/90">Continue mock payment</Link> : data.status === "Paid" ? <p className="mt-8 flex items-center gap-2 text-sm font-bold text-emerald-800"><CheckCircle2 className="size-5" />Mock payment completed {data.paidAt ? new Date(data.paidAt).toLocaleString("vi-VN") : ""}</p> : null}</section></main>;
}
