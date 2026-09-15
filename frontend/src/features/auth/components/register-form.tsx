"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { FieldError } from "@/components/ui/field-error";
import { Input } from "@/components/ui/input";
import { register } from "@/features/auth/api";
import { registerSchema, type RegisterFormValues } from "@/features/auth/schemas/auth-schemas";
import { getProblemMessage } from "@/lib/http/api-error";

export function RegisterForm() {
  const router = useRouter();
  const form = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: "", displayName: "", password: "", confirmPassword: "" },
  });
  const mutation = useMutation({
    mutationFn: register,
    onSuccess: (user) => {
      toast.success(`Account created for ${user.displayName}. You can sign in now.`);
      router.replace("/login");
    },
    onError: (error) => toast.error(getProblemMessage(error)),
  });

  return (
    <form className="space-y-5" onSubmit={form.handleSubmit((values) => mutation.mutate(values))} noValidate>
      <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Display name</span><Input autoComplete="name" placeholder="Your name" {...form.register("displayName")} /><FieldError message={form.formState.errors.displayName?.message} /></label>
      <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Email address</span><Input type="email" autoComplete="email" placeholder="you@example.com" {...form.register("email")} /><FieldError message={form.formState.errors.email?.message} /></label>
      <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Password</span><Input type="password" autoComplete="new-password" placeholder="At least 12 characters" {...form.register("password")} /><FieldError message={form.formState.errors.password?.message} /></label>
      <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Confirm password</span><Input type="password" autoComplete="new-password" placeholder="Repeat your password" {...form.register("confirmPassword")} /><FieldError message={form.formState.errors.confirmPassword?.message} /></label>
      <Button className="w-full" type="submit" disabled={mutation.isPending}>{mutation.isPending ? "Creating account…" : "Create account"}</Button>
      <p className="text-center text-sm text-forest/65">Already have an account? <Link className="font-bold text-forest underline underline-offset-4" href="/login">Sign in</Link></p>
    </form>
  );
}
