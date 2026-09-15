export const queryKeys = {
  auth: {
    currentUser: ["auth", "current-user"] as const,
  },
  products: {
    all: ["products"] as const,
    lists: () => [...queryKeys.products.all, "list"] as const,
    list: (input: Record<string, unknown>) => [...queryKeys.products.lists(), input] as const,
    details: () => [...queryKeys.products.all, "detail"] as const,
    detail: (id: string) => [...queryKeys.products.details(), id] as const,
  },
};
