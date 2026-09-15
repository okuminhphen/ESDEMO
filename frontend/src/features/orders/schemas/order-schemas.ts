import { z } from "zod";

export const paymentSchema = z.object({
  amount: z.number().int("VND must be a whole number.").min(0, "Enter the payment amount."),
});

export type PaymentFormValues = z.infer<typeof paymentSchema>;
