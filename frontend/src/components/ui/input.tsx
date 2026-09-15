import type { InputHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

type InputProps = InputHTMLAttributes<HTMLInputElement>;

export function Input({ className, ...props }: InputProps) {
  return (
    <input
      className={cn(
        "min-h-11 w-full rounded-lg border border-border bg-white px-3 text-sm text-forest outline-none transition placeholder:text-forest/40 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100 disabled:bg-mist",
        className,
      )}
      {...props}
    />
  );
}
