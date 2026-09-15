"use client";

import Link from "next/link";
import { useRef } from "react";
import { useRouter } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { CheckCircle2, LockKeyhole, ShoppingBag } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { FieldError } from "@/components/ui/field-error";
import { Input } from "@/components/ui/input";
import { getCurrentUser } from "@/features/auth/api";
import { getCatalogProduct } from "@/features/catalog/api";
import { createOrder, getOrder, payOrder } from "@/features/orders/api";
import { paymentSchema, type PaymentFormValues } from "@/features/orders/schemas/order-schemas";
import type { Order } from "@/features/orders/types";
import { ApiError, getProblemMessage } from "@/lib/http/api-error";
import { queryKeys } from "@/lib/query/query-keys";
import { formatVnd } from "@/lib/utils";

export function CheckoutWorkspace({ productId, orderId }: { productId: string; orderId?: string }) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const createKey = useRef(crypto.randomUUID());
  const payKey = useRef(crypto.randomUUID());
  const product = useQuery({ queryKey: queryKeys.catalog.detail(productId), queryFn: () => getCatalogProduct(productId) });
  const user = useQuery({ queryKey: queryKeys.auth.currentUser, queryFn: getCurrentUser, retry: false });
  const form = useForm<PaymentFormValues>({ resolver: zodResolver(paymentSchema), defaultValues: { amount: 0 } });
  const order = useQuery({ queryKey: queryKeys.orders.detail(orderId ?? ""), queryFn: () => getOrder(orderId!), enabled: Boolean(orderId) });
  const createMutation = useMutation({ mutationFn: () => createOrder({ productId, idempotencyKey: createKey.current }), onSuccess: (created) => { queryClient.setQueryData(queryKeys.orders.detail(created.id), created); router.replace(`/checkout?productId=${productId}&orderId=${created.id}`); router.refresh(); }, onError: (error) => toast.error(getProblemMessage(error)) });
  const payMutation = useMutation({ mutationFn: (values: PaymentFormValues) => payOrder(orderId!, { amount: values.amount, idempotencyKey: payKey.current }), onSuccess: (paid) => { queryClient.setQueryData(queryKeys.orders.detail(paid.id), paid); queryClient.invalidateQueries({ queryKey: queryKeys.orders.lists() }); toast.success("Mock payment completed."); router.replace(`/orders/${paid.id}`); router.refresh(); }, onError: (error) => { if (error instanceof ApiError && error.status === 409) queryClient.invalidateQueries({ queryKey: queryKeys.orders.detail(orderId!) }); toast.error(getProblemMessage(error)); } });

  if (product.isLoading || user.isLoading) return <main className="mx-auto max-w-5xl px-5 py-20 text-forest/60">Preparing checkout…</main>;
  if (!product.data || product.isError) return <main className="mx-auto max-w-5xl px-5 py-20"><p className="text-forest/60">This product is unavailable.</p><Link href="/products" className="mt-5 inline-flex font-bold text-forest underline">Back to catalog</Link></main>;
  if (!user.data) return <main className="mx-auto max-w-5xl px-5 py-20"><p className="text-forest/60">Sign in with a Customer account to checkout.</p><Link href={`/login?next=${encodeURIComponent(`/checkout?productId=${productId}`)}`} className="mt-5 inline-flex font-bold text-forest underline">Sign in</Link></main>;
  if (!user.data.roles.includes("Customer")) return <main className="mx-auto max-w-5xl px-5 py-20"><p className="text-forest/60">A Customer account is required for checkout.</p><Link href="/products" className="mt-5 inline-flex font-bold text-forest underline">Back to catalog</Link></main>;
  const item = product.data;
  const currentOrder = order.data;
  return <main className="mx-auto max-w-5xl px-5 py-12 lg:px-8 lg:py-16"><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Mock checkout</p><h1 className="mt-3 text-5xl font-black tracking-[-0.06em] text-forest">Confirm your order.</h1><p className="mt-4 max-w-2xl text-sm leading-6 text-forest/65">This is a demonstration payment. Do not enter card or bank details. The API recalculates the price and availability.</p><div className="mt-10 grid gap-6 lg:grid-cols-[1fr_0.78fr]"><section className="rounded-2xl border border-border bg-white p-7"><p className="font-mono text-xs font-bold tracking-[0.16em] text-forest/50">{item.sku}</p><h2 className="mt-3 text-3xl font-black tracking-[-0.04em] text-forest">{item.name}</h2><p className="mt-3 text-sm leading-6 text-forest/65">{item.description ?? "One product is included in this order."}</p><div className="mt-8 border-t border-border pt-5"><div className="flex justify-between text-sm text-forest/65"><span>Quantity</span><span>1</span></div><div className="mt-3 flex justify-between text-lg font-black text-forest"><span>Total</span><span>{formatVnd(currentOrder?.totalAmount ?? item.price)}</span></div></div></section><aside className="rounded-2xl bg-forest p-7 text-white">{!currentOrder ? <><ShoppingBag className="size-6 text-accent" /><h2 className="mt-8 text-2xl font-black tracking-[-0.04em]">Create a pending order.</h2><p className="mt-3 text-sm leading-6 text-white/65">It is valid for 30 minutes. Stock is checked at payment time.</p><Button className="mt-8 w-full bg-white text-forest hover:bg-mist" onClick={() => createMutation.mutate()} disabled={createMutation.isPending || !item.isInStock}>{createMutation.isPending ? "Creating…" : item.isInStock ? "Continue to payment" : "Out of stock"}</Button></> : currentOrder.status === "Paid" ? <PaidOrder order={currentOrder} /> : order.isLoading ? <p className="text-white/65">Loading order…</p> : <form onSubmit={form.handleSubmit((values) => payMutation.mutate(values))} noValidate><LockKeyhole className="size-6 text-accent" /><h2 className="mt-8 text-2xl font-black tracking-[-0.04em]">Enter the exact amount.</h2><p className="mt-3 text-sm leading-6 text-white/65">Enter {formatVnd(currentOrder.totalAmount)} to complete this mock payment.</p><label className="mt-7 block"><span className="mb-2 block text-sm font-bold">Amount (VND)</span><Input className="border-white/20 bg-white text-forest" type="number" min="0" step="1" {...form.register("amount", { valueAsNumber: true })} /><FieldError message={form.formState.errors.amount?.message} /></label><Button className="mt-6 w-full bg-white text-forest hover:bg-mist" type="submit" disabled={payMutation.isPending}>{payMutation.isPending ? "Processing…" : "Complete mock payment"}</Button></form>}</aside></div></main>;
}

function PaidOrder({ order }: { order: Order }) { return <><CheckCircle2 className="size-7 text-accent" /><h2 className="mt-8 text-2xl font-black">Payment completed.</h2><p className="mt-3 text-sm text-white/65">Order {order.orderNumber} was already paid.</p><Link href={`/orders/${order.id}`} className="mt-8 inline-flex min-h-11 items-center justify-center rounded-lg bg-white px-4 text-sm font-bold text-forest hover:bg-mist">View order</Link></>; }
