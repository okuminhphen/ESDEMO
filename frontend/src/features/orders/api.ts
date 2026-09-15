"use client";

import { browserRequest } from "@/lib/http/browser-client";
import type { CreateOrderPayload, Order, OrderPage, PayOrderPayload } from "@/features/orders/types";

export function getOrders(input: { page?: number; pageSize?: number }) { return browserRequest<OrderPage>({ method: "GET", url: "/orders", params: input }); }
export function getOrder(id: string) { return browserRequest<Order>({ method: "GET", url: `/orders/${id}` }); }
export function createOrder(payload: CreateOrderPayload) { return browserRequest<Order>({ method: "POST", url: "/orders", data: payload }); }
export function payOrder(id: string, payload: PayOrderPayload) { return browserRequest<Order>({ method: "POST", url: `/orders/${id}/pay`, data: payload }); }
