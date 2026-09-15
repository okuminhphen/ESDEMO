export type Product = {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  price: number;
  stockQuantity: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  deletedAt: string | null;
  version: number;
};

export type ProductPage = {
  items: Product[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type ProductListInput = {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
  includeDeleted?: boolean;
};

export type ProductPayload = {
  sku: string;
  name: string;
  description: string | null;
  price: number;
  stockQuantity: number;
  isActive: boolean;
};

export type UpdateProductPayload = ProductPayload & { version: number };
