import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "ESDEMO",
  description: "Next.js and .NET Clean Architecture starter",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
