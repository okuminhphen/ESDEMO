export type CatalogProduct = {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  price: number;
  isInStock: boolean;
};

export type CatalogProductPage = {
  items: CatalogProduct[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type CatalogProductListInput = {
  page?: number;
  pageSize?: number;
  search?: string;
};
