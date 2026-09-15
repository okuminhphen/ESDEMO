"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { LogOut } from "lucide-react";
import { toast } from "sonner";
import { getCurrentUser, logout } from "@/features/auth/api";
import { Button } from "@/components/ui/button";
import { queryKeys } from "@/lib/query/query-keys";

export function AuthMenu() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const currentUser = useQuery({ queryKey: queryKeys.auth.currentUser, queryFn: getCurrentUser, retry: false });
  const logoutMutation = useMutation({
    mutationFn: logout,
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: queryKeys.auth.currentUser });
      router.replace("/");
      router.refresh();
      toast.success("You have been signed out.");
    },
    onError: () => toast.error("Could not sign out. Please try again."),
  });

  if (!currentUser.data) {
    return (
      <div className="flex items-center gap-2">
        <Link href="/login" className="hidden px-3 py-2 text-sm font-semibold text-forest sm:inline">Sign in</Link>
        <Link href="/register" className="rounded-lg bg-forest px-3 py-2 text-sm font-semibold text-white hover:bg-forest/90">Create account</Link>
      </div>
    );
  }

  const isAdmin = currentUser.data.roles.includes("Admin");
  return (
    <div className="flex items-center gap-2">
      {isAdmin ? <Link href="/admin" className="hidden px-3 py-2 text-sm font-semibold text-forest sm:inline">Admin console</Link> : currentUser.data.roles.includes("Customer") ? <Link href="/orders" className="hidden px-3 py-2 text-sm font-semibold text-forest sm:inline">My orders</Link> : null}
      <Button variant="ghost" size="small" onClick={() => logoutMutation.mutate()} disabled={logoutMutation.isPending} aria-label="Sign out">
        <LogOut className="size-4" />
        <span className="hidden sm:inline">Sign out</span>
      </Button>
    </div>
  );
}
