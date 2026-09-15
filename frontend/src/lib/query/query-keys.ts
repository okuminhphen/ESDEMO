export const queryKeys = {
  auth: {
    currentUser: ["auth", "current-user"] as const,
  },
  catalog: {
    all: ["catalog"] as const,
    lists: () => [...queryKeys.catalog.all, "list"] as const,
    list: (input: object) => [...queryKeys.catalog.lists(), input] as const,
    detail: (id: string) => [...queryKeys.catalog.all, "detail", id] as const,
  },
  orders: {
    all: ["orders"] as const,
    lists: () => [...queryKeys.orders.all, "list"] as const,
    list: (input: object) => [...queryKeys.orders.lists(), input] as const,
    detail: (id: string) => [...queryKeys.orders.all, "detail", id] as const,
  },
  products: {
    all: ["products"] as const,
    lists: () => [...queryKeys.products.all, "list"] as const,
    list: (input: Record<string, unknown>) => [...queryKeys.products.lists(), input] as const,
    details: () => [...queryKeys.products.all, "detail"] as const,
    detail: (id: string) => [...queryKeys.products.details(), id] as const,
  },
};
