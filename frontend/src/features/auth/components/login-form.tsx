"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { FieldError } from "@/components/ui/field-error";
import { Input } from "@/components/ui/input";
import { login } from "@/features/auth/api";
import { loginSchema, type LoginFormValues } from "@/features/auth/schemas/auth-schemas";
import { getProblemMessage } from "@/lib/http/api-error";
import { queryKeys } from "@/lib/query/query-keys";

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();
  const form = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema), defaultValues: { email: "", password: "" } });
  const mutation = useMutation({
    mutationFn: login,
    onSuccess: (user) => {
      queryClient.setQueryData(queryKeys.auth.currentUser, user);
      toast.success(`Welcome back, ${user.displayName}.`);
      const next = searchParams.get("next");
      router.replace(next?.startsWith("/") ? next : user.roles.includes("Admin") ? "/admin" : "/");
      router.refresh();
    },
    onError: (error) => toast.error(getProblemMessage(error)),
  });

  return (
    <form className="space-y-5" onSubmit={form.handleSubmit((values) => mutation.mutate(values))} noValidate>
      <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Email address</span><Input type="email" autoComplete="email" placeholder="you@example.com" {...form.register("email")} /><FieldError message={form.formState.errors.email?.message} /></label>
      <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Password</span><Input type="password" autoComplete="current-password" placeholder="Your password" {...form.register("password")} /><FieldError message={form.formState.errors.password?.message} /></label>
      <Button className="w-full" type="submit" disabled={mutation.isPending}>{mutation.isPending ? "Signing in…" : "Sign in"}</Button>
      <p className="text-center text-sm text-forest/65">New to ESDEMO? <Link className="font-bold text-forest underline underline-offset-4" href="/register">Create an account</Link></p>
    </form>
  );
}
