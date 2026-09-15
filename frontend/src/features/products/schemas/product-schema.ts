import { z } from "zod";

const skuPattern = /^[A-Za-z0-9][A-Za-z0-9_-]*$/;

export const productFormSchema = z.object({
  sku: z
    .string()
    .trim()
    .min(1, "SKU is required.")
    .max(64, "SKU cannot exceed 64 characters.")
    .regex(skuPattern, "SKU may contain only letters, digits, underscores and hyphens."),
  name: z.string().trim().min(2, "Name must contain at least 2 characters.").max(200),
  description: z.string().max(4000).optional(),
  price: z.number().int("Price must be a whole number of VND.").min(0).max(999_999_999_999_999_999),
  stockQuantity: z.number().int("Stock quantity must be a whole number.").min(0).max(2_147_483_647),
  isActive: z.boolean(),
});

export type ProductFormValues = z.infer<typeof productFormSchema>;
