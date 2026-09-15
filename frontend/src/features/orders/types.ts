export type OrderItem = { productId: string; productName: string; unitPrice: number; quantity: number };
export type Order = { id: string; orderNumber: string; status: "PendingPayment" | "Paid" | "Cancelled" | "Expired"; totalAmount: number; currency: "VND"; createdAt: string; expiresAt: string; paidAt: string | null; items: OrderItem[] };
export type OrderPage = { items: Order[]; totalCount: number; page: number; pageSize: number };
export type CreateOrderPayload = { productId: string; idempotencyKey: string };
export type PayOrderPayload = { amount: number; idempotencyKey: string };
